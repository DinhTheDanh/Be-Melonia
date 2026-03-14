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
    private readonly IUserRepository _userRepo;

    public InteractionService(IInteractionRepository interactionRepo, IBaseRepository<Playlist> playlistRepo, IConnectionMultiplexer redis, IUserRepository userRepo)
    {
        _interactionRepo = interactionRepo;
        _playlistRepo = playlistRepo;
        _redis = redis;
        _userRepo = userRepo;
    }

    /// <summary>
    /// Xóa cache theo pattern sử dụng Lua script (atomic, reliable hơn server.Keys/SCAN)
    /// </summary>
    private async Task InvalidateCacheByPatternAsync(string pattern)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.ScriptEvaluateAsync(
                "local keys = redis.call('keys', ARGV[1]) for i=1,#keys do redis.call('unlink', keys[i]) end return #keys",
                values: new RedisValue[] { pattern });
        }
        catch (Exception) { }
    }

    public async Task<(bool IsLiked, string Message)> ToggleLikeAsync(Guid userId, Guid songId)
    {
        bool isLiked = await _interactionRepo.ToggleLikeAsync(userId, songId);
        await InvalidateCacheByPatternAsync($"liked_songs:{userId}:*");
        return (isLiked, isLiked ? "Đã thích bài hát" : "Đã bỏ thích bài hát");
    }

    public async Task<PagingResult<SongDto>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize)
    {
        string cacheKey = $"liked_songs:{userId}:{pageIndex}:{pageSize}";
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync(cacheKey);
            if (!cached.IsNullOrEmpty)
                return System.Text.Json.JsonSerializer.Deserialize<PagingResult<SongDto>>(cached!)!;
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
        await InvalidateCacheByPatternAsync($"playlist:{playlistId}:details:*");
        return Result.Success("Đã thêm bài hát vào playlist");
    }

    public async Task<Result> RemoveSongFromPlaylistAsync(Guid userId, Guid playlistId, Guid songId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result.NotFound("Playlist không tồn tại.");

        if (playlist.UserId != userId) return Result.Forbidden("Bạn không có quyền chỉnh sửa playlist này.");

        await _interactionRepo.RemoveSongFromPlaylistAsync(playlistId, songId);
        await InvalidateCacheByPatternAsync($"playlist:{playlistId}:details:*");
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

        var artistIds = result.Data.Select(u => u.UserId).ToList();
        var statsMap = await _userRepo.GetArtistsStatsBatchAsync(artistIds);

        var dtos = result.Data.Select(u =>
        {
            statsMap.TryGetValue(u.UserId, out var stats);
            return new ArtistDto
            {
                UserId = u.UserId,
                FullName = u.FullName ?? u.Username,
                Avatar = u.Avatar,
                Banner = u.Banner,
                Bio = u.Bio,
                ArtistType = u.ArtistType,
                FollowerCount = stats?.FollowerCount ?? 0,
                SongCount = stats?.SongCount ?? 0,
                TotalLikes = stats?.TotalLikes ?? 0,
                TotalListens = stats?.TotalListens ?? 0
            };
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

    public async Task<Result<Playlist>> UpdatePlaylistAsync(Guid userId, Guid playlistId, UpdatePlaylistDto dto)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result<Playlist>.NotFound("Playlist không tồn tại.");
        if (playlist.UserId != userId) return Result<Playlist>.Forbidden("Bạn không có quyền chỉnh sửa playlist này.");

        playlist.Title = dto.Title;
        if (dto.Thumbnail != null) playlist.Thumbnail = dto.Thumbnail;
        if (dto.Description != null) playlist.Description = dto.Description;
        if (dto.IsPublic.HasValue) playlist.IsPublic = dto.IsPublic.Value;
        playlist.UpdatedAt = DateTime.Now;
        await _playlistRepo.UpdateAsync(playlistId, playlist);
        await InvalidateCacheByPatternAsync($"playlist:{playlistId}:details:*");
        return Result<Playlist>.Success(playlist);
    }

    public async Task<Result<(bool IsPublic, string Message)>> TogglePlaylistVisibilityAsync(Guid userId, Guid playlistId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result<(bool, string)>.NotFound("Playlist không tồn tại.");
        if (playlist.UserId != userId) return Result<(bool, string)>.Forbidden("Bạn không có quyền chỉnh sửa playlist này.");

        playlist.IsPublic = !playlist.IsPublic;
        playlist.UpdatedAt = DateTime.Now;
        await _playlistRepo.UpdateAsync(playlistId, playlist);
        await InvalidateCacheByPatternAsync($"playlist:{playlistId}:details:*");

        string message = playlist.IsPublic ? "Playlist đã được đặt thành công khai" : "Playlist đã được đặt thành riêng tư";
        return Result<(bool, string)>.Success((playlist.IsPublic, message));
    }

    public async Task<Result> DeletePlaylistAsync(Guid userId, Guid playlistId)
    {
        var playlist = await _playlistRepo.GetByIdAsync(playlistId);
        if (playlist == null) return Result.NotFound("Playlist không tồn tại.");
        if (playlist.UserId != userId) return Result.Forbidden("Bạn không có quyền xóa playlist này.");

        await _playlistRepo.DeleteAsync(playlistId);
        await InvalidateCacheByPatternAsync($"playlist:{playlistId}:details:*");
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

    public async Task<Result> RecordPlayAsync(Guid userId, RecordPlayDto dto)
    {
        if (dto.SongId == Guid.Empty)
            return Result.Failure("SongId không hợp lệ.");

        if (dto.DurationListened < 0)
            return Result.Failure("DurationListened không hợp lệ.");

        await _interactionRepo.RecordPlayAsync(userId, dto.SongId, dto.DurationListened, dto.Completed, dto.Source);
        return Result.Success("Đã ghi nhận lượt nghe");
    }

    public async Task RemoveSongFromAlbumAsync(Guid userId, Guid albumId, Guid songId)
    {
        await _interactionRepo.RemoveSongFromAlbumAsync(userId, albumId, songId);
        await InvalidateCacheByPatternAsync($"album:{albumId}:details:*");
    }
}

