using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IAlbumRepository : IBaseRepository<Album>
{
    /// <summary>
    /// Lấy danh sách Album kèm theo thông tin Artist, có phân trang
    /// </summary>
    /// <param name="pageIndex">Số trang</param>
    /// <param name="pageSize">Số lượng bản ghi trên mỗi trang</param>
    /// <returns></returns>
    Task<PagingResult<AlbumDto>> GetAlbumsWithArtistAsync(int pageIndex, int pageSize);
}
