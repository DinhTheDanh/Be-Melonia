using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class VnPayService : IVnPayService
{
    private readonly VnPayConfig _config;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(IConfiguration configuration, ILogger<VnPayService> logger)
    {
        _logger = logger;
        _config = new VnPayConfig();
        configuration.GetSection("VnPay").Bind(_config);

        if (string.IsNullOrEmpty(_config.TmnCode) || string.IsNullOrEmpty(_config.HashSecret))
        {
            throw new InvalidOperationException("VNPay configuration is missing. Please check appsettings.json");
        }
    }

    public string CreatePaymentUrl(Guid paymentId, string orderId, long amount, string orderInfo, string ipAddress)
    {
        var vnpayData = new SortedDictionary<string, string>
        {
            { "vnp_Amount", (amount * 100).ToString() },
            { "vnp_Command", _config.Command },
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", _config.CurrCode },
            { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") },
            { "vnp_IpAddr", ipAddress },
            { "vnp_Locale", _config.Locale },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "billpayment" },
            { "vnp_ReturnUrl", _config.ReturnUrl },
            { "vnp_TmnCode", _config.TmnCode },
            { "vnp_TxnRef", orderId },
            { "vnp_Version", _config.Version }
        };

        // Build URL-encoded query string (sorted A-Z)
        var queryString = BuildQueryString(vnpayData);

        // Ký HMAC-SHA512 trên chuỗi URL-encoded
        var secureHash = HmacSha512(_config.HashSecret, queryString);

        var paymentUrl = $"{_config.BaseUrl}?{queryString}&vnp_SecureHash={secureHash}";

        _logger.LogInformation("VNPay SignData: {SignData}", queryString);
        _logger.LogInformation("VNPay SecureHash ({Length} chars): {Hash}", secureHash.Length, secureHash);
        _logger.LogInformation("Created VNPay payment URL for OrderId: {OrderId}, PaymentId: {PaymentId}",
            orderId, paymentId);

        return paymentUrl;
    }

    public bool ValidateSignature(Dictionary<string, string> queryParams)
    {
        try
        {
            var vnpayData = new Dictionary<string, string>();
            string secureHash = string.Empty;

            foreach (var (key, value) in queryParams)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    if (key == "vnp_SecureHash" || key == "vnp_SecureHashType")
                    {
                        if (key == "vnp_SecureHash")
                            secureHash = value;
                        continue;
                    }
                    vnpayData[key] = value;
                }
            }

            return ValidateSignature(vnpayData, secureHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VNPay signature");
            return false;
        }
    }

    public bool ValidateSignature(Dictionary<string, string> vnpayData, string secureHash)
    {
        try
        {
            if (string.IsNullOrEmpty(secureHash))
            {
                _logger.LogWarning("VNPay secure hash is empty");
                return false;
            }

            // Sort theo key A-Z
            var sortedData = new SortedDictionary<string, string>(vnpayData);

            // Build URL-encoded query string
            var signData = BuildQueryString(sortedData);

            // Ký HMAC-SHA512 trên chuỗi URL-encoded
            var computedHash = HmacSha512(_config.HashSecret, signData);

            _logger.LogInformation("VNPay Validate - SignData: {SignData}", signData);
            _logger.LogInformation("VNPay Validate - ComputedHash: {Hash}", computedHash);
            _logger.LogInformation("VNPay Validate - ReceivedHash: {Hash}", secureHash);

            // So sánh an toàn - tránh timing attack
            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash.ToLower()),
                Encoding.UTF8.GetBytes(secureHash.ToLower())
            );

            if (!isValid)
            {
                _logger.LogWarning("VNPay signature validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VNPay signature");
            return false;
        }
    }

    /// <summary>
    /// Build URL-encoded query string từ sorted dictionary
    /// Key: URL-encode, Value: URL-encode
    /// Bỏ qua entry có value null/empty
    /// </summary>
    private static string BuildQueryString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in data)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(WebUtility.UrlEncode(key));
                sb.Append('=');
                sb.Append(WebUtility.UrlEncode(value));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Tạo HMAC-SHA512 hash — luôn trả về đúng 128 hex chars
    /// </summary>
    private static string HmacSha512(string key, string inputData)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        var sb = new StringBuilder(128);
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
