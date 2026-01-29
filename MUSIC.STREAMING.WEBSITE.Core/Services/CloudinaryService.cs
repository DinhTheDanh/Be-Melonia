using System;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly string _apiKey;

    public CloudinaryService(IConfiguration config)
    {

        // Lấy thông tin từ appsettings.json
        _cloudName = config["CloudinarySettings:CloudName"];
        _apiKey = config["CloudinarySettings:ApiKey"];
        var apiSecret = config["CloudinarySettings:ApiSecret"];

        var account = new Account(_cloudName, _apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new Exception("File không tồn tại.");
        }

        var uploadResult = new ImageUploadResult();

        using (var stream = file.OpenReadStream())
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
            };

            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }
        if (uploadResult.Error != null)
        {
            throw new Exception(uploadResult.Error.Message);
        }

        // Trả về đường dẫn ảnh tuyệt đối (https://...)
        return uploadResult.SecureUrl.ToString();
    }

    public async Task<string> UploadAudioAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new Exception("File không tồn tại.");
        }

        // Kiểm tra định dạng file (Chỉ cho phép mp3, wav, ogg)
        var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            throw new Exception("Định dạng file không hỗ trợ. Vui lòng tải lên .mp3, .wav, hoặc .m4a");
        }

        var uploadResult = new VideoUploadResult(); // Dùng VideoUploadResult cho Audio

        using (var stream = file.OpenReadStream())
        {
            var uploadParams = new VideoUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "music-streaming/songs"   // Gom vào 1 thư mục cho gọn
            };

            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }

        if (uploadResult.Error != null)
        {
            throw new Exception(uploadResult.Error.Message);
        }

        return uploadResult.SecureUrl.ToString();
    }
    public object GetMediaUploadSignature()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var parameters = new SortedDictionary<string, object>
        {
            { "timestamp", timestamp },
            { "folder", "music-streaming/songs" },
        };
        string signature = _cloudinary.Api.SignParameters(parameters);

        return new
        {
            CloudName = _cloudName,
            ApiKey = _apiKey,
            Timestamp = timestamp,
            Signature = signature,
            Folder = "music-streaming/songs" // Trả về folder nếu có dùng
        };
    }
}
