# 🎵 MUSIC STREAMING WEBSITE - API DOCUMENTATION

---

## Base URL

```
http://localhost:5111/api/v1
```

## Authentication

- Endpoints với ✅ cần token JWT trong Cookie `jwt`
- Gửi Authorization Header: `Authorization: Bearer {token}` hoặc Cookie

---

## 📦 Response Format

> **Lưu ý:** API trả về JSON với property names dạng **PascalCase** (viết hoa chữ cái đầu)

### Chuẩn Response cho các thao tác (Create/Update/Delete)

**✅ Thành công (200 OK):**

```json
{
  "Message": "Thông báo thành công"
}
```

**❌ Không tìm thấy (404 Not Found):**

```json
{
  "Message": "Tài nguyên không tồn tại"
}
```

**🚫 Không có quyền (403 Forbidden):**

```json
{
  "Message": "Bạn không có quyền thực hiện thao tác này"
}
```

**⚠️ Lỗi validation (400 Bad Request):**

```json
{
  "Message": "Mô tả lỗi"
}
```

**🔒 Chưa đăng nhập (401 Unauthorized):**

```json
{
  "Message": "Unauthorized"
}
```

### Response có kèm Data

```json
{
  "Message": "Thao tác thành công",
  "Data": { ... }
}
```

### Response Paging (Danh sách)

```json
{
  "Data": [...],
  "TotalRecords": 100,
  "TotalPages": 10,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

## 🔐 AUTH ENDPOINTS

### Authentication Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  1. User đăng nhập (login/google-login)                         │
│     ↓                                                           │
│  2. Server trả về:                                              │
│     • Access Token (trong response body) - hết hạn sau 60 phút  │
│     • Refresh Token (trong HTTP-Only Cookie) - hết hạn sau 7 ngày│
│     ↓                                                           │
│  3. FE lưu Access Token, Cookie tự động lưu Refresh Token       │
│     ↓                                                           │
│  4. Khi Access Token hết hạn (nhận 401):                        │
│     → Gọi /Auth/refresh-token (Cookie tự động gửi kèm)          │
│     → Nhận Access Token mới                                     │
└─────────────────────────────────────────────────────────────────┘
```

---

### 1. Login

```
POST /Auth/login
Content-Type: application/json

{
  "Email": "user@example.com",
  "Password": "password123"
}
```

**Response (200 OK):**

```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "abc123def456...",
  "Expiration": "2026-02-08T11:00:00Z"
}
```

**Cookies được set:**

```
Set-Cookie: refreshToken=abc123...; HttpOnly; Secure; SameSite=Strict; Path=/; Expires=...
```

---

### 2. Google Login

```
POST /Auth/google-login
Content-Type: application/json

{
  "IdToken": "google-id-token-from-fe"
}
```

**Response:** Giống Login

---

### 3. Register

```
POST /Auth/register
Content-Type: application/json

{
  "Email": "user@example.com",
  "Password": "password123",
  "FullName": "Nguyễn Văn A"
}
```

---

### 4. Refresh Token

```
POST /Auth/refresh-token
```

**Note:**

- Không cần body
- Refresh token được gửi tự động qua Cookie
- FE chỉ cần gọi khi nhận lỗi 401 (Access Token hết hạn)
- Server kiểm tra blacklist Redis trước khi xử lý

**Response (200 OK):**

```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "newRefreshToken...",
  "Expiration": "2026-02-08T12:00:00Z"
}
```

---

### 5. Logout ✅

```
POST /Auth/logout
```

**Note:** Xóa refresh token khỏi database, blacklist token trong Redis (7 ngày), và xóa cookie

---

### 6. Forgot Password

```
POST /Auth/forgot-password
Content-Type: application/json

{
  "Email": "user@example.com"
}
```

**Note:** Gửi email chứa link reset password. Token được lưu trong Redis với TTL 15 phút.

---

### 7. Reset Password

```
POST /Auth/reset-password
Content-Type: application/json

{
  "Token": "reset-token-from-email",
  "NewPassword": "newpassword123"
}
```

**Note:** Xác minh token từ Redis, sau khi reset thành công token sẽ bị xóa.

---

## 🎵 MUSIC ENDPOINTS

### 1. Get All Songs (Search)

```
GET /Music/songs?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên bài hát
- `pageIndex` (int) - Trang (mặc định 1)
- `pageSize` (int) - Số bản ghi/trang (mặc định 10)

**Response:** PagingResult\<SongDto\>

---

### 2. Get My Songs (Authorized) ✅

```
GET /Music/my-songs?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Songs

