using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class PagingResult<T>
{
    public int TotalRecords { get; set; } // Tổng số bản ghi
    public int TotalPages { get; set; }   // Tổng số trang
    public int FromRecord { get; set; }   // Bản ghi bắt đầu
    public int ToRecord { get; set; }     // Bản ghi kết thúc
    public IEnumerable<T> Data { get; set; } = []; // Dữ liệu
}
