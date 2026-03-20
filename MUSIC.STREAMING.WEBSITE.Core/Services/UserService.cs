using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User không tồn tại");

        return new UserProfileDto
        {
            UserId = user.UserId,
            Email = user.Email,
            Username = user.Username,
            FullName = user.FullName,
            Bio = user.Bio,
            Avatar = user.Avatar,
            Banner = user.Banner,
            ArtistType = user.ArtistType,
            Role = user.Role
        };
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        // Lấy user hiện tại từ DB lên
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("Không tìm thấy người dùng.");

        // Cập nhật các trường thông tin
        user.FullName = dto.FullName;
        user.Bio = dto.Bio;
        user.ArtistType = dto.ArtistType;

        // Chỉ cập nhật ảnh nếu Frontend có gửi link mới lên
        // (Nếu gửi chuỗi rỗng hoặc null nghĩa là họ không đổi ảnh)
        if (!string.IsNullOrEmpty(dto.Avatar))
        {
            user.Avatar = dto.Avatar;
        }

        if (!string.IsNullOrEmpty(dto.Banner))
        {
            user.Banner = dto.Banner;
        }

        user.UpdatedAt = DateTime.Now;

        // Lưu xuống DB
        await _userRepository.UpdateAsync(user.UserId, user);
    }

    public async Task UpdateInterestsAsync(Guid userId, List<Guid> genreIds)
    {
        if (genreIds == null)
        {
            genreIds = new List<Guid>();
        }

        await _userRepository.AddUserFavoriteGenresAsync(userId, genreIds);
    }

    public async Task<PagingResult<ArtistDto>> GetArtistsAsync(string? keyword, int pageIndex, int pageSize)
    {
        var result = await _userRepository.GetArtistsPagingAsync(keyword ?? string.Empty, pageIndex, pageSize);

        var artistIds = result.Data.Select(u => u.UserId).ToList();
        var statsMap = await _userRepository.GetArtistsStatsBatchAsync(artistIds);

        var artistDtos = result.Data.Select(u =>
        {
            statsMap.TryGetValue(u.UserId, out var stats);
            return new ArtistDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Avatar = u.Avatar,
                Banner = u.Banner,
                Bio = u.Bio,
                ArtistType = u.ArtistType,
                FollowerCount = stats?.FollowerCount ?? 0,
                SongCount = stats?.SongCount ?? 0,
                TotalLikes = stats?.TotalLikes ?? 0,
                TotalListens = stats?.TotalListens ?? 0
            };
        });

        return new PagingResult<ArtistDto>
        {
            Data = artistDtos,
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            FromRecord = result.FromRecord,
            ToRecord = result.ToRecord
        };
    }

    public async Task<IEnumerable<GenreDto>> GetUserFavoriteGenresAsync(Guid userId)
    {
        return await _userRepository.GetUserFavoriteGenresAsync(userId);
    }

    public async Task<ArtistAnalyticsDashboardDto> GetArtistAnalyticsDashboardAsync(Guid artistId, int days)
    {
        var normalizedDays = NormalizeAnalyticsDays(days);

        var toDate = DateTime.Today;
        var fromDate = toDate.AddDays(-(normalizedDays - 1));
        var toDateExclusive = toDate.AddDays(1);

        var stats = await _userRepository.GetArtistStatsAsync(artistId);
        var increments = (await _userRepository.GetArtistDailyIncrementsAsync(artistId, fromDate, toDateExclusive)).ToList();

        var rangeFollowerDelta = increments.Sum(x => x.FollowersDelta);
        var rangeListenDelta = increments.Sum(x => x.ListensDelta);
        var rangeLikeDelta = increments.Sum(x => x.LikesDelta);

        var currentFollowers = Math.Max(0, stats.FollowerCount - rangeFollowerDelta);
        var currentListens = Math.Max(0, stats.TotalListens - rangeListenDelta);
        var currentLikes = Math.Max(0, stats.TotalLikes - rangeLikeDelta);

        var incrementsByDate = increments
            .GroupBy(x => x.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Followers = g.Sum(v => v.FollowersDelta),
                    Listens = g.Sum(v => v.ListensDelta),
                    Likes = g.Sum(v => v.LikesDelta)
                });

        var trends = new List<ArtistTrendPointDto>();
        for (var i = 0; i < normalizedDays; i++)
        {
            var date = fromDate.AddDays(i).Date;

            if (incrementsByDate.TryGetValue(date, out var delta))
            {
                currentFollowers += delta.Followers;
                currentListens += delta.Listens;
                currentLikes += delta.Likes;
            }

            trends.Add(new ArtistTrendPointDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Followers = currentFollowers,
                Listens = currentListens,
                Likes = currentLikes
            });
        }

        return new ArtistAnalyticsDashboardDto
        {
            Summary = new ArtistAnalyticsSummaryDto
            {
                TotalFollowers = stats.FollowerCount,
                TotalListens = stats.TotalListens,
                TotalLikes = stats.TotalLikes,
                TotalSongs = stats.SongCount
            },
            Trends = trends
        };
    }

    public async Task<PagingResult<ArtistTopSongDto>> GetArtistTopSongsAsync(Guid artistId, int days, int pageIndex, int pageSize)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;

        var normalizedDays = NormalizeAnalyticsDays(days);

        var toDate = DateTime.Today;
        var fromDate = toDate.AddDays(-(normalizedDays - 1));
        var toDateExclusive = toDate.AddDays(1);

        return await _userRepository.GetArtistTopSongsAsync(artistId, fromDate, toDateExclusive, pageIndex, pageSize);
    }

    private static int NormalizeAnalyticsDays(int days)
    {
        return days == 7 || days == 30 || days == 90 ? days : 30;
    }
}
