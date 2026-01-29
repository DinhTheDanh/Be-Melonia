using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IPlaylistRepository : IBaseRepository<Playlist>
{
    /// <summary>
    /// Lấy danh sách playlist của người dùng với phân trang
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên playlist</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách playlist phân trang</returns>
    Task<PagingResult<PlaylistDto>> GetUserPlaylistsAsync(Guid userId, string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách tất cả playlist (công khai)
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên playlist</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách playlist phân trang</returns>
    Task<PagingResult<PlaylistDto>> GetAllPlaylistsAsync(string keyword, int pageIndex, int pageSize);
}

