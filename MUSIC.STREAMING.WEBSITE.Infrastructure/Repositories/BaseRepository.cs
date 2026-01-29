using System.Data;
using Dapper;
using Dommel;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly IDbConnection _connection;

    public BaseRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public virtual async Task<int> CreateAsync(T entity)
    {
        var id = await _connection.InsertAsync(entity);
        return 1;
    }

    public virtual async Task<int> UpdateAsync(Guid id, T entity)
    {
        var success = await _connection.UpdateAsync(entity);
        return success ? 1 : 0;
    }

    public virtual async Task<int> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            var success = await _connection.DeleteAsync(entity);
            return success ? 1 : 0;
        }
        return 0;
    }

    public virtual async Task<T> GetByIdAsync(Guid id)
    {
        return await _connection.GetAsync<T>(id);
    }

    public virtual async Task<PagingResult<T>> GetPagingAsync(string keyword, int pageIndex, int pageSize)
    {
        var tableName = GetTableName();
        var offset = (pageIndex - 1) * pageSize;
        var parameters = new DynamicParameters();
        parameters.Add("@v_Offset", offset);
        parameters.Add("@v_Limit", pageSize);
        parameters.Add("@v_Keyword", keyword);

        var whereClause = BuildWhereClause(keyword);

        var sqlCount = $"SELECT COUNT(*) FROM {tableName} {whereClause};";

        var sqlData = $"SELECT * FROM {tableName} {whereClause} ORDER BY created_at DESC LIMIT @v_Limit OFFSET @v_Offset;";

        using (var multi = await _connection.QueryMultipleAsync(sqlCount + sqlData, parameters))
        {
            var totalRecords = await multi.ReadFirstAsync<int>();
            var data = await multi.ReadAsync<T>();

            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var fromRecord = totalRecords == 0 ? 0 : ((pageIndex - 1) * pageSize) + 1;
            var toRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords);

            return new PagingResult<T>
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                FromRecord = fromRecord,
                ToRecord = toRecord,
                Data = data
            };
        }
    }

    protected virtual string BuildWhereClause(string keyword)
    {
        return "";
    }

    private string GetTableName()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null)
        {
            return tableAttr.Name;
        }

        return type.Name.ToLower() + "s";
    }
}