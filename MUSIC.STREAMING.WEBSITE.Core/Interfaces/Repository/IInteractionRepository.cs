using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IInteractionRepository
{
    /// <summary>
    /// Thêm hoặc xóa lượt thích của người dùng đối với bài hát
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="songId">ID của bài hát</param>
    /// <returns>True nếu người dùng đã thích bài hát, ngược lại là False</returns>
    Task<bool> ToggleLikeAsync(Guid userId, Guid songId);

    /// <summary>
    /// Lấy danh sách bài hát mà người dùng đã thích, có phân trang
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="pageIndex">Số trang</param>
    /// <param name="pageSize">Số lượng bản ghi trên mỗi trang</param>
    /// <returns> Kết quả phân trang</returns>
    Task<PagingResult<Song>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize);

    /// <summary>
    /// Thêm bài hát vào danh sách phát
    /// </summary>
    /// <param name="playlistId">ID của danh sách phát</param>
    /// <param name="songId">ID của bài hát</param>
    /// <returns> Bài hát đã được thêm vào danh sách phát</returns>
    Task AddSongToPlaylistAsync(Guid playlistId, Guid songId);

    /// <summary>
    /// Xóa bài hát khỏi danh sách phát
    /// </summary>
    /// <param name="playlistId">ID của danh sách phát</param>
    /// <param name="songId">ID của bài hát</param>
    /// <returns></returns>
    Task RemoveSongFromPlaylistAsync(Guid playlistId, Guid songId);

    /// <summary>
    /// Thêm hoặc xóa theo dõi giữa hai người dùng
    /// </summary>
    /// <param name="followerId">ID của người theo dõi</param>
    /// <param name="followingId">ID của người được theo dõi</param>
    /// <returns></returns>
    Task<bool> ToggleFollowAsync(Guid followerId, Guid followingId);

    /// <summary>
    /// Lấy danh sách người dùng mà người dùng đang theo dõi
    /// </summary>
    /// <param name="followerId">ID của người theo dõi</param>
    /// <param name="pageIndex">Số trang</param>
    /// <param name="pageSize">Số lượng bản ghi trên mỗi trang</param>
    /// <returns>Kết quả phân trang</returns>
    Task<PagingResult<User>> GetFollowingsAsync(Guid followerId, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách người dùng đang theo dõi người dùng
    /// </summary>
    /// <param name="followingId">ID của người được theo dõi</param>
    /// <param name="pageIndex">Số trang</param>
    /// <param name="pageSize">Số lượng bản ghi trên mỗi trang</param>
    /// <returns>Kết quả phân trang</returns>
    Task<PagingResult<User>> GetFollowersAsync(Guid followingId, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy chi tiết playlist kèm danh sách bài hát
    /// </summary>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Thông tin playlist và danh sách bài hát</returns>
    Task<dynamic> GetPlaylistDetailsAsync(Guid playlistId, int pageIndex, int pageSize);

    /// <summary>
    /// Xóa bài hát khỏi album
    /// </summary>
    /// <param name="userId">ID người dùng (artist/admin)</param>
    /// <param name="albumId">ID album</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns></returns>
    Task RemoveSongFromAlbumAsync(Guid userId, Guid albumId, Guid songId);
}
