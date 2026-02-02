using System;
using System.Transactions;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Helpers;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class MusicService : IMusicService
{
    private readonly ISongRepository _songRepo;
    private readonly IAlbumRepository _albumRepo;
    private readonly IPlaylistRepository _playlistRepo;
    private readonly IGenreRepository _genreRepo;

    public MusicService(ISongRepository songRepo, IAlbumRepository albumRepo, IPlaylistRepository playlistRepo, IGenreRepository genreRepo)
    {
        _songRepo = songRepo;
        _albumRepo = albumRepo;
        _playlistRepo = playlistRepo;
        _genreRepo = genreRepo;
    }
    public async Task<PagingResult<SongDto>> GetAllSongsAsync(string keyword, int pageIndex, int pageSize)
    {
        return await _songRepo.GetAllSongsWithArtistAsync(keyword, pageIndex, pageSize);
    }
    public async Task<PagingResult<SongDto>> GetSongsByArtistAsync(Guid artistId, int pageIndex, int pageSize)
    {
        return await _songRepo.GetSongsByArtistIdAsync(artistId, pageIndex, pageSize);
    }

    public async Task<IEnumerable<GenreDto>> GetAllGenresAsync()
    {
        var genres = await _genreRepo.GetAllGenresAsync();
        return genres.Select(g => new GenreDto { Id = g.Id, Name = g.Name, ImageUrl = g.ImageUrl });
    }

    public async Task<PagingResult<AlbumDto>> GetAlbumsAsync(string keyword, int pageIndex, int pageSize)
    {
        return await _albumRepo.GetAlbumsWithArtistAsync(keyword, pageIndex, pageSize);
    }

    public async Task<Song?> CheckFileHashAsync(string hash)
    {
        return await _songRepo.GetByFileHashAsync(hash);
    }

    public async Task<Song> CreateSongAsync(Guid artistId, CreateSongDto dto)
    {

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            // 1. Random Thumbnail 
            var thumb = !string.IsNullOrEmpty(dto.Thumbnail) ? dto.Thumbnail : ImageHelper.GenerateCover(dto.Title);

            // 2. Tạo Entity Song
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

            // 3. Insert bài hát vào bảng songs
            await _songRepo.CreateAsync(song);

            // 4. Xử lý Artist 
            if (!dto.ArtistIds.Contains(artistId))
            {
                dto.ArtistIds.Add(artistId);
            }
            if (dto.ArtistIds.Any())
            {
                await _songRepo.AddArtistsToSongAsync(song.SongId, dto.ArtistIds);
            }

            // Lưu các thể loại vào bảng trung gian song_genres
            if (dto.GenreIds != null && dto.GenreIds.Any())
            {
                await _songRepo.AddGenresToSongAsync(song.SongId, dto.GenreIds);
            }
            scope.Complete();
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

    public async Task<Genre> CreateGenreAsync(CreateGenreDto dto)
    {
        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ImageUrl = dto.ImageUrl
        };

        await _genreRepo.CreateAsync(genre);

        return genre;
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

    public async Task<Result> DeleteSongAsync(Guid artistId, Guid songId)
    {
        var song = await _songRepo.GetByIdAsync(songId);
        if (song == null) return Result.NotFound("Bài hát không tồn tại");

        var isOwner = await _songRepo.CheckSongOwnerAsync(artistId, songId);
        if (!isOwner) return Result.Forbidden("Bạn không có quyền xóa bài hát này");

        await _songRepo.DeleteAsync(songId);
        return Result.Success();
    }

    public async Task<Result> UpdateSongAsync(Guid artistId, Guid songId, UpdateSongDto dto)
    {
        var song = await _songRepo.GetByIdAsync(songId);
        if (song == null) return Result.NotFound("Bài hát không tồn tại");

        var isOwner = await _songRepo.CheckSongOwnerAsync(artistId, songId);
        if (!isOwner) return Result.Forbidden("Bạn không có quyền chỉnh sửa bài hát này");

        song.Title = dto.Title ?? song.Title;
        song.Thumbnail = dto.Thumbnail ?? song.Thumbnail;
        song.Lyrics = dto.Lyrics ?? song.Lyrics;
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

        return Result.Success();
    }

    public async Task<Result> DeleteAlbumAsync(Guid artistId, Guid albumId)
    {
        var album = await _albumRepo.GetByIdAsync(albumId);
        if (album == null) return Result.NotFound("Album không tồn tại");

        if (album.ArtistId != artistId) return Result.Forbidden("Bạn không có quyền xóa album này");

        await _albumRepo.DeleteAsync(albumId);
        return Result.Success();
    }

    public async Task<Result> UpdateAlbumAsync(Guid artistId, Guid albumId, UpdateAlbumDto dto)
    {
        var album = await _albumRepo.GetByIdAsync(albumId);
        if (album == null) return Result.NotFound("Album không tồn tại");

        if (album.ArtistId != artistId) return Result.Forbidden("Bạn không có quyền chỉnh sửa album này");

        album.Title = dto.Title ?? album.Title;
        album.Thumbnail = dto.Thumbnail ?? album.Thumbnail;
        if (dto.ReleaseDate.HasValue)
        {
            album.ReleaseDate = dto.ReleaseDate.Value;
        }

        await _albumRepo.UpdateAsync(albumId, album);
        return Result.Success();
    }
}

