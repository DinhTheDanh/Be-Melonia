using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces;

public interface IBaseRepository<T>
{
    /// <summary>
    /// Lọc danh sách 
    /// </summary>
    /// <param name="keyword">Tìm kiếm</param>
    /// <param name="pageIndex">Số trang</param>
    /// <param name="pageSize">Kích thước của trang</param>
    /// <returns>DTO của danh sách</returns> 
    /// Created by: ddanh (06/12/2025)
    Task<PagingResult<T>> GetPagingAsync(string keyword, int pageIndex, int pageSize);

    /// <summary>
    /// Lấy cụ thể 1 phần tử
    /// </summary>
    /// <param name="id"></param>
    /// <returns>1 bản ghi</returns>
    /// Created by: ddanh (06/12/2025)
    Task<T> GetByIdAsync(Guid id);

    /// <summary>
    /// Tạo 1 bản ghi 
    /// </summary>
    /// <param name="entity">Dữ liệu</param>
    /// <returns>1</returns>
    /// Created by: ddanh (06/12/2025)
    Task<int> CreateAsync(T entity);

    /// <summary>
    /// Xóa 1 bản ghi
    /// </summary>
    /// <param name="id">Id của bản ghi</param>
    /// <returns>1</returns>
    /// Created by: ddanh (06/12/2025)
    Task<int> DeleteAsync(Guid id);

    /// <summary>
    /// Sửa 1 bản ghi
    /// </summary>
    /// <param name="id">Id của bản ghi</param>
    /// <param name="entity">Dữ liệu mới</param>
    /// <returns>1</returns> 
    /// Created by: ddanh (06/12/2025)
    Task<int> UpdateAsync(Guid id, T entity);

}
