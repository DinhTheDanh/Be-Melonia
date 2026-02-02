using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface ISongRepository : IBaseRepository<Song>
{
    /// <summary>
    /// Lấy danh sách bài hát theo Album ID
    /// </summary>
    /// <param name="albumId">ID của album</param>
    /// <returns>Danh sách bài hát thuộc album đó</returns>
    Task<IEnumerable<Song>> GetByAlbumIdAsync(Guid albumId);

    /// <summary>
    /// Lấy danh sách bài hát theo Artist ID
    /// </summary>
    /// <param name="artistId">ID của nghệ sĩ</param>
    /// <returns>Danh sách bài hát của nghệ sĩ đó</returns>
    Task<IEnumerable<Song>> GetByArtistIdAsync(Guid artistId);

    /// <summary>
    /// Lấy danh sách bài hát theo Playlist ID
    /// </summary>
    /// <param name="playlistId">ID của danh sách phát</param>
    /// <returns>Danh sách bài hát trong danh sách phát đó</returns>
    Task<IEnumerable<Song>> GetByPlaylistIdAsync(Guid playlistId);

    /// <summary>
    /// Thêm nghệ sĩ vào bài hát
    /// </summary>
    /// <param name="songId">ID của bài hát</param>
    /// <param name="artistIds">Danh sách ID của nghệ sĩ</param>
    /// <returns></returns>
    Task AddArtistsToSongAsync(Guid songId, List<Guid> artistIds);

    /// <summary>
    /// Thêm thể loại vào bài hát
    /// </summary>
    /// <param name="songId">ID của bài hát</param>
    /// <param name="genreIds">Danh sách ID của thể loại</param>
    /// <returns> </returns>
    Task AddGenresToSongAsync(Guid songId, List<Guid> genreIds);

    /// <summary>
    /// Lấy danh sách bài hát theo từ khóa tìm kiếm
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ số trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách bài hát theo từ khóa tìm kiếm</returns>
    Task<PagingResult<SongDto>> GetAllSongsWithArtistAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy danh sách bài hát theo Artist ID, có phân trang
    /// </summary>
    /// <param name="artistId">ID của nghệ sĩ</param>
    /// <param name="pageIndex">Chỉ số trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns></returns>
    Task<PagingResult<SongDto>> GetSongsByArtistIdAsync(Guid artistId, int pageIndex, int pageSize);

    Task<Song?> GetByFileHashAsync(string hash);

    /// <summary>
    /// Lấy danh sách bài hát của người dùng (nghệ sĩ) với phân trang
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="keyword">Từ khóa tìm kiếm theo tên bài hát</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách bài hát phân trang</returns>
    Task<PagingResult<SongDto>> GetUserSongsAsync(Guid userId, string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Kiểm tra artist có phải chủ sở hữu bài hát không
    /// </summary>
    /// <param name="artistId">ID artist</param>
    /// <param name="songId">ID bài hát</param>
    /// <returns>True nếu artist là chủ sở hữu, ngược lại False</returns>
    Task<bool> CheckSongOwnerAsync(Guid artistId, Guid songId);

    /// <summary>
    /// Xóa tất cả thể loại của bài hát
    /// </summary>
    /// <param name="songId">ID bài hát</param>
    /// <returns></returns>
    Task RemoveGenresFromSongAsync(Guid songId);
}

