using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Helpers;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using StackExchange.Redis;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class InteractionService : IInteractionService
{
    private readonly IInteractionRepository _interactionRepo;
    private readonly IBaseRepository<Playlist> _playlistRepo;
    private readonly IConnectionMultiplexer _redis;

    public InteractionService(IInteractionRepository interactionRepo, IBaseRepository<Playlist> playlistRepo, IConnectionMultiplexer redis)
    {
        _interactionRepo = interactionRepo;
        _playlistRepo = playlistRepo;
        _redis = redis;
    }

    public async Task<(bool IsLiked, string Message)> ToggleLikeAsync(Guid userId, Guid songId)
    {
        bool isLiked = await _interactionRepo.ToggleLikeAsync(userId, songId);
        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: $"liked_songs:{userId}:*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException) { }
        return (isLiked, isLiked ? "Đã thích bài hát" : "Đã bỏ thích bài hát");
    }

    public async Task<PagingResult<Song>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize)
    {
        string cacheKey = $"liked_songs:{userId}:{pageIndex}:{pageSize}";
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync(cacheKey);
            if (!cached.IsNullOrEmpty)
                return System.Text.Json.JsonSerializer.Deserialize<PagingResult<Song>>(cached!)!;
            var result = await _interactionRepo.GetLikedSongsAsync(userId, pageIndex, pageSize);
            await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(result), TimeSpan.FromMinutes(10));
            return result;
        }
        catch (RedisException)
        {
            return await _interactionRepo.GetLikedSongsAsync(userId, pageIndex, pageSize);
        }
    }

    public async Task<Result> AddSongToPlaylistAsync(Guid userId, Guid playlistId, Guid songId)
    {
        // 1. Kiểm tra Playlist có tồn tại không
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null)
        {
            return Result.NotFound("Playlist không tồn tại.");
        }

        // 2. Kiểm tra User có phải chủ sở hữu Playlist không (Logic nghiệp vụ)
        if (playlist.UserId != userId)
        {
            return Result.Forbidden("Bạn không có quyền chỉnh sửa playlist này.");
        }

        // 3. Gọi Repo thêm vào DB
        await _interactionRepo.AddSongToPlaylistAsync(playlistId, songId);
        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: $"playlist:{playlistId}:details:*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException) { }
        return Result.Success("Đã thêm bài hát vào playlist");
    }

    public async Task<Result> RemoveSongFromPlaylistAsync(Guid userId, Guid playlistId, Guid songId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result.NotFound("Playlist không tồn tại.");

        if (playlist.UserId != userId) return Result.Forbidden("Bạn không có quyền chỉnh sửa playlist này.");

        await _interactionRepo.RemoveSongFromPlaylistAsync(playlistId, songId);
        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: $"playlist:{playlistId}:details:*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException) { }
        return Result.Success("Đã xóa bài hát khỏi playlist");
    }

    public async Task<Result<(bool IsFollowing, string Message)>> ToggleFollowAsync(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
        {
            return Result<(bool, string)>.BadRequest("Bạn không thể tự theo dõi chính mình.");
        }

        bool isFollowing = await _interactionRepo.ToggleFollowAsync(followerId, followingId);

        return Result<(bool, string)>.Success((isFollowing, isFollowing ? "Đã theo dõi" : "Đã hủy theo dõi"));
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

    public async Task<Result<Playlist>> UpdatePlaylistAsync(Guid userId, Guid playlistId, string title)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result<Playlist>.NotFound("Playlist không tồn tại.");
        if (playlist.UserId != userId) return Result<Playlist>.Forbidden("Bạn không có quyền chỉnh sửa playlist này.");

        playlist.Title = title;
        playlist.UpdatedAt = DateTime.Now;
        await _playlistRepo.UpdateAsync(playlistId, playlist);
        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: $"playlist:{playlistId}:details:*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException) { }
        return Result<Playlist>.Success(playlist);
    }

    public async Task<Result> DeletePlaylistAsync(Guid userId, Guid playlistId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result.NotFound("Playlist không tồn tại.");
        if (playlist.UserId != userId) return Result.Forbidden("Bạn không có quyền xóa playlist này.");

        await _playlistRepo.DeleteAsync(playlistId);
        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: $"playlist:{playlistId}:details:*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException) { }
        return Result.Success("Đã xóa playlist thành công");
    }

    public async Task<PlaylistDetailsDto?> GetPlaylistDetailsAsync(Guid playlistId, int pageIndex, int pageSize)
    {
        string cacheKey = $"playlist:{playlistId}:details:{pageIndex}:{pageSize}";
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync(cacheKey);
            if (!cached.IsNullOrEmpty)
                return System.Text.Json.JsonSerializer.Deserialize<PlaylistDetailsDto>(cached!);
            var details = await _interactionRepo.GetPlaylistDetailsAsync(playlistId, pageIndex, pageSize);
            if (details != null)
                await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(details), TimeSpan.FromMinutes(10));
            return details;
        }
        catch (RedisException)
        {
            return await _interactionRepo.GetPlaylistDetailsAsync(playlistId, pageIndex, pageSize);
        }
    }

    public async Task RemoveSongFromAlbumAsync(Guid userId, Guid albumId, Guid songId)
    {
        await _interactionRepo.RemoveSongFromAlbumAsync(userId, albumId, songId);
        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: $"album:{albumId}:details:*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException) { }
    }
}

