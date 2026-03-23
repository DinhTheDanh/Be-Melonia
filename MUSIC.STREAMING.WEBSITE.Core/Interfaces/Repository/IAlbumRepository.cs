using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IAlbumRepository : IBaseRepository<Album>
{
    /// <summary>
    /// Lấy danh sách Album kèm theo thông tin Artist, có phân trang
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên album</param>
    /// <param name="pageIndex">Số trang</param>
    /// <param name="pageSize">Số lượng bản ghi trên mỗi trang</param>
    /// <returns></returns>
    Task<PagingResult<AlbumDto>> GetAlbumsWithArtistAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách album của người dùng (nghệ sĩ) với phân trang
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên album</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách album phân trang</returns>
    Task<PagingResult<AlbumDto>> GetUserAlbumsAsync(Guid userId, string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy chi tiết album kèm danh sách bài hát
    /// </summary>
    /// <param name="albumId">ID album</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Thông tin album và danh sách bài hát</returns>
    Task<AlbumDetailsDto?> GetAlbumDetailsAsync(Guid albumId, int pageIndex, int pageSize);

    /// <summary>
    /// Thêm bài hát vào album
    /// </summary>
    /// <param name="albumId">ID album</param>
    /// <param name="songId">ID bài hát</param>
    Task AddSongToAlbumAsync(Guid albumId, Guid songId);

    /// <summary>
    /// Kiểm tra quyền sở hữu album
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="albumId">ID album</param>
    /// <returns>true nếu là chủ sở hữu</returns>
    Task<bool> CheckAlbumOwnerAsync(Guid userId, Guid albumId);

    /// <summary>
    /// Lấy danh sách album theo danh sách ID
    /// </summary>
    /// <param name="albumIds">Danh sách ID album</param>
    /// <returns>Danh sách AlbumDto</returns>
    Task<List<AlbumDto>> GetAlbumsByIdsAsync(List<Guid> albumIds);

    /// <summary>
    /// Lấy danh sách album phổ biến theo khung thời gian
    /// </summary>
    /// <param name="windowType">Khung thời gian: 1d, 7d, 28d, all</param>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên album</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách album phổ biến phân trang</returns>
    Task<PagingResult<PopularAlbumDto>> GetPopularAlbumsAsync(string windowType, string keyword, int pageIndex, int pageSize);
}

