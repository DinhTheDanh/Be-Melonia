# Missing API cho Dashboard Thống kê chuyên sâu (FE -> BE)

## Mục tiêu

Frontend đã triển khai trang Dashboard phân tích nâng cao tại `/user`, mở khóa khi user có gói `>= 6 tháng`.

Hiện FE đã hiển thị được **snapshot tổng quan** (tổng followers/listens/likes) từ API hiện có, nhưng **chưa có dữ liệu tăng trưởng theo thời gian** do thiếu API chuyên biệt.

---

## API cần bổ sung

### 1) Lấy dữ liệu dashboard nâng cao cho artist hiện tại

**Endpoint đề xuất**

`GET /Artist/analytics/dashboard?days=30`

- Auth: JWT (Artist/Admin)
- Không cần truyền artistId ở query (lấy từ token để tránh lộ dữ liệu)

**Response đề xuất (200)**

```json
{
  "Message": "Lấy dữ liệu dashboard thành công",
  "Data": {
    "Summary": {
      "TotalFollowers": 1250,
      "TotalListens": 98540,
      "TotalLikes": 8430,
      "TotalSongs": 42
    },
    "Trends": [
      {
        "Date": "2026-02-15",
        "Followers": 1180,
        "Listens": 91200,
        "Likes": 7900
      },
      {
        "Date": "2026-02-16",
        "Followers": 1188,
        "Listens": 91870,
        "Likes": 7964
      }
    ]
  }
}
```

**Ghi chú**

- `Trends` nên là **cumulative theo ngày** để FE vẽ line chart trực tiếp.
- `days` hỗ trợ: 7, 30, 90 (mặc định 30).

---

### 2) (Optional) API chi tiết theo bài hát để drill-down

Nếu BE muốn FE hiển thị chi tiết sâu hơn (bảng top songs theo period), cần thêm:

`GET /Artist/analytics/top-songs?days=30&pageIndex=1&pageSize=10`

**Response đề xuất (200)**

```json
{
  "Data": [
    {
      "SongId": "guid",
      "Title": "Song A",
      "Thumbnail": "https://...",
      "Listens": 23450,
      "Likes": 2150,
      "FollowersGained": 78
    }
  ],
  "TotalRecords": 25,
  "TotalPages": 3,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

## FE hiện đang fallback như sau

Khi chưa có API trend:

- Vẫn hiển thị số tổng hợp từ API hiện có (`/Music/my-songs`, `/Artist`).
- Biểu đồ tăng trưởng hiển thị trạng thái thiếu dữ liệu.
- Hiển thị cảnh báo mềm để đội BE biết cần bổ sung endpoint.

---

## Ưu tiên triển khai BE

1. `GET /Artist/analytics/dashboard` (bắt buộc)
2. `GET /Artist/analytics/top-songs` (tùy chọn)
