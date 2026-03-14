using Microsoft.Extensions.Logging;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IVnPayService _vnPayService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ISubscriptionPlanRepository planRepository,
        ISubscriptionService subscriptionService,
        IVnPayService vnPayService,
        INotificationService notificationService,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _planRepository = planRepository;
        _subscriptionService = subscriptionService;
        _vnPayService = vnPayService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<PaymentResponseDto>> CreatePaymentAsync(Guid userId, CreatePaymentDto dto, string ipAddress)
    {
        try
        {
            // 1. Validate plan
            var plan = await _planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null)
                return Result<PaymentResponseDto>.NotFound("Gói subscription không tồn tại");

            if (!plan.IsActive)
                return Result<PaymentResponseDto>.BadRequest("Gói subscription này đã ngừng hoạt động");

            // 2. Kiểm tra xem user có payment pending cho plan này không (tránh tạo trùng)
            var existingPending = await _paymentRepository.GetPendingPaymentAsync(userId, dto.PlanId);
            if (existingPending != null && !string.IsNullOrEmpty(existingPending.PaymentUrl))
            {
                // Trả lại payment URL cũ nếu còn hiệu lực
                return Result<PaymentResponseDto>.Success(new PaymentResponseDto
                {
                    PaymentId = existingPending.PaymentId,
                    OrderId = existingPending.OrderId,
                    Amount = existingPending.Amount,
                    Status = existingPending.Status,
                    PaymentUrl = existingPending.PaymentUrl,
                    PaymentMethod = existingPending.PaymentMethod,
                    CreatedAt = existingPending.CreatedAt
                });
            }

            // 3. Tạo mã đơn hàng unique (VNPay yêu cầu alphanumeric, unique per merchant)
            var orderId = $"{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..32];

            // 4. Tạo Payment entity với Status = Pending
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = userId,
                PlanId = dto.PlanId,
                Amount = plan.Price,
                Status = "Pending",
                PaymentMethod = dto.PaymentMethod,
                OrderId = orderId,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 5. Tạo VNPay payment URL
            // OrderInfo chỉ dùng ASCII thuần, không space, không ký tự đặc biệt
            var safeOrderInfo = $"Payment{orderId}";
            var paymentUrl = _vnPayService.CreatePaymentUrl(
                payment.PaymentId,
                orderId,
                plan.Price,
                safeOrderInfo,
                ipAddress
            );

            payment.PaymentUrl = paymentUrl;

            // 6. Save to database
            await _paymentRepository.CreateAsync(payment);

            _logger.LogInformation(
                "Payment created: PaymentId={PaymentId}, OrderId={OrderId}, UserId={UserId}, PlanId={PlanId}, Amount={Amount}",
                payment.PaymentId, orderId, userId, dto.PlanId, plan.Price);

            // 6.5 Gửi notification cho user: Payment đang Pending
            await _notificationService.SendSystemNotificationAsync(
                userId,
                "Thanh toán đang chờ xử lý",
                $"Đơn thanh toán gói {plan.PlanName} ({plan.Price:N0} VNĐ) đã được tạo và đang chờ xử lý.",
                "payment",
                payment.PaymentId);

            // 7. Trả về response cho frontend
            return Result<PaymentResponseDto>.Success(new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                OrderId = orderId,
                Amount = plan.Price,
                Status = "Pending",
                PaymentUrl = paymentUrl,
                PaymentMethod = dto.PaymentMethod,
                CreatedAt = payment.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for UserId={UserId}, PlanId={PlanId}", userId, dto.PlanId);
            return Result<PaymentResponseDto>.Failure("Lỗi hệ thống khi tạo thanh toán. Vui lòng thử lại.");
        }
    }

    public async Task<Result<string>> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams)
    {
        try
        {
            // 1. Xác thực chữ ký bảo mật
            if (!_vnPayService.ValidateSignature(queryParams))
            {
                _logger.LogWarning("VNPay IPN: Invalid signature");
                return Result<string>.Failure("Invalid signature", ResultType.Unauthorized);
            }

            // 2. Lấy thông tin từ callback
            var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "");
            var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
            var transactionNo = queryParams.GetValueOrDefault("vnp_TransactionNo", "");
            var amountStr = queryParams.GetValueOrDefault("vnp_Amount", "0");
            var amount = long.Parse(amountStr) / 100; // VNPay trả về nhân 100
            var transactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "");

            _logger.LogInformation(
                "VNPay IPN received: TxnRef={TxnRef}, ResponseCode={ResponseCode}, TransactionNo={TransactionNo}, Amount={Amount}",
                txnRef, responseCode, transactionNo, amount);

            // 3. Tìm payment theo OrderId
            var payment = await _paymentRepository.GetByOrderIdAsync(txnRef);
            if (payment == null)
            {
                _logger.LogWarning("VNPay IPN: Payment not found for OrderId={OrderId}", txnRef);
                return Result<string>.NotFound("Payment not found");
            }

            // 4. Kiểm tra payment đã được xử lý chưa (idempotent)
            if (payment.Status != "Pending")
            {
                _logger.LogInformation("VNPay IPN: Payment already processed. PaymentId={PaymentId}, Status={Status}",
                    payment.PaymentId, payment.Status);
                return Result<string>.Success("Already processed");
            }

            // 5. Verify amount matches
            if (payment.Amount != amount)
            {
                _logger.LogWarning(
                    "VNPay IPN: Amount mismatch. Expected={Expected}, Received={Received}, PaymentId={PaymentId}",
                    payment.Amount, amount, payment.PaymentId);
                await _paymentRepository.UpdateStatusAsync(payment.PaymentId, "Failed", transactionNo, responseCode);
                return Result<string>.Failure("Amount mismatch");
            }

            // 6. Xử lý kết quả thanh toán
            if (responseCode == "00" && transactionStatus == "00")
            {
                // THANH TOÁN THÀNH CÔNG
                await _paymentRepository.UpdateStatusAsync(payment.PaymentId, "Success", transactionNo, responseCode);

                // Tạo Subscription
                var subscriptionResult = await _subscriptionService.CreateSubscriptionAsync(payment.UserId, payment.PlanId);
                if (subscriptionResult.IsFailure)
                {
                    _logger.LogError("Failed to create subscription after successful payment. PaymentId={PaymentId}, Error={Error}",
                        payment.PaymentId, subscriptionResult.Error);
                    // Vẫn giữ payment Success, cần xử lý manual
                }

                _logger.LogInformation(
                    "VNPay IPN: Payment SUCCESS. PaymentId={PaymentId}, UserId={UserId}, TransactionNo={TransactionNo}",
                    payment.PaymentId, payment.UserId, transactionNo);

                // Gửi notification: Thanh toán thành công
                await _notificationService.SendSystemNotificationAsync(
                    payment.UserId,
                    "Thanh toán thành công",
                    "Thanh toán của bạn đã được xác nhận thành công. Gói subscription đã được kích hoạt!",
                    "payment",
                    payment.PaymentId);

                return Result<string>.Success("Payment processed successfully");
            }
            else
            {
                // THANH TOÁN THẤT BẠI
                await _paymentRepository.UpdateStatusAsync(payment.PaymentId, "Failed", transactionNo, responseCode);

                _logger.LogWarning(
                    "VNPay IPN: Payment FAILED. PaymentId={PaymentId}, ResponseCode={ResponseCode}",
                    payment.PaymentId, responseCode);

                // Gửi notification: Thanh toán thất bại
                await _notificationService.SendSystemNotificationAsync(
                    payment.UserId,
                    "Thanh toán thất bại",
                    "Thanh toán của bạn không thành công. Vui lòng thử lại hoặc liên hệ hỗ trợ.",
                    "payment",
                    payment.PaymentId);

                return Result<string>.Success("Payment failed - status updated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay IPN");
            return Result<string>.Failure("Internal server error");
        }
    }

    public async Task<Result<PaymentResponseDto>> ProcessVnPayReturnAsync(Dictionary<string, string> queryParams)
    {
        try
        {
            // 1. Lấy thông tin từ VNPay callback (KHÔNG verify chữ ký vì đã bỏ IpnUrl)
            var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "");
            var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
            var transactionNo = queryParams.GetValueOrDefault("vnp_TransactionNo", "");

            _logger.LogInformation(
                "VNPay Return: TxnRef={TxnRef}, ResponseCode={ResponseCode}, TransactionNo={TransactionNo}",
                txnRef, responseCode, transactionNo);

            // 2. Tìm payment theo OrderId
            var payment = await _paymentRepository.GetByOrderIdAsync(txnRef);
            if (payment == null)
                return Result<PaymentResponseDto>.NotFound("Không tìm thấy giao dịch");

            // 3. Idempotent: Nếu payment đã xử lý rồi, trả về kết quả hiện tại
            if (payment.Status != "Pending")
            {
                _logger.LogInformation(
                    "VNPay Return: Payment already processed. PaymentId={PaymentId}, Status={Status}",
                    payment.PaymentId, payment.Status);

                return Result<PaymentResponseDto>.Success(new PaymentResponseDto
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    CreatedAt = payment.CreatedAt
                });
            }

            // 4. Xử lý kết quả thanh toán
            if (responseCode == "00")
            {
                // THANH TOÁN THÀNH CÔNG → Update status + Tạo subscription + Cập nhật role
                await _paymentRepository.UpdateStatusAsync(payment.PaymentId, "Success", transactionNo, responseCode);

                var subscriptionResult = await _subscriptionService.CreateSubscriptionAsync(payment.UserId, payment.PlanId);
                if (subscriptionResult.IsFailure)
                {
                    _logger.LogError(
                        "Failed to create subscription after payment. PaymentId={PaymentId}, Error={Error}",
                        payment.PaymentId, subscriptionResult.Error);
                }
                else
                {
                    _logger.LogInformation(
                        "VNPay Return: Payment SUCCESS + Subscription created. PaymentId={PaymentId}, UserId={UserId}",
                        payment.PaymentId, payment.UserId);
                }

                // Gửi notification: Thanh toán thành công
                await _notificationService.SendSystemNotificationAsync(
                    payment.UserId,
                    "Thanh toán thành công",
                    "Thanh toán của bạn đã được xác nhận thành công. Gói subscription đã được kích hoạt!",
                    "payment",
                    payment.PaymentId);

                return Result<PaymentResponseDto>.Success(new PaymentResponseDto
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Status = "Success",
                    PaymentMethod = payment.PaymentMethod,
                    CreatedAt = payment.CreatedAt
                });
            }
            else
            {
                // THANH TOÁN THẤT BẠI
                await _paymentRepository.UpdateStatusAsync(payment.PaymentId, "Failed", transactionNo, responseCode);

                _logger.LogWarning(
                    "VNPay Return: Payment FAILED. PaymentId={PaymentId}, ResponseCode={ResponseCode}",
                    payment.PaymentId, responseCode);

                // Gửi notification: Thanh toán thất bại
                await _notificationService.SendSystemNotificationAsync(
                    payment.UserId,
                    "Thanh toán thất bại",
                    "Thanh toán của bạn không thành công. Vui lòng thử lại hoặc liên hệ hỗ trợ.",
                    "payment",
                    payment.PaymentId);

                return Result<PaymentResponseDto>.Success(new PaymentResponseDto
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Status = "Failed",
                    PaymentMethod = payment.PaymentMethod,
                    CreatedAt = payment.CreatedAt
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay return");
            return Result<PaymentResponseDto>.Failure("Lỗi xử lý kết quả thanh toán");
        }
    }

    public async Task<Result<IEnumerable<PaymentHistoryDto>>> GetPaymentHistoryAsync(Guid userId)
    {
        try
        {
            var payments = await _paymentRepository.GetByUserIdAsync(userId);
            var plans = await _planRepository.GetActivePlansAsync();
            var planDict = plans.ToDictionary(p => p.PlanId, p => p.PlanName);

            var history = payments.Select(p => new PaymentHistoryDto
            {
                PaymentId = p.PaymentId,
                OrderId = p.OrderId,
                PlanName = planDict.GetValueOrDefault(p.PlanId, "Unknown"),
                Amount = p.Amount,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt
            });

            return Result<IEnumerable<PaymentHistoryDto>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history for UserId={UserId}", userId);
            return Result<IEnumerable<PaymentHistoryDto>>.Failure("Lỗi khi lấy lịch sử thanh toán");
        }
    }

    public async Task<Result<PaymentResponseDto>> GetPaymentByIdAsync(Guid paymentId, Guid userId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return Result<PaymentResponseDto>.NotFound("Không tìm thấy giao dịch");

            // Security: Chỉ user của payment mới xem được
            if (payment.UserId != userId)
                return Result<PaymentResponseDto>.Forbidden("Bạn không có quyền xem giao dịch này");

            return Result<PaymentResponseDto>.Success(new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Status = payment.Status,
                PaymentUrl = payment.PaymentUrl ?? string.Empty,
                PaymentMethod = payment.PaymentMethod,
                CreatedAt = payment.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by ID. PaymentId={PaymentId}", paymentId);
            return Result<PaymentResponseDto>.Failure("Lỗi khi lấy thông tin giao dịch");
        }
    }
}
