using System;
using Microsoft.AspNetCore.Http;
namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface ICloudinaryService
{
    /// <summary>
    /// Gửi ảnh lên Cloudinary và trả về URL của ảnh đã tải lên
    /// </summary>
    /// <param name="file">Tệp ảnh cần tải lên</param>
    /// <returns>URL của ảnh đã tải lên</returns>
    Task<string> UploadImageAsync(IFormFile file);

    /// <summary>
    /// Gửi tệp âm thanh lên Cloudinary và trả về URL của tệp đã tải lên
    /// </summary>
    /// <param name="file">Tệp âm thanh cần tải lên</param>
    /// <returns>URL của tệp âm thanh đã tải lên</returns>
    Task<string> UploadAudioAsync(IFormFile file);

    /// <summary> 
    /// </summary>
    /// <returns></returns>
    object GetMediaUploadSignature();
}
