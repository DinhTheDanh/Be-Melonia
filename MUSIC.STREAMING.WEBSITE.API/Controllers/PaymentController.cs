using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.API.Extensions;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo thanh toán mới - Trả về URL QR VNPay để frontend redirect
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var ipAddress = GetClientIpAddress();

        var result = await _paymentService.CreatePaymentAsync(userId, dto, ipAddress);
        return result.ToActionResult();
    }

    /// <summary>
    /// VNPay IPN (Instant Payment Notification) - Webhook VNPay gọi về
    /// KHÔNG CẦN AUTHORIZE - VNPay gọi trực tiếp
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn()
    {
        _logger.LogInformation("VNPay IPN received: {QueryString}", Request.QueryString);

        var queryDict = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
        var result = await _paymentService.ProcessVnPayIpnAsync(queryDict);

        // VNPay yêu cầu response theo format cụ thể
        if (result.IsSuccess)
        {
            return Ok(new
            {
                RspCode = "00",
                Message = "Confirm Success"
            });
        }

        // Trả mã lỗi cho VNPay
        var rspCode = result.Type switch
        {
            ResultType.NotFound => "01",      // Order not found
            ResultType.Unauthorized => "97",   // Invalid checksum
            _ => "99"                          // Other error
        };

        return Ok(new
        {
            RspCode = rspCode,
            Message = result.Error ?? "Unknown error"
        });
    }

    /// <summary>
    /// VNPay Return URL - Redirect user về sau khi thanh toán
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn()
    {
        _logger.LogInformation("VNPay Return received: {QueryString}", Request.QueryString);

        var queryDict = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
        var result = await _paymentService.ProcessVnPayReturnAsync(queryDict);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy lịch sử thanh toán của user
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _paymentService.GetPaymentHistoryAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy chi tiết 1 giao dịch
    /// </summary>
    [HttpGet("{paymentId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _paymentService.GetPaymentByIdAsync(paymentId, userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy IP client, hỗ trợ proxy/load balancer
    /// </summary>
    private string GetClientIpAddress()
    {
        // Check X-Forwarded-For header first (proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check X-Real-IP header
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress switch
        {
            null => "127.0.0.1",
            var ip when ip.ToString() == "::1" => "127.0.0.1",
            var ip when ip.IsIPv6LinkLocal => "127.0.0.1",
            var ip => ip.MapToIPv4().ToString()
        };
    }
}