**Response:** PagingResult\<SongDto\> của chính mình

---

### 3. Create Song ✅

```
POST /Music/song
Content-Type: application/json

{
  "Title": "Bài hát mới",
  "AlbumId": "guid-or-null",
  "FileUrl": "https://...",
  "Duration": 180,
  "Lyrics": "...",
  "FileHash": "hash",
  "Thumbnail": "https://...",
  "ArtistIds": ["guid1", "guid2"]
}
```

---

### 4. Update Song ✅

```
PUT /Music/song/{songId}
Content-Type: application/json

{
  "Title": "Tên bài hát mới",
  "Thumbnail": "https://...",
  "Lyrics": "Lời bài hát...",
  "GenreIds": ["guid1", "guid2"]
}
```

**Params:**

- `songId` (Guid) - ID bài hát cần chỉnh sửa

**Note:** Chỉ chủ sở hữu bài hát mới có thể chỉnh sửa. Tất cả fields đều optional.

---

### 5. Delete Song ✅

```
DELETE /Music/song/{songId}
```

**Params:**

- `songId` (Guid) - ID bài hát cần xóa

**Note:** Chỉ chủ sở hữu bài hát mới có thể xóa

---

## 📀 ALBUM ENDPOINTS

### 6. Get All Albums (Search)

```
GET /Music/albums?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên album
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<AlbumDto\>

---

### 7. Get My Albums (Authorized) ✅

```
GET /Music/my-albums?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Albums

**Response:** PagingResult\<AlbumDto\> của chính mình

---

### 8. Create Album ✅

```
POST /Music/album
Content-Type: application/json

{
  "Title": "Album mới",
  "Thumbnail": "https://...",
  "ReleaseDate": "2026-01-30"
}
```

---

### 9. Update Album ✅

```
PUT /Music/album/{albumId}
Content-Type: application/json

{
  "Title": "Tên album mới",
  "Thumbnail": "https://...",
  "ReleaseDate": "2026-01-30"
}
```

**Params:**

- `albumId` (Guid) - ID album cần chỉnh sửa

**Note:** Chỉ chủ sở hữu album mới có thể chỉnh sửa. Tất cả fields đều optional.

---

### 10. Delete Album ✅

```
DELETE /Music/album/{albumId}
```

**Params:**

- `albumId` (Guid) - ID album cần xóa

**Note:** Chỉ chủ sở hữu album mới có thể xóa

---

### 11. Get Album Details (Xem chi tiết album + bài hát)

```
GET /Music/album/{albumId}?pageIndex=1&pageSize=10
```

**Params:**

- `albumId` (Guid) - ID album
- `pageIndex` (int) - Trang danh sách bài hát (mặc định 1)
- `pageSize` (int) - Số bài hát/trang (mặc định 10)

**Response:**

```json
{
  "Album": {
    "AlbumId": "guid",
    "Title": "Tên album",
    "Thumbnail": "https://...",
    "ReleaseDate": "2026-01-30",
    "ArtistId": "guid",
    "ArtistName": "Tên nghệ sĩ",
    "CreatedAt": "2026-01-30T10:00:00",
    "UpdatedAt": "2026-01-30T15:00:00"
  },
  "Songs": {
    "Items": [...],
    "PageIndex": 1,
    "PageSize": 10,
    "TotalRecords": 5,
    "TotalPages": 1
  }
}
```

---

### 12. Add Song to Album ✅

```
POST /Music/album/{albumId}/add-song/{songId}
```

**Params:**

- `albumId` (Guid) - ID album
- `songId` (Guid) - ID bài hát cần thêm

**Note:**

- Chỉ chủ sở hữu album VÀ bài hát mới có thể thực hiện
- Bài hát sẽ được gắn vào album (cập nhật album_id)

---

### 13. Remove Song from Album ✅

```
DELETE /Interaction/album/{albumId}/remove-song/{songId}
```

**Params:**

- `albumId` (Guid) - ID album
- `songId` (Guid) - ID bài hát cần xóa khỏi album

**Note:** Chỉ chủ sở hữu album mới có thể xóa bài hát khỏi album

---

## 📋 PLAYLIST ENDPOINTS

### 14. Get All Playlists (Search)

```
GET /Music/playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên playlist
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<PlaylistDto\>

---

### 15. Get My Playlists (Authorized) ✅

```
GET /Music/my-playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Playlists

**Response:** PagingResult\<PlaylistDto\> của chính mình

---

### 16. Create Playlist ✅

```
POST /Interaction/playlist
Content-Type: application/json

{
  "Title": "Playlist mới",
  "Description": "Mô tả (optional)"
}
```

