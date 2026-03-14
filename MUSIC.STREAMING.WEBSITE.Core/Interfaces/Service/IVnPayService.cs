namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IVnPayService
{
    /// <summary>
    /// Tạo URL thanh toán VNPay
    /// </summary>
    /// <param name="paymentId">ID payment</param>
    /// <param name="orderId">Mã đơn hàng</param>
    /// <param name="amount">Số tiền (VND)</param>
    /// <param name="orderInfo">Mô tả đơn hàng</param>
    /// <param name="ipAddress">IP người dùng</param>
    /// <returns>URL thanh toán</returns>
    string CreatePaymentUrl(Guid paymentId, string orderId, long amount, string orderInfo, string ipAddress);

    /// <summary>
    /// Xác thực chữ ký bảo mật VNPay callback từ query params dictionary
    /// </summary>
    /// <param name="queryParams">Tất cả vnp_ params (key-value)</param>
    /// <returns>True nếu chữ ký hợp lệ</returns>
    bool ValidateSignature(Dictionary<string, string> queryParams);

    /// <summary>
    /// Xác thực chữ ký từ dictionary params với secure hash riêng
    /// </summary>
    bool ValidateSignature(Dictionary<string, string> vnpayData, string secureHash);
}
