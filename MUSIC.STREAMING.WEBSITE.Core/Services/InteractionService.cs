using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class InteractionService : IInteractionService
{
    private readonly IInteractionRepository _interactionRepo;
    private readonly IBaseRepository<Playlist> _playlistRepo;

    public InteractionService(IInteractionRepository interactionRepo, IBaseRepository<Playlist> playlistRepo)
    {
        _interactionRepo = interactionRepo;
        _playlistRepo = playlistRepo;
    }

    public async Task<(bool IsLiked, string Message)> ToggleLikeAsync(Guid userId, Guid songId)
    {
        // Gọi Repo thực hiện like/unlike
        bool isLiked = await _interactionRepo.ToggleLikeAsync(userId, songId);

        return (isLiked, isLiked ? "Đã thích bài hát" : "Đã bỏ thích bài hát");
    }

    public async Task<PagingResult<Song>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize)
    {
        return await _interactionRepo.GetLikedSongsAsync(userId, pageIndex, pageSize);
    }

    public async Task AddSongToPlaylistAsync(Guid userId, Guid playlistId, Guid songId)
    {
        // 1. Kiểm tra Playlist có tồn tại không
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null)
        {
            throw new Exception("Playlist không tồn tại.");
        }

        // 2. Kiểm tra User có phải chủ sở hữu Playlist không (Logic nghiệp vụ)
        if (playlist.UserId != userId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa playlist này.");
        }

        // 3. Gọi Repo thêm vào DB
        await _interactionRepo.AddSongToPlaylistAsync(playlistId, songId);
    }

    public async Task RemoveSongFromPlaylistAsync(Guid userId, Guid playlistId, Guid songId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) throw new Exception("Playlist không tồn tại.");

        if (playlist.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa playlist này.");

        await _interactionRepo.RemoveSongFromPlaylistAsync(playlistId, songId);
    }

    public async Task<(bool IsFollowing, string Message)> ToggleFollowAsync(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
        {
            throw new Exception("Bạn không thể tự theo dõi chính mình.");
        }

        bool isFollowing = await _interactionRepo.ToggleFollowAsync(followerId, followingId);

        return (isFollowing, isFollowing ? "Đã theo dõi" : "Đã hủy theo dõi");
    }

    public async Task<PagingResult<ArtistDto>> GetFollowingsAsync(Guid userId, int pageIndex, int pageSize)
    {
        var result = await _interactionRepo.GetFollowingsAsync(userId, pageIndex, pageSize);

        var dtos = result.Data.Select(u => new ArtistDto
        {
            UserId = u.UserId,
            FullName = u.FullName ?? u.Username,
            Avatar = u.Avatar,
            Bio = u.Bio,
            ArtistType = u.ArtistType
        });

        return new PagingResult<ArtistDto>
        {
            Data = dtos,
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            FromRecord = result.FromRecord,
            ToRecord = result.ToRecord
        };
    }

    public async Task<Playlist> UpdatePlaylistAsync(Guid userId, Guid playlistId, string title)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) throw new Exception("Playlist không tồn tại.");
        if (playlist.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa playlist này.");

        playlist.Title = title;
        await _playlistRepo.UpdateAsync(playlistId, playlist);
        return playlist;
    }

    public async Task DeletePlaylistAsync(Guid userId, Guid playlistId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) throw new Exception("Playlist không tồn tại.");
        if (playlist.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền xóa playlist này.");

        await _playlistRepo.DeleteAsync(playlistId);
    }

    public async Task<dynamic> GetPlaylistDetailsAsync(Guid playlistId, int pageIndex, int pageSize)
    {
        return await _interactionRepo.GetPlaylistDetailsAsync(playlistId, pageIndex, pageSize);
    }

    public async Task RemoveSongFromAlbumAsync(Guid userId, Guid albumId, Guid songId)
    {
        await _interactionRepo.RemoveSongFromAlbumAsync(userId, albumId, songId);
    }
}

