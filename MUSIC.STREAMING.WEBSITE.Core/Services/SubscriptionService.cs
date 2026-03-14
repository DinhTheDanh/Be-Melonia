using Microsoft.Extensions.Logging;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<SubscriptionService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<SubscriptionPlanDto>>> GetAllPlansAsync()
    {
        try
        {
            var plans = await _planRepository.GetActivePlansAsync();
            var dtos = plans.Select(p => new SubscriptionPlanDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                DurationMonths = p.DurationMonths,
                Price = p.Price,
                RoleGranted = p.RoleGranted,
                UploadLimit = p.UploadLimit,
                CanScheduleRelease = p.CanScheduleRelease,
                HasAdvancedAnalytics = p.HasAdvancedAnalytics
            });
            return Result<IEnumerable<SubscriptionPlanDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return Result<IEnumerable<SubscriptionPlanDto>>.Failure("Lỗi khi lấy danh sách gói subscription");
        }
    }

    public async Task<Result<SubscriptionDto>> GetActiveSubscriptionAsync(Guid userId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            if (subscription == null)
                return Result<SubscriptionDto>.NotFound("Bạn chưa có gói subscription nào đang hoạt động");

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId);

            return Result<SubscriptionDto>.Success(MapToDto(subscription, plan));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription for UserId={UserId}", userId);
            return Result<SubscriptionDto>.Failure("Lỗi khi lấy thông tin subscription");
        }
    }

    public async Task<Result<SubscriptionDto>> CreateSubscriptionAsync(Guid userId, Guid planId)
    {
        try
        {
            // 1. Validate plan
            var plan = await _planRepository.GetByIdAsync(planId);
            if (plan == null)
                return Result<SubscriptionDto>.NotFound("Gói subscription không tồn tại");

            // 2. Kiểm tra subscription active hiện tại
            var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            if (existingSubscription != null)
            {
                // Kết thúc subscription cũ trước khi tạo mới
                await _subscriptionRepository.UpdateStatusAsync(existingSubscription.SubscriptionId, "Replaced");
                _logger.LogInformation(
                    "Existing subscription {OldSubId} replaced for UserId={UserId}",
                    existingSubscription.SubscriptionId, userId);
            }

            // 3. Tạo subscription mới
            var now = DateTime.UtcNow;
            var subscription = new Subscription
            {
                SubscriptionId = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                StartDate = now,
                EndDate = now.AddMonths(plan.DurationMonths),
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _subscriptionRepository.CreateAsync(subscription);

            // 4. Cập nhật Role user theo gói
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.Role = plan.RoleGranted;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(userId, user);

                _logger.LogInformation(
                    "User role updated: UserId={UserId}, NewRole={Role}, PlanName={PlanName}",
                    userId, plan.RoleGranted, plan.PlanName);

                // Gửi notification: Role đã được cập nhật
                await _notificationService.SendSystemNotificationAsync(
                    userId,
                    "Nâng cấp tài khoản thành công",
                    $"Tài khoản của bạn đã được nâng cấp lên {plan.RoleGranted} với gói {plan.PlanName}.",
                    "role_update",
                    subscription.SubscriptionId);
            }

            _logger.LogInformation(
                "Subscription created: SubId={SubId}, UserId={UserId}, PlanId={PlanId}, EndDate={EndDate}",
                subscription.SubscriptionId, userId, planId, subscription.EndDate);

            return Result<SubscriptionDto>.Success(MapToDto(subscription, plan));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for UserId={UserId}, PlanId={PlanId}", userId, planId);
            return Result<SubscriptionDto>.Failure("Lỗi khi tạo subscription");
        }
    }

    public async Task ProcessExpiredSubscriptionsAsync()
    {
        try
        {
            var expiredSubscriptions = await _subscriptionRepository.GetExpiredActiveSubscriptionsAsync();
            var processedCount = 0;

            foreach (var subscription in expiredSubscriptions)
            {
                try
                {
                    // 1. Đánh dấu subscription hết hạn
                    await _subscriptionRepository.UpdateStatusAsync(subscription.SubscriptionId, "Expired");

                    // 2. Kiểm tra user có subscription active khác không
                    var otherActive = await _subscriptionRepository.GetActiveSubscriptionAsync(subscription.UserId);
                    if (otherActive == null)
                    {
                        // Không có subscription active nào khác → revert về User
                        var user = await _userRepository.GetByIdAsync(subscription.UserId);
                        if (user != null && user.Role != "User" && user.Role != "Admin")
                        {
                            user.Role = "User";
                            user.UpdatedAt = DateTime.UtcNow;
                            await _userRepository.UpdateAsync(subscription.UserId, user);

                            _logger.LogInformation(
                                "User role reverted to User: UserId={UserId}, ExpiredSubId={SubId}",
                                subscription.UserId, subscription.SubscriptionId);

                            // Gửi notification: Subscription hết hạn, role bị revert
                            await _notificationService.SendSystemNotificationAsync(
                                subscription.UserId,
                                "Subscription hết hạn",
                                "Gói subscription của bạn đã hết hạn. Tài khoản đã được chuyển về gói miễn phí.",
                                "subscription",
                                subscription.SubscriptionId);
                        }
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing expired subscription: SubId={SubId}, UserId={UserId}",
                        subscription.SubscriptionId, subscription.UserId);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} expired subscriptions", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessExpiredSubscriptionsAsync");
        }
    }

    public async Task<Result<IEnumerable<SubscriptionDto>>> GetSubscriptionHistoryAsync(Guid userId)
    {
        try
        {
            var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);
            var plans = await _planRepository.GetActivePlansAsync();
            var planDict = plans.ToDictionary(p => p.PlanId);

            var dtos = subscriptions.Select(s =>
            {
                planDict.TryGetValue(s.PlanId, out var plan);
                return MapToDto(s, plan);
            });

            return Result<IEnumerable<SubscriptionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history for UserId={UserId}", userId);
            return Result<IEnumerable<SubscriptionDto>>.Failure("Lỗi khi lấy lịch sử subscription");
        }
    }

    private static SubscriptionDto MapToDto(Subscription subscription, SubscriptionPlan? plan)
    {
        var daysRemaining = Math.Max(0, (int)(subscription.EndDate - DateTime.UtcNow).TotalDays);

        return new SubscriptionDto
        {
            SubscriptionId = subscription.SubscriptionId,
            PlanId = subscription.PlanId,
            PlanName = plan?.PlanName ?? "Unknown",
            RoleGranted = plan?.RoleGranted ?? "User",
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            DaysRemaining = daysRemaining,
            UploadLimit = plan?.UploadLimit ?? 0,
            CanScheduleRelease = plan?.CanScheduleRelease ?? false,
            HasAdvancedAnalytics = plan?.HasAdvancedAnalytics ?? false
        };
    }
}