---

### 17. Get Playlist Details ✅

```
GET /Interaction/playlist/{playlistId}?pageIndex=1&pageSize=10
```

**Params:**

- `playlistId` (Guid) - ID playlist
- `pageIndex` (int)
- `pageSize` (int)

**Response:**

```json
{
  "Playlist": {
    "PlaylistId": "guid",
    "Title": "...",
    "CreatedAt": "2026-01-30",
    "CreatedBy": "Tên người dùng"
  },
  "Songs": {
    "Data": [...],
    "TotalRecords": 10,
    "TotalPages": 1,
    "FromRecord": 1,
    "ToRecord": 10
  }
}
```

---

### 18. Update Playlist ✅

```
PUT /Interaction/playlist/{playlistId}
Content-Type: application/json

{
  "Title": "Tên playlist mới"
}
```

---

### 19. Delete Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}
```

**Note:** Chỉ chủ sở hữu playlist mới có thể xóa

---

## 🎶 PLAYLIST SONG MANAGEMENT

### 20. Add Song to Playlist ✅

```
POST /Interaction/playlist/{playlistId}/add-song/{songId}
```

**Params:**

- `playlistId` (Guid) - ID playlist
- `songId` (Guid) - ID bài hát

---

### 21. Remove Song from Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}/remove-song/{songId}
```

**Params:**

- `playlistId` (Guid)
- `songId` (Guid)

---

## ❤️ LIKE ENDPOINTS

### 22. Toggle Like Song ✅

```
POST /Interaction/like/{songId}
```

**Params:**

- `songId` (Guid) - ID bài hát

**Response:**

```json
{
  "IsLiked": true,
  "Message": "Đã thích bài hát"
}
```

---

### 23. Get Liked Songs ✅

```
GET /Interaction/liked-songs?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<Song\>

---

## 👥 FOLLOW ENDPOINTS

### 24. Toggle Follow User ✅

```
POST /Interaction/follow/{targetUserId}
```

**Params:**

- `targetUserId` (Guid) - ID người dùng cần theo dõi

**Response:**

```json
{
  "IsFollowing": true,
  "Message": "Đã theo dõi"
}
```

---

### 25. Get Following List ✅

```
GET /Interaction/followings?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<ArtistDto\>

---

## 🏷️ GENRE ENDPOINTS

### 26. Get All Genres

```
GET /Music/genres
```

**Response:** IEnumerable\<GenreDto\>

---

### 27. Create Genre ✅

```
POST /Music/genre
Content-Type: application/json

{
  "Name": "Rock",
  "ImageUrl": "https://..."
}
```

---

## 🤖 RECOMMENDATION ENDPOINTS

### Thuật toán đề xuất

