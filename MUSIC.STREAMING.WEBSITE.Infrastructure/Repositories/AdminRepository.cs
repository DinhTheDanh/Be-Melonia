using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly IDbConnection _connection;

    public AdminRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    // ==================== Dashboard ====================
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM users WHERE role = 'User') AS TotalUsers,
                (SELECT COUNT(*) FROM users WHERE role IN ('Artist', 'ArtistPremium')) AS TotalArtists,
                (SELECT COUNT(*) FROM songs) AS TotalSongs,
                (SELECT COUNT(*) FROM subscriptions WHERE status = 'Active') AS TotalSubscriptions,
                (SELECT COALESCE(SUM(amount), 0) FROM payments WHERE status = 'Success') AS TotalRevenue,
                (SELECT COUNT(*) FROM users WHERE DATE(created_at) = CURDATE()) AS NewUsersToday,
                (SELECT COUNT(*) FROM songs WHERE DATE(created_at) = CURDATE()) AS NewSongsToday
        ";
        return await _connection.QueryFirstAsync<DashboardStatsDto>(sql);
    }

    // ==================== Users ====================
    public async Task<AdminPagingResult<AdminUserDto>> GetUsersAsync(string keyword, int pageIndex, int pageSize, string? role)
    {
        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        if (!string.IsNullOrEmpty(keyword))
        {
            whereClauses.Add("(u.full_name LIKE @Keyword OR u.email LIKE @Keyword OR u.username LIKE @Keyword)");
            parameters.Add("Keyword", $"%{keyword}%");
        }

        if (!string.IsNullOrEmpty(role))
        {
            whereClauses.Add("u.role = @Role");
            parameters.Add("Role", role);
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(*) FROM users u {whereSql}";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        var dataSql = $@"
            SELECT 
                u.user_id AS UserId,
                u.full_name AS FullName,
                u.username AS UserName,
                u.email AS Email,
                u.avatar AS AvatarUrl,
                u.role AS Role,
                CASE WHEN u.is_active = 0 THEN 1 ELSE 0 END AS IsBanned,
                u.created_at AS CreatedAt
            FROM users u
            {whereSql}
            ORDER BY u.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var data = await _connection.QueryAsync<AdminUserDto>(dataSql, parameters);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new AdminPagingResult<AdminUserDto>
        {
            Data = data,
            TotalRecords = totalCount,
            TotalPages = totalPages,
            FromRecord = totalCount == 0 ? 0 : offset + 1,
            ToRecord = totalCount == 0 ? 0 : Math.Min(pageIndex * pageSize, totalCount)
        };
    }

    public async Task<bool?> GetUserBanStatusAsync(Guid userId)
    {
        var sql = "SELECT is_active FROM users WHERE user_id = @UserId LIMIT 1";
        var isActive = await _connection.QueryFirstOrDefaultAsync<bool?>(sql, new { UserId = userId });
        if (isActive == null) return null;
        return !isActive.Value; // IsBanned = !IsActive
    }

    public async Task<bool> ToggleBanUserAsync(Guid userId)
    {
        // Toggle is_active: nếu đang active → ban (is_active = 0), ngược lại → unban
        var sql = @"
            UPDATE users 
            SET is_active = NOT is_active, updated_at = @UpdatedAt 
            WHERE user_id = @UserId";
        var rows = await _connection.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow });
        return rows > 0;
    }

    // ==================== Artists ====================
    public async Task<AdminPagingResult<AdminArtistDto>> GetArtistsAsync(string keyword, int pageIndex, int pageSize)
    {
        var parameters = new DynamicParameters();
        var whereSql = "WHERE u.role IN ('Artist', 'ArtistPremium')";

        if (!string.IsNullOrEmpty(keyword))
        {
            whereSql += " AND (u.full_name LIKE @Keyword OR u.username LIKE @Keyword)";
            parameters.Add("Keyword", $"%{keyword}%");
        }

        var countSql = $"SELECT COUNT(*) FROM users u {whereSql}";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        var dataSql = $@"
            SELECT 
                u.user_id AS UserId,
                u.user_id AS ArtistId,
                u.full_name AS FullName,
                COALESCE(u.username, u.full_name) AS ArtistName,
                u.email AS Email,
                u.avatar AS AvatarUrl,
                (SELECT COUNT(DISTINCT sa.song_id) FROM song_artists sa WHERE sa.artist_id = u.user_id) AS SongCount,
                (SELECT COUNT(*) FROM user_follows uf WHERE uf.following_id = u.user_id) AS FollowerCount,
                (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss 
                 JOIN song_artists sa ON uss.song_id = sa.song_id 
                 WHERE sa.artist_id = u.user_id) AS TotalListens,
                u.created_at AS CreatedAt
            FROM users u
            {whereSql}
            ORDER BY u.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var data = await _connection.QueryAsync<AdminArtistDto>(dataSql, parameters);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new AdminPagingResult<AdminArtistDto>
        {
            Data = data,
            TotalRecords = totalCount,
            TotalPages = totalPages,
            FromRecord = totalCount == 0 ? 0 : offset + 1,
            ToRecord = totalCount == 0 ? 0 : Math.Min(pageIndex * pageSize, totalCount)
        };
    }

    // ==================== Songs ====================
    public async Task<AdminPagingResult<AdminSongDto>> GetSongsAsync(string keyword, int pageIndex, int pageSize)
    {
        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        if (!string.IsNullOrEmpty(keyword))
        {
            whereClauses.Add(@"(s.title LIKE @Keyword 
                OR EXISTS (
                    SELECT 1 FROM song_artists sa2 
                    JOIN users u2 ON sa2.artist_id = u2.user_id 
                    WHERE sa2.song_id = s.song_id AND u2.full_name LIKE @Keyword
                ))");
            parameters.Add("Keyword", $"%{keyword}%");
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(*) FROM songs s {whereSql}";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        var dataSql = $@"
            SELECT 
                s.song_id AS Id,
                s.song_id AS SongId,
                s.title AS Title,
                (SELECT GROUP_CONCAT(u.full_name SEPARATOR ', ') 
                 FROM song_artists sa JOIN users u ON sa.artist_id = u.user_id 
                 WHERE sa.song_id = s.song_id) AS ArtistNames,
                s.thumbnail AS Thumbnail,
                s.file_url AS FileUrl,
                s.duration AS Duration,
                (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) AS ListenCount,
                (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) AS LikeCount,
                (SELECT g.name FROM song_genres sg JOIN genres g ON sg.genre_id = g.id WHERE sg.song_id = s.song_id LIMIT 1) AS GenreName,
                s.created_at AS CreatedAt
            FROM songs s
            {whereSql}
            ORDER BY s.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var data = await _connection.QueryAsync<AdminSongDto>(dataSql, parameters);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new AdminPagingResult<AdminSongDto>
        {
            Data = data,
            TotalRecords = totalCount,
            TotalPages = totalPages,
            FromRecord = totalCount == 0 ? 0 : offset + 1,
            ToRecord = totalCount == 0 ? 0 : Math.Min(pageIndex * pageSize, totalCount)
        };
    }

    public async Task<bool> DeleteSongAsync(Guid songId)
    {
        // Xóa các bản ghi liên quan trước
        await _connection.ExecuteAsync("DELETE FROM song_artists WHERE song_id = @SongId", new { SongId = songId });
        await _connection.ExecuteAsync("DELETE FROM song_genres WHERE song_id = @SongId", new { SongId = songId });
        await _connection.ExecuteAsync("DELETE FROM user_likes WHERE song_id = @SongId", new { SongId = songId });
        await _connection.ExecuteAsync("DELETE FROM user_song_stats WHERE song_id = @SongId", new { SongId = songId });
        await _connection.ExecuteAsync("DELETE FROM listening_histories WHERE song_id = @SongId", new { SongId = songId });
        await _connection.ExecuteAsync("DELETE FROM playlist_songs WHERE song_id = @SongId", new { SongId = songId });

        var rows = await _connection.ExecuteAsync("DELETE FROM songs WHERE song_id = @SongId", new { SongId = songId });
        return rows > 0;
    }

    // ==================== Subscriptions ====================
    public async Task<AdminPagingResult<AdminSubscriptionDto>> GetSubscriptionsAsync(string keyword, int pageIndex, int pageSize, string? status)
    {
        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        if (!string.IsNullOrEmpty(keyword))
        {
            whereClauses.Add("(u.full_name LIKE @Keyword OR u.username LIKE @Keyword)");
            parameters.Add("Keyword", $"%{keyword}%");
        }

        if (!string.IsNullOrEmpty(status))
        {
            whereClauses.Add("sub.status = @Status");
            parameters.Add("Status", status);
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM subscriptions sub
            JOIN users u ON sub.user_id = u.user_id
            {whereSql}";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        var dataSql = $@"
            SELECT 
                sub.subscription_id AS SubscriptionId,
                sub.user_id AS UserId,
                u.username AS UserName,
                u.full_name AS FullName,
                sub.plan_id AS PlanId,
                sp.plan_name AS PlanName,
                sub.status AS Status,
                sub.start_date AS StartDate,
                sub.end_date AS EndDate,
                sub.created_at AS CreatedAt
            FROM subscriptions sub
            JOIN users u ON sub.user_id = u.user_id
            JOIN subscription_plans sp ON sub.plan_id = sp.plan_id
            {whereSql}
            ORDER BY sub.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var data = await _connection.QueryAsync<AdminSubscriptionDto>(dataSql, parameters);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new AdminPagingResult<AdminSubscriptionDto>
        {
            Data = data,
            TotalRecords = totalCount,
            TotalPages = totalPages,
            FromRecord = totalCount == 0 ? 0 : offset + 1,
            ToRecord = totalCount == 0 ? 0 : Math.Min(pageIndex * pageSize, totalCount)
        };
    }

    // ==================== Payments ====================
    public async Task<AdminPagingResult<AdminPaymentDto>> GetPaymentsAsync(string keyword, int pageIndex, int pageSize, string? status)
    {
        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        if (!string.IsNullOrEmpty(keyword))
        {
            whereClauses.Add("(u.full_name LIKE @Keyword OR u.username LIKE @Keyword)");
            parameters.Add("Keyword", $"%{keyword}%");
        }

        if (!string.IsNullOrEmpty(status))
        {
            whereClauses.Add("p.status = @Status");
            parameters.Add("Status", status);
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM payments p
            JOIN users u ON p.user_id = u.user_id
            {whereSql}";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        var dataSql = $@"
            SELECT 
                p.payment_id AS PaymentId,
                p.user_id AS UserId,
                u.username AS UserName,
                u.full_name AS FullName,
                p.amount AS Amount,
                sp.plan_name AS PlanName,
                p.plan_id AS PlanId,
                p.status AS Status,
                p.payment_method AS PaymentMethod,
                p.transaction_id AS TransactionId,
                p.created_at AS CreatedAt
            FROM payments p
            JOIN users u ON p.user_id = u.user_id
            JOIN subscription_plans sp ON p.plan_id = sp.plan_id
            {whereSql}
            ORDER BY p.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var data = await _connection.QueryAsync<AdminPaymentDto>(dataSql, parameters);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new AdminPagingResult<AdminPaymentDto>
        {
            Data = data,
            TotalRecords = totalCount,
            TotalPages = totalPages,
            FromRecord = totalCount == 0 ? 0 : offset + 1,
            ToRecord = totalCount == 0 ? 0 : Math.Min(pageIndex * pageSize, totalCount)
        };
    }

    public async Task<AdminPagingResult<AdminPaymentDto>> GetPendingPaymentsAsync(int pageIndex, int pageSize)
    {
        return await GetPaymentsAsync("", pageIndex, pageSize, "Pending");
    }

    public async Task<bool> ApprovePaymentAsync(Guid paymentId)
    {
        // 1. Cập nhật status payment → Success
        var updatePaymentSql = @"
            UPDATE payments 
            SET status = 'Success', updated_at = @UpdatedAt 
            WHERE payment_id = @PaymentId AND status = 'Pending'";
        var rows = await _connection.ExecuteAsync(updatePaymentSql, new { PaymentId = paymentId, UpdatedAt = DateTime.UtcNow });
        if (rows == 0) return false;

        // 2. Lấy thông tin payment để tạo subscription
        var payment = await _connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT user_id AS UserId, plan_id AS PlanId FROM payments WHERE payment_id = @PaymentId",
            new { PaymentId = paymentId });
        if (payment == null) return false;

        // 3. Lấy thông tin plan
        var plan = await _connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT duration_months AS DurationMonths, role_granted AS RoleGranted FROM subscription_plans WHERE plan_id = @PlanId",
            new { PlanId = (Guid)payment.PlanId });
        if (plan == null) return false;

        // 4. Hủy subscription cũ nếu có
        await _connection.ExecuteAsync(
            @"UPDATE subscriptions SET status = 'Cancelled', updated_at = @UpdatedAt 
              WHERE user_id = @UserId AND status = 'Active'",
            new { UserId = (Guid)payment.UserId, UpdatedAt = DateTime.UtcNow });

        // 5. Tạo subscription mới
        var now = DateTime.UtcNow;
        var subscriptionSql = @"
            INSERT INTO subscriptions (subscription_id, user_id, plan_id, start_date, end_date, status, created_at, updated_at)
            VALUES (@SubscriptionId, @UserId, @PlanId, @StartDate, @EndDate, 'Active', @CreatedAt, @UpdatedAt)";
        await _connection.ExecuteAsync(subscriptionSql, new
        {
            SubscriptionId = Guid.NewGuid(),
            UserId = (Guid)payment.UserId,
            PlanId = (Guid)payment.PlanId,
            StartDate = now,
            EndDate = now.AddMonths((int)plan.DurationMonths),
            CreatedAt = now,
            UpdatedAt = now
        });

        // 6. Cập nhật role user
        await _connection.ExecuteAsync(
            "UPDATE users SET role = @Role, updated_at = @UpdatedAt WHERE user_id = @UserId",
            new { Role = (string)plan.RoleGranted, UserId = (Guid)payment.UserId, UpdatedAt = now });

        return true;
    }

    public async Task<bool> RejectPaymentAsync(Guid paymentId)
    {
        var sql = @"
            UPDATE payments 
            SET status = 'Rejected', updated_at = @UpdatedAt 
            WHERE payment_id = @PaymentId AND status = 'Pending'";
        var rows = await _connection.ExecuteAsync(sql, new { PaymentId = paymentId, UpdatedAt = DateTime.UtcNow });
        return rows > 0;
    }

    // ==================== Set User Role ====================
    public async Task<string?> GetUserRoleAsync(Guid userId)
    {
        var sql = "SELECT role FROM users WHERE user_id = @UserId LIMIT 1";
        return await _connection.QueryFirstOrDefaultAsync<string?>(sql, new { UserId = userId });
    }

    public async Task<bool> SetUserRoleAsync(Guid userId, string role)
    {
        var sql = @"
            UPDATE users 
            SET role = @Role, updated_at = @UpdatedAt 
            WHERE user_id = @UserId";
        var rows = await _connection.ExecuteAsync(sql, new { UserId = userId, Role = role, UpdatedAt = DateTime.UtcNow });
        return rows > 0;
    }

    // ==================== Update Payment Status ====================
    public async Task<AdminUpdatePaymentStatusResponseDto?> UpdatePaymentStatusAsync(Guid paymentId, string newStatus)
    {
        // 1. Get current payment info
        var payment = await _connection.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT p.payment_id AS PaymentId, p.user_id AS UserId, p.plan_id AS PlanId, p.status AS Status
              FROM payments p WHERE p.payment_id = @PaymentId",
            new { PaymentId = paymentId });

        if (payment == null) return null;

        // 2. Update payment status
        var updateSql = @"
            UPDATE payments 
            SET status = @Status, updated_at = @UpdatedAt 
            WHERE payment_id = @PaymentId";
        await _connection.ExecuteAsync(updateSql, new { PaymentId = paymentId, Status = newStatus, UpdatedAt = DateTime.UtcNow });

        bool roleUpdated = false;

        // 3. If transitioning to Success → create subscription + update role
        if (newStatus == "Success" && (string)payment.Status != "Success")
        {
            var plan = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT duration_months AS DurationMonths, role_granted AS RoleGranted FROM subscription_plans WHERE plan_id = @PlanId",
                new { PlanId = (Guid)payment.PlanId });

            if (plan != null)
            {
                // Cancel old subscriptions
                await _connection.ExecuteAsync(
                    @"UPDATE subscriptions SET status = 'Cancelled', updated_at = @UpdatedAt 
                      WHERE user_id = @UserId AND status = 'Active'",
                    new { UserId = (Guid)payment.UserId, UpdatedAt = DateTime.UtcNow });

                // Create new subscription
                var now = DateTime.UtcNow;
                await _connection.ExecuteAsync(@"
                    INSERT INTO subscriptions (subscription_id, user_id, plan_id, start_date, end_date, status, created_at, updated_at)
                    VALUES (@SubscriptionId, @UserId, @PlanId, @StartDate, @EndDate, 'Active', @CreatedAt, @UpdatedAt)",
                    new
                    {
                        SubscriptionId = Guid.NewGuid(),
                        UserId = (Guid)payment.UserId,
                        PlanId = (Guid)payment.PlanId,
                        StartDate = now,
                        EndDate = now.AddMonths((int)plan.DurationMonths),
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                // Update user role
                await _connection.ExecuteAsync(
                    "UPDATE users SET role = @Role, updated_at = @UpdatedAt WHERE user_id = @UserId",
                    new { Role = (string)plan.RoleGranted, UserId = (Guid)payment.UserId, UpdatedAt = now });

                roleUpdated = true;
            }
        }

        return new AdminUpdatePaymentStatusResponseDto
        {
            Message = "Payment status updated",
            PaymentId = paymentId,
            NewStatus = newStatus,
            RoleUpdated = roleUpdated
        };
    }

    // ==================== Cancel Expired Payments ====================
    public async Task<CancelExpiredPaymentsResponseDto> CancelExpiredPaymentsAsync(int daysThreshold)
    {
        // Find expired pending payments
        var findSql = @"
            SELECT payment_id AS PaymentId 
            FROM payments 
            WHERE status = 'Pending' AND created_at < DATE_SUB(UTC_TIMESTAMP(), INTERVAL @Days DAY)";
        var expiredIds = (await _connection.QueryAsync<Guid>(findSql, new { Days = daysThreshold })).ToList();

        if (expiredIds.Count > 0)
        {
            var updateSql = @"
                UPDATE payments 
                SET status = 'Cancelled', updated_at = @UpdatedAt 
                WHERE status = 'Pending' AND created_at < DATE_SUB(UTC_TIMESTAMP(), INTERVAL @Days DAY)";
            await _connection.ExecuteAsync(updateSql, new { Days = daysThreshold, UpdatedAt = DateTime.UtcNow });
        }

        return new CancelExpiredPaymentsResponseDto
        {
            CancelledCount = expiredIds.Count,
            CancelledPaymentIds = expiredIds
        };
    }

    // ==================== Helpers ====================
    public async Task<dynamic?> GetPaymentWithPlanAsync(Guid paymentId)
    {
        var sql = @"
            SELECT p.user_id AS UserId, p.plan_id AS PlanId, sp.plan_name AS PlanName
            FROM payments p
            JOIN subscription_plans sp ON p.plan_id = sp.plan_id
            WHERE p.payment_id = @PaymentId";
        return await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { PaymentId = paymentId });
    }
}
