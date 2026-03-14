using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Helpers;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IInteractionService
{
    /// <summary>
    /// Thích hoặc bỏ thích bài hát
    /// </summary>
    /// <param name="IsLiked">Trạng thái thích/bỏ thích</param>
    /// <param name="userId">ID người dùng</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns>Trạng thái thích và thông báo</returns>
    Task<(bool IsLiked, string Message)> ToggleLikeAsync(Guid userId, Guid songId);

    /// <summary>
    /// Lấy danh sách bài hát đã thích của người dùng với phân trang
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách bài hát đã thích phân trang</returns>
    Task<PagingResult<SongDto>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize);

    /// <summary>
    /// Thêm bài hát vào playlist
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns>Kết quả xử lý</returns>
    Task<Result> AddSongToPlaylistAsync(Guid userId, Guid playlistId, Guid songId);

    /// <summary>
    /// Xóa bài hát khỏi playlist
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns>Kết quả xử lý</returns>
    Task<Result> RemoveSongFromPlaylistAsync(Guid userId, Guid playlistId, Guid songId);

    /// <summary>
    /// Theo dõi hoặc bỏ theo dõi nghệ sĩ
    /// </summary>
    /// <param name="IsFollowing">Trạng thái theo dõi/bỏ theo dõi</param>
    /// <param name="followerId">ID người theo dõi</param>
    /// <param name="followingId">ID nghệ sĩ được theo dõi</param>
    /// <returns>Kết quả với trạng thái theo dõi và thông báo</returns>
    Task<Result<(bool IsFollowing, string Message)>> ToggleFollowAsync(Guid followerId, Guid followingId);

    /// <summary>
    /// Lấy danh sách nghệ sĩ đang theo dõi của người dùng với phân trang
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách nghệ sĩ đang theo dõi phân trang</returns>
    Task<PagingResult<ArtistDto>> GetFollowingsAsync(Guid userId, int pageIndex, int pageSize);

    /// <summary>
    /// Cập nhật thông tin playlist
    /// </summary>
    /// <param name="userId">ID người dùng (chủ playlist)</param>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="dto">Dữ liệu cập nhật</param>
    /// <returns>Kết quả với Playlist đã cập nhật</returns>
    Task<Result<Playlist>> UpdatePlaylistAsync(Guid userId, Guid playlistId, UpdatePlaylistDto dto);

    /// <summary>
    /// Chuyển đổi trạng thái công khai/riêng tư của playlist
    /// </summary>
    /// <param name="userId">ID người dùng (chủ playlist)</param>
    /// <param name="playlistId">ID playlist</param>
    /// <returns>Kết quả với trạng thái mới</returns>
    Task<Result<(bool IsPublic, string Message)>> TogglePlaylistVisibilityAsync(Guid userId, Guid playlistId);

    /// <summary>
    /// Xóa playlist
    /// </summary>
    /// <param name="userId">ID người dùng (chủ playlist)</param>
    /// <param name="playlistId">ID playlist</param>
    /// <returns>Kết quả xử lý</returns>
    Task<Result> DeletePlaylistAsync(Guid userId, Guid playlistId);

    /// <summary>
    /// Lấy chi tiết playlist kèm danh sách bài hát
    /// </summary>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Thông tin playlist và danh sách bài hát</returns>
    Task<PlaylistDetailsDto?> GetPlaylistDetailsAsync(Guid playlistId, int pageIndex, int pageSize);

    /// <summary>
    /// Ghi nhận lượt nghe bài hát (play count + listening history)
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="dto">Dữ liệu lượt nghe</param>
    /// <returns>Kết quả xử lý</returns>
    Task<Result> RecordPlayAsync(Guid userId, RecordPlayDto dto);

    /// <summary>
    /// Xóa bài hát khỏi album
    /// </summary>
    /// <param name="userId">ID người dùng (artist/admin)</param>
    /// <param name="albumId">ID album</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns></returns>
    Task RemoveSongFromAlbumAsync(Guid userId, Guid albumId, Guid songId);
}
