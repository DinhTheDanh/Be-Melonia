using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IUserService
{

    /// <summary>
    /// Lấy thông tin hồ sơ người dùng
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <returns>Thông tin hồ sơ người dùng</returns>
    Task<UserProfileDto> GetProfileAsync(Guid userId);

    /// <summary>
    /// Cập nhật thông tin hồ sơ người dùng
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="dto">Thông tin hồ sơ người dùng</param>
    /// <returns></returns>
    Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto);

    /// <summary>
    /// Cập nhật sở thích thể loại của người dùng
    /// </summary>
    /// <param name="userId">ID người dùng</param>
    /// <param name="genreIds">Danh sách ID thể loại</param>
    /// <returns></returns>
    Task UpdateInterestsAsync(Guid userId, List<Guid> genreIds);

    /// <summary>
    /// Lấy danh sách nghệ sĩ theo từ khóa với phân trang
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="pageIndex">Chỉ mục trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Danh sách nghệ sĩ phân trang</returns>
    Task<PagingResult<ArtistDto>> GetArtistsAsync(string? keyword, int pageIndex, int pageSize);
}
