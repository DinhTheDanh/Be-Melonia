using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User không tồn tại");

        return new UserProfileDto
        {
            UserId = user.UserId,
            Email = user.Email,
            Username = user.Username,
            FullName = user.FullName,
            Bio = user.Bio,
            Avatar = user.Avatar,
            Role = user.Role
        };
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        // Lấy user hiện tại từ DB lên
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("Không tìm thấy người dùng.");

        // Cập nhật các trường thông tin
        user.FullName = dto.FullName;
        user.Bio = dto.Bio;
        user.ArtistType = dto.ArtistType;

        // Chỉ cập nhật ảnh nếu Frontend có gửi link mới lên
        // (Nếu gửi chuỗi rỗng hoặc null nghĩa là họ không đổi ảnh)
        if (!string.IsNullOrEmpty(dto.Avatar))
        {
            user.Avatar = dto.Avatar;
        }

        if (!string.IsNullOrEmpty(dto.Banner))
        {
            user.Banner = dto.Banner;
        }

        user.UpdatedAt = DateTime.Now;

        // Lưu xuống DB
        await _userRepository.UpdateAsync(user.UserId, user);
    }

    public async Task UpdateInterestsAsync(Guid userId, List<Guid> genreIds)
    {
        if (genreIds == null)
        {
            genreIds = new List<Guid>();
        }

        await _userRepository.AddUserFavoriteGenresAsync(userId, genreIds);
    }

    public async Task<PagingResult<ArtistDto>> GetArtistsAsync(string keyword, int pageIndex, int pageSize)
    {
        var result = await _userRepository.GetArtistsPagingAsync(keyword, pageIndex, pageSize);

        var artistDtos = result.Data.Select(u => new ArtistDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Avatar = u.Avatar,
            Bio = u.Bio,
            ArtistType = u.ArtistType
        });

        return new PagingResult<ArtistDto>
        {
            Data = artistDtos,
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            FromRecord = result.FromRecord,
            ToRecord = result.ToRecord
        };
    }
}
