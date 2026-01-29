using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

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
    Task<PagingResult<Song>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize);

    /// <summary>
    /// Thêm bài hát vào playlist
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns> </returns>
    Task AddSongToPlaylistAsync(Guid userId, Guid playlistId, Guid songId);

    /// <summary>
    /// Xóa bài hát khỏi playlist
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="playlistId">ID playlist</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns> </returns>
    Task RemoveSongFromPlaylistAsync(Guid userId, Guid playlistId, Guid songId);

    /// <summary>
    /// Theo dõi hoặc bỏ theo dõi nghệ sĩ
    /// </summary>
    /// <param name="IsFollowing">Trạng thái theo dõi/bỏ theo dõi</param>
    /// <param name="followerId">ID người theo dõi</param>
    /// <param name="followingId">ID nghệ sĩ được theo dõi</param>
    /// <returns>Trạng thái theo dõi và thông báo</returns>
    Task<(bool IsFollowing, string Message)> ToggleFollowAsync(Guid followerId, Guid followingId);

    /// <summary>
    /// Lấy danh sách nghệ sĩ đang theo dõi của người dùng với phân trang
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách nghệ sĩ đang theo dõi phân trang</returns>
    Task<PagingResult<ArtistDto>> GetFollowingsAsync(Guid userId, int pageIndex, int pageSize);
}
