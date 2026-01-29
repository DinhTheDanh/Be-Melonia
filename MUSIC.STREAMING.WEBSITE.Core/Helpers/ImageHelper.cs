using System;
using System.Net;

namespace MUSIC.STREAMING.WEBSITE.Core.Helpers;

public class ImageHelper
{
    public static string GenerateAvatar(string name)
    {
        var encodedName = WebUtility.UrlEncode(name);
        return $"https://ui-avatars.com/api/?name={encodedName}&background=random&color=fff&size=256&bold=true";
    }

    public static string GenerateCover(string title, string seedInfo = "")
    {
        // Kết hợp Title + Seed để ảnh không bị trùng lặp
        var seed = WebUtility.UrlEncode(title + seedInfo);

        // Dùng DiceBear style "shapes" 
        return $"https://api.dicebear.com/7.x/shapes/svg?seed={seed}";

    }
}
