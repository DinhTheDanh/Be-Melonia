using System;
using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class GenreRepository : BaseRepository<Genre>, IGenreRepository
{
    public GenreRepository(IDbConnection connection) : base(connection)
    {
    }

    public override async Task<int> CreateAsync(Genre entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        var sql = @"INSERT INTO genres (id, name, image_url)
                    VALUES (@Id, @Name, @ImageUrl);";

        return await _connection.ExecuteAsync(sql, entity);
    }

    public async Task<IEnumerable<Genre>> GetAllGenresAsync()
    {
        return await _connection.QueryAsync<Genre>("SELECT * FROM genres ORDER BY name ASC");
    }
}
