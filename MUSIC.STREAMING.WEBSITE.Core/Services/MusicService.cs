using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Helpers;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using StackExchange.Redis;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class MusicService : IMusicService
{
    private readonly ISongRepository _songRepo;
    private readonly IAlbumRepository _albumRepo;
    private readonly IPlaylistRepository _playlistRepo;
    private readonly IGenreRepository _genreRepo;
    private readonly IConnectionMultiplexer _redis;

    public MusicService(ISongRepository songRepo, IAlbumRepository albumRepo, IPlaylistRepository playlistRepo, IGenreRepository genreRepo, IConnectionMultiplexer redis)
    {
        _songRepo = songRepo;
        _albumRepo = albumRepo;
        _playlistRepo = playlistRepo;
        _genreRepo = genreRepo;
        _redis = redis;
    }

    public async Task<IEnumerable<GenreDto>> GetAllGenresAsync()
    {
        string cacheKey = "genres:all";
        try
        {
            var db = _redis.GetDatabase();
            var cachedData = await db.StringGetAsync(cacheKey);
            if (!cachedData.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<IEnumerable<GenreDto>>(cachedData)!;
            }

            var genres = await _genreRepo.GetAllGenresAsync();
            var result = genres.Select(g => new GenreDto { Id = g.Id, Name = g.Name, ImageUrl = g.ImageUrl }).ToList();

            if (result.Any())
            {
                var json = JsonSerializer.Serialize(result);
                await db.StringSetAsync(cacheKey, json, TimeSpan.FromHours(24));
            }

            return result;
        }
        catch (RedisException)
        {
            var genres = await _genreRepo.GetAllGenresAsync();
            return genres.Select(g => new GenreDto { Id = g.Id, Name = g.Name, ImageUrl = g.ImageUrl }).ToList();
        }
    }

    public async Task<AlbumDetailsDto?> GetAlbumDetailsAsync(Guid albumId, int pageIndex, int pageSize)
    {
        string cacheKey = $"album:{albumId}:details:{pageIndex}:{pageSize}";
        try
        {
            var db = _redis.GetDatabase();
            var cachedData = await db.StringGetAsync(cacheKey);
            if (!cachedData.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<AlbumDetailsDto>(cachedData!);
            }

            var data = await _albumRepo.GetAlbumDetailsAsync(albumId, pageIndex, pageSize);

            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(10));
            }

            return data;
        }
        catch (RedisException)
        {
            return await _albumRepo.GetAlbumDetailsAsync(albumId, pageIndex, pageSize);
        }
    }


    public async Task<PagingResult<SongDto>> GetAllSongsAsync(string keyword, int pageIndex, int pageSize)
    {
        return await _songRepo.GetAllSongsWithArtistAsync(keyword, pageIndex, pageSize);
    }
    public async Task<PagingResult<SongDto>> GetSongsByArtistAsync(Guid artistId, int pageIndex, int pageSize)
    {
        return await _songRepo.GetSongsByArtistIdAsync(artistId, pageIndex, pageSize);
    }

    public async Task<PagingResult<AlbumDto>> GetAlbumsAsync(string keyword, int pageIndex, int pageSize)
    {
        return await _albumRepo.GetAlbumsWithArtistAsync(keyword, pageIndex, pageSize);
    }

    public async Task<Song?> CheckFileHashAsync(string hash)
    {
        return await _songRepo.GetByFileHashAsync(hash);
    }

    public async Task<PagingResult<SongDto>> GetUserSongsAsync(Guid userId, string keyword, int pageIndex, int pageSize)
    {
        return await _songRepo.GetUserSongsAsync(userId, keyword, pageIndex, pageSize);
    }

    public async Task<PagingResult<AlbumDto>> GetUserAlbumsAsync(Guid userId, string keyword, int pageIndex, int pageSize)
    {
        return await _albumRepo.GetUserAlbumsAsync(userId, keyword, pageIndex, pageSize);
    }

    public async Task<PagingResult<PlaylistDto>> GetUserPlaylistsAsync(Guid userId, string keyword, int pageIndex, int pageSize)
    {
        return await _playlistRepo.GetUserPlaylistsAsync(userId, keyword, pageIndex, pageSize);
    }

    public async Task<PagingResult<PlaylistDto>> GetAllPlaylistsAsync(string keyword, int pageIndex, int pageSize)
    {
        return await _playlistRepo.GetAllPlaylistsAsync(keyword, pageIndex, pageSize);
    }


    public async Task<Genre> CreateGenreAsync(CreateGenreDto dto)
    {
        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ImageUrl = dto.ImageUrl
        };

        await _genreRepo.CreateAsync(genre);

        // [REDIS] Xóa cache danh sách Genre để user thấy cái mới ngay
        try { await _redis.GetDatabase().KeyDeleteAsync("genres:all"); } catch (RedisException) { }

        return genre;
    }

    public async Task<Song> CreateSongAsync(Guid artistId, CreateSongDto dto)
    {
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            var thumb = !string.IsNullOrEmpty(dto.Thumbnail) ? dto.Thumbnail : ImageHelper.GenerateCover(dto.Title);

            var song = new Song
            {
                SongId = Guid.NewGuid(),
                Title = dto.Title,
                AlbumId = dto.AlbumId,
                FileUrl = dto.FileUrl,
                Thumbnail = thumb,
                Duration = dto.Duration,
                Lyrics = dto.Lyrics,
                FileHash = dto.FileHash,
                CreatedAt = DateTime.Now
            };

            await _songRepo.CreateAsync(song);

            if (!dto.ArtistIds.Contains(artistId))
            {
                dto.ArtistIds.Add(artistId);
            }
            if (dto.ArtistIds.Any())
            {
                await _songRepo.AddArtistsToSongAsync(song.SongId, dto.ArtistIds);
            }

            if (dto.GenreIds != null && dto.GenreIds.Any())
            {
                await _songRepo.AddGenresToSongAsync(song.SongId, dto.GenreIds);
            }
            scope.Complete();

            if (dto.AlbumId.HasValue && dto.AlbumId != Guid.Empty)
            {
                try { await _redis.GetDatabase().KeyDeleteAsync($"album:{dto.AlbumId}:details:1:10"); } catch (RedisException) { }
            }

            return song;
        }
    }

    public async Task<Album> CreateAlbumAsync(Guid artistId, CreateAlbumDto dto)
    {
        var thumb = !string.IsNullOrEmpty(dto.Thumbnail) ? dto.Thumbnail : ImageHelper.GenerateCover(dto.Title, "Album");
        var album = new Album
        {
            AlbumId = Guid.NewGuid(),
            ArtistId = artistId,
            Title = dto.Title,
            Thumbnail = thumb,
            ReleaseDate = DateTime.Now,
            CreatedAt = DateTime.Now
        };
        await _albumRepo.CreateAsync(album);

        return album;
    }

    public async Task<Playlist> CreatePlaylistAsync(Guid userId, CreatePlaylistDto dto)
    {
        var thumb = !string.IsNullOrEmpty(dto.Thumbnail) ? dto.Thumbnail : ImageHelper.GenerateCover(dto.Title, "Playlist");
        var playlist = new Playlist
        {
            PlaylistId = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title,
            Thumbnail = thumb,
            IsPublic = dto.IsPublic,
            CreatedAt = DateTime.Now
        };
        await _playlistRepo.CreateAsync(playlist);
        return playlist;
    }

    public async Task<Result> UpdateSongAsync(Guid artistId, Guid songId, UpdateSongDto dto)
    {
        var song = await _songRepo.GetByIdAsync(songId);
        if (song == null) return Result.NotFound("Bài hát không tồn tại");

        var isOwner = await _songRepo.CheckSongOwnerAsync(artistId, songId);
        if (!isOwner) return Result.Forbidden("Bạn không có quyền chỉnh sửa bài hát này");

        var oldAlbumId = song.AlbumId;

        song.Title = dto.Title ?? song.Title;
        song.Thumbnail = dto.Thumbnail ?? song.Thumbnail;
        song.Lyrics = dto.Lyrics ?? song.Lyrics;
        song.UpdatedAt = DateTime.Now;
        if (dto.AlbumId.HasValue)
        {
            song.AlbumId = dto.AlbumId.Value;
        }

        await _songRepo.UpdateAsync(songId, song);

        if (dto.GenreIds != null && dto.GenreIds.Any())
        {
            await _songRepo.RemoveGenresFromSongAsync(songId);
            await _songRepo.AddGenresToSongAsync(songId, dto.GenreIds);
        }

        try
        {
            var db = _redis.GetDatabase();
            if (oldAlbumId != Guid.Empty)
                await db.KeyDeleteAsync($"album:{oldAlbumId}:details:1:10");
            if (dto.AlbumId.HasValue && dto.AlbumId != Guid.Empty)
                await db.KeyDeleteAsync($"album:{dto.AlbumId}:details:1:10");
        }
        catch (RedisException) { }

        return Result.Success();
    }

    public async Task<Result> DeleteSongAsync(Guid artistId, Guid songId)
    {
        var song = await _songRepo.GetByIdAsync(songId);
        if (song == null) return Result.NotFound("Bài hát không tồn tại");

        var isOwner = await _songRepo.CheckSongOwnerAsync(artistId, songId);
        if (!isOwner) return Result.Forbidden("Bạn không có quyền xóa bài hát này");

        var albumId = song.AlbumId;

        await _songRepo.DeleteAsync(songId);

        if (albumId != Guid.Empty)
        {
            try { await _redis.GetDatabase().KeyDeleteAsync($"album:{albumId}:details:1:10"); } catch (RedisException) { }
        }

        return Result.Success();
    }

    public async Task<Result> UpdateAlbumAsync(Guid artistId, Guid albumId, UpdateAlbumDto dto)
    {
        var album = await _albumRepo.GetByIdAsync(albumId);
        if (album == null) return Result.NotFound("Album không tồn tại");

        if (album.ArtistId != artistId) return Result.Forbidden("Bạn không có quyền chỉnh sửa album này");

        album.Title = dto.Title ?? album.Title;
        album.Thumbnail = dto.Thumbnail ?? album.Thumbnail;
        album.UpdatedAt = DateTime.Now;
        if (dto.ReleaseDate.HasValue)
        {
            album.ReleaseDate = dto.ReleaseDate.Value;
        }

        await _albumRepo.UpdateAsync(albumId, album);
        // [REDIS] Xóa cache chi tiết Album (mọi page)
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
        return Result.Success();
    }

    public async Task<Result> DeleteAlbumAsync(Guid artistId, Guid albumId)
    {
        var album = await _albumRepo.GetByIdAsync(albumId);
        if (album == null) return Result.NotFound("Album không tồn tại");

        if (album.ArtistId != artistId) return Result.Forbidden("Bạn không có quyền xóa album này");

        await _albumRepo.DeleteAsync(albumId);
        // [REDIS] Xóa cache chi tiết Album (mọi page)
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
        return Result.Success();
    }

    public async Task<Result> AddSongToAlbumAsync(Guid userId, Guid albumId, Guid songId)
    {
        var album = await _albumRepo.GetByIdAsync(albumId);
        if (album == null) return Result.NotFound("Album không tồn tại");

        var isAlbumOwner = await _albumRepo.CheckAlbumOwnerAsync(userId, albumId);
        if (!isAlbumOwner) return Result.Forbidden("Bạn không có quyền thêm bài hát vào album này");

        var song = await _songRepo.GetByIdAsync(songId);
        if (song == null) return Result.NotFound("Bài hát không tồn tại");

        var isSongOwner = await _songRepo.CheckSongOwnerAsync(userId, songId);
        if (!isSongOwner) return Result.Forbidden("Bạn không có quyền thêm bài hát này vào album");

        await _albumRepo.AddSongToAlbumAsync(albumId, songId);
        // [REDIS] Xóa cache chi tiết Album (mọi page)
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
        return Result.Success();
    }
}