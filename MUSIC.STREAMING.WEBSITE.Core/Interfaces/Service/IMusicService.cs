using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IMusicService
{
    /// <summary>
    /// Tạo bài hát mới cho nghệ sĩ
    /// </summary>
    /// <param name="artistId">ID nghệ sĩ</param>
    /// <param name="dto">Thông tin bài hát</param>
    /// <returns>Bài hát đã tạo</returns>
    Task<Song> CreateSongAsync(Guid artistId, CreateSongDto dto);

    /// <summary>
    /// Tạo album mới cho nghệ sĩ
    /// </summary>
    /// <param name="artistId">ID nghệ sĩ</param>
    /// <param name="dto">Thông tin album</param>
    /// <returns>Album đã tạo</returns>
    Task<Album> CreateAlbumAsync(Guid artistId, CreateAlbumDto dto);

    /// <summary>
    /// Tạo playlist mới cho người dùng
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="dto">Thông tin playlist</param>
    /// <returns>Playlist đã tạo</returns>
    Task<Playlist> CreatePlaylistAsync(Guid userId, CreatePlaylistDto dto);

    /// <summary>
    /// Lấy danh sách bài hát theo từ khóa với phân trang
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách bài hát phân trang</returns>
    Task<PagingResult<SongDto>> GetAllSongsAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách bài hát của nghệ sĩ với phân trang
    /// </summary>
    /// <param name="artistId">ID nghệ sĩ</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách bài hát phân trang</returns>
    Task<PagingResult<SongDto>> GetSongsByArtistAsync(Guid artistId, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách thể loại
    /// </summary>
    /// <returns>Danh sách thể loại</returns>
    Task<IEnumerable<GenreDto>> GetAllGenresAsync();

    /// <summary>
    /// Lấy danh sách album với phân trang
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên album</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách album phân trang</returns>
    Task<PagingResult<AlbumDto>> GetAlbumsAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Tạo thể loại âm nhạc
    /// </summary>
    /// <param name="dto">DTO tạo thể loại</param>
    /// <returns></returns>
    Task<Genre> CreateGenreAsync(CreateGenreDto dto);

    /// <summary>
    /// Check file trùng
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    Task<Song?> CheckFileHashAsync(string hash);

    /// <summary>
    /// Lấy danh sách bài hát của người dùng (nghệ sĩ) hiện tại
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách bài hát phân trang</returns>
    Task<PagingResult<SongDto>> GetUserSongsAsync(Guid userId, string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách album của người dùng (nghệ sĩ) hiện tại
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách album phân trang</returns>
    Task<PagingResult<AlbumDto>> GetUserAlbumsAsync(Guid userId, string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách playlist của người dùng hiện tại
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách playlist phân trang</returns>
    Task<PagingResult<PlaylistDto>> GetUserPlaylistsAsync(Guid userId, string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách tất cả playlist (công khai)
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách playlist phân trang</returns>
    Task<PagingResult<PlaylistDto>> GetAllPlaylistsAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Xóa bài hát
    /// </summary>
    /// <param name="artistId">ID nghệ sĩ (chủ sở hữu)</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns></returns>
    Task DeleteSongAsync(Guid artistId, Guid songId);

    /// <summary>
    /// Xóa album
    /// </summary>
    /// <param name="artistId">ID nghệ sĩ (chủ sở hữu)</param>
    /// <param name="albumId">ID album</param>
    /// <returns></returns>
    Task DeleteAlbumAsync(Guid artistId, Guid albumId);
}