```
┌──────────────────────────────────────────────────────────────────────────┐
│  Recommendation Algorithm (Content-Based Filtering)                      │
│                                                                          │
│  1. Lấy dữ liệu từ bảng user_song_stats (play_count, total_listen_time)│
│     ↓                                                                    │
│  2. Xác định Top Artists yêu thích:                                      │
│     • JOIN song_artists ON sa.artist_id                                  │
│     • ORDER BY SUM(play_count) DESC → lấy top 5 artist                  │
│     ↓                                                                    │
│  3. Xác định Top Genres yêu thích:                                       │
│     • JOIN song_genres ON sg.genre_id                                    │
│     • ORDER BY SUM(play_count) DESC → lấy top 5 genre                   │
│     ↓                                                                    │
│  4. Lấy bài hát/album từ artists + genres yêu thích                     │
│     ↓                                                                    │
│  5. Loại bỏ bài hát user đã nghe nhiều (có trong user_song_stats)        │
│     ↓                                                                    │
│  6. Trả về danh sách bài hát/album đề xuất                              │
│     (Cache trong Redis 10 phút)                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

### Database Tables sử dụng

| Bảng                | Mô tả                                                                                             |
| ------------------- | ------------------------------------------------------------------------------------------------- |
| `user_song_stats`   | Lưu `play_count`, `total_listen_time`, `skip_count`, `last_played` cho mỗi cặp (user_id, song_id) |
| `song_artists`      | Quan hệ N-N giữa songs và artists (`song_id`, `artist_id`)                                        |
| `song_genres`       | Quan hệ N-N giữa songs và genres (`song_id`, `genre_id`)                                          |
| `listening_history` | Lịch sử nghe (`user_id`, `song_id`, `listened_at`)                                                |

---

### 28. Get Recommended Songs ✅

```
GET /Recommendation/songs/{userId}?topN=20
```

**Params:**

- `userId` (Guid) - ID người dùng cần lấy đề xuất
- `topN` (int, optional) - Số bài hát đề xuất tối đa (mặc định 20)

**Response (200 OK):**

```json
{
  "Message": "Lấy danh sách bài hát đề xuất thành công",
  "Data": [
    {
      "Id": "guid",
      "Title": "Bài hát đề xuất",
      "Thumbnail": "https://...",
      "FileUrl": "https://...",
      "Duration": 240,
      "ArtistNames": "Artist 1, Artist 2",
      "ArtistIds": ["guid1", "guid2"],
      "CreatedAt": "2026-01-30T10:00:00"
    }
  ]
}
```

**Lưu ý:**

- Kết quả được cache trong Redis 10 phút (key: `recommendation:songs:{userId}`)
- Nếu user chưa có lịch sử nghe → trả về danh sách rỗng
- Thuật toán: Lấy bài hát từ Top Artists + Top Genres yêu thích → loại bỏ bài đã nghe nhiều

---

### 29. Get Recommended Albums ✅

```
GET /Recommendation/albums/{userId}?topN=10
```

**Params:**

- `userId` (Guid) - ID người dùng cần lấy đề xuất
- `topN` (int, optional) - Số album đề xuất tối đa (mặc định 10)

**Response (200 OK):**

```json
{
  "Message": "Lấy danh sách album đề xuất thành công",
  "Data": [
    {
      "AlbumId": "guid",
      "Title": "Album đề xuất",
      "Thumbnail": "https://...",
      "ReleaseDate": "2026-01-30",
      "ArtistName": "Tên nghệ sĩ",
      "CreatedAt": "2026-01-30T10:00:00"
    }
  ]
}
```

**Lưu ý:**

- Kết quả được cache trong Redis 10 phút (key: `recommendation:albums:{userId}`)
- Thuật toán: Lấy Top Artists yêu thích → lấy album của các artist đó

---

## 🔴 REDIS CACHING

### Cache Strategy

| Feature                 | Cache Key Pattern                     | TTL     | Invalidation                                |
| ----------------------- | ------------------------------------- | ------- | ------------------------------------------- |
| Genres                  | `genres:all`                          | 24h     | Khi tạo genre mới                           |
| Album Details           | `album:{id}:details:{page}:{size}`    | 10 phút | Khi update/delete album, add/remove song    |
| Liked Songs             | `liked_songs:{userId}:{page}:{size}`  | 10 phút | Khi toggle like                             |
| Playlist Details        | `playlist:{id}:details:{page}:{size}` | 10 phút | Khi update/delete playlist, add/remove song |
| Recommended Songs       | `recommendation:songs:{userId}`       | 10 phút | Tự hết hạn                                  |
| Recommended Albums      | `recommendation:albums:{userId}`      | 10 phút | Tự hết hạn                                  |
| Reset Password Token    | `reset_token:{token}`                 | 15 phút | Khi reset thành công                        |
| Blacklist Refresh Token | `blacklist_refresh_token:{token}`     | 7 ngày  | Tự hết hạn                                  |

---

## 📝 DATA MODELS

### SongDto

```json
{
  "Id": "guid",
  "Title": "Tên bài hát",
  "Thumbnail": "https://...",
  "FileUrl": "https://...",
  "Duration": 180,
  "ArtistNames": "Artist 1, Artist 2",
  "ArtistIds": ["guid1", "guid2"],
  "CreatedAt": "2026-01-30T10:00:00",
  "UpdatedAt": "2026-01-31T15:30:00"
}
```

### AlbumDto

```json
{
  "AlbumId": "guid",
  "Title": "Tên album",
  "Thumbnail": "https://...",
  "ReleaseDate": "2026-01-30",
  "ArtistName": "Tên nghệ sĩ",
  "CreatedAt": "2026-01-30T10:00:00",
  "UpdatedAt": "2026-01-31T15:30:00"
}
```

### PlaylistDto

```json
{
  "PlaylistId": "guid",
  "Title": "Tên playlist",
  "Description": "Mô tả",
  "CreatedBy": "Tên người dùng",
  "CreatedAt": "2026-01-30T10:00:00",
  "UpdatedAt": "2026-01-31T15:30:00",
  "SongCount": 5
}
```

### PagingResult\<T\>

```json
{
  "Data": [...],
  "TotalRecords": 100,
  "TotalPages": 10,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

## 🔐 Error Responses

### 400 Bad Request

```json
{
  "Message": "Thông báo lỗi"
}
```

### 401 Unauthorized

```json
{
  "Message": "Cần đăng nhập"
}
```

### 403 Forbidden

```json
{
  "Message": "Bạn không có quyền truy cập"
}
```

---

## 📌 Summary Table

| #   | Endpoint                                       | Method | Auth | Desc                         |
| --- | ---------------------------------------------- | ------ | ---- | ---------------------------- |
| -   | **AUTH**                                       |        |      |                              |
| A1  | `/Auth/login`                                  | POST   | ❌   | Đăng nhập                    |
| A2  | `/Auth/google-login`                           | POST   | ❌   | Đăng nhập Google             |
| A3  | `/Auth/register`                               | POST   | ❌   | Đăng ký                      |
| A4  | `/Auth/refresh-token`                          | POST   | ❌   | Làm mới token                |
| A5  | `/Auth/logout`                                 | POST   | ✅   | Đăng xuất                    |
| A6  | `/Auth/forgot-password`                        | POST   | ❌   | Quên mật khẩu                |
| A7  | `/Auth/reset-password`                         | POST   | ❌   | Đặt lại mật khẩu             |
| -   | **MUSIC**                                      |        |      |                              |
| 1   | `/Music/songs`                                 | GET    | ❌   | Tất cả bài hát (search)      |
| 2   | `/Music/my-songs`                              | GET    | ✅   | Bài hát của mình (search)    |
| 3   | `/Music/song`                                  | POST   | ✅   | Tạo bài hát                  |
| 4   | `/Music/song/{id}`                             | PUT    | ✅   | Cập nhật bài hát             |
| 5   | `/Music/song/{id}`                             | DELETE | ✅   | Xóa bài hát                  |
| 6   | `/Music/albums`                                | GET    | ❌   | Tất cả album (search)        |
| 7   | `/Music/my-albums`                             | GET    | ✅   | Album của mình (search)      |
| 8   | `/Music/album`                                 | POST   | ✅   | Tạo album                    |
| 9   | `/Music/album/{id}`                            | PUT    | ✅   | Cập nhật album               |
| 10  | `/Music/album/{id}`                            | DELETE | ✅   | Xóa album                    |
| 11  | `/Music/album/{id}`                            | GET    | ❌   | Chi tiết album + bài hát     |
| 12  | `/Music/album/{id}/add-song/{sid}`             | POST   | ✅   | Thêm bài hát vào album       |
| 13  | `/Music/playlists`                             | GET    | ❌   | Tất cả playlist (search)     |
| 14  | `/Music/my-playlists`                          | GET    | ✅   | Playlist của mình (search)   |
| -   | **INTERACTION**                                |        |      |                              |
| 15  | `/Interaction/playlist`                        | POST   | ✅   | Tạo playlist                 |
| 16  | `/Interaction/playlist/{id}`                   | GET    | ❌   | Chi tiết playlist            |
| 17  | `/Interaction/playlist/{id}`                   | PUT    | ✅   | Cập nhật playlist            |
| 18  | `/Interaction/playlist/{id}`                   | DELETE | ✅   | Xóa playlist                 |
| 19  | `/Interaction/playlist/{id}/add-song/{sid}`    | POST   | ✅   | Thêm bài hát vào playlist    |
| 20  | `/Interaction/playlist/{id}/remove-song/{sid}` | DELETE | ✅   | Xóa bài hát khỏi playlist    |
| 21  | `/Interaction/album/{id}/remove-song/{sid}`    | DELETE | ✅   | Xóa bài hát khỏi album       |
| 22  | `/Interaction/like/{songId}`                   | POST   | ✅   | Thích/bỏ thích bài hát       |
| 23  | `/Interaction/liked-songs`                     | GET    | ✅   | Danh sách bài hát yêu thích  |
| 24  | `/Interaction/follow/{userId}`                 | POST   | ✅   | Theo dõi/bỏ theo dõi user    |
| 25  | `/Interaction/followings`                      | GET    | ✅   | Danh sách user đang theo dõi |
| -   | **GENRE**                                      |        |      |                              |
| 26  | `/Music/genres`                                | GET    | ❌   | Tất cả thể loại              |
| 27  | `/Music/genre`                                 | POST   | ✅   | Tạo thể loại                 |
| -   | **RECOMMENDATION**                             |        |      |                              |
| 28  | `/Recommendation/songs/{userId}`               | GET    | ✅   | Đề xuất bài hát              |
| 29  | `/Recommendation/albums/{userId}`              | GET    | ✅   | Đề xuất album                |

---

**Total: 7 Auth + 29 Feature = 36 Endpoints** ✅
