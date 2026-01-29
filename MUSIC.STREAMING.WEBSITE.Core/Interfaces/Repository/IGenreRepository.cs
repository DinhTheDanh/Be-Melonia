using System;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IGenreRepository : IBaseRepository<Genre>
{
    /// <summary>
    /// Lấy danh sách tất cả các thể loại nhạc
    /// </summary>
    /// <returns>Kết quả trả về là danh sách các thể loại nhạc</returns>
    Task<IEnumerable<Genre>> GetAllGenresAsync();
}
