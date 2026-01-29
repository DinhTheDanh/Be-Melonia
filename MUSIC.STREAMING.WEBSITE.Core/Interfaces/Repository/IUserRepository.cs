using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IUserRepository : IBaseRepository<User>

{
    /// <summary>
    /// Lấy người dùng theo email
    /// </summary>
    /// <param name="email">Email của người dùng</param>
    /// <returns>Người dùng tìm thấy</returns>
    Task<User> GetByEmailAsync(string email);

    /// <summary>
    /// Lấy người dùng theo username
    /// </summary>
    /// <param name="username">Tên người dùng</param>
    /// <returns>Người dùng tìm thấy</returns>
    Task<User> GetByUsernameAsync(string username); // Hàm check trùng username

    /// <summary>
    /// Lấy người dùng theo username hoặc email
    /// </summary>
    /// <param name="identifier">Tên người dùng hoặc email</param>
    /// <returns>Người dùng tìm thấy</returns>
    Task<User> GetByUsernameOrEmailAsync(string identifier); // Hàm dùng để Login


    /// <summary>
    /// Lấy người dùng theo token reset mật khẩu
    /// </summary>
    /// <param name="token">Token reset mật khẩu</param>
    /// <returns>Người dùng tìm thấy</returns>
    Task<User?> GetByResetTokenAsync(string token);

    /// <summary>
    /// Lấy danh sách nghệ sĩ có phân trang
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách nghệ sĩ phân trang</returns>
    Task<PagingResult<User>> GetArtistsPagingAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Thêm thể loại yêu thích cho người dùng
    /// </summary>
    /// <param name="userId">ID của người dùng</param>
    /// <param name="genreIds">Danh sách ID thể loại yêu thích</param>
    /// <returns> </returns>
    Task AddUserFavoriteGenresAsync(Guid userId, List<Guid> genreIds);
}
