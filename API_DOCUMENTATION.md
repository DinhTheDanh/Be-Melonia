# 🎵 Music Streaming Website - API Documentation

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

## 🎵 MUSIC ENDPOINTS

### 1. Get All Songs (Search)

```
GET /Music/songs?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên bài hát
- `pageIndex` (int) - Trang (mặc định 1)
- `pageSize` (int) - Số bản ghi/trang (mặc định 10)

**Response:** PagingResult<SongDto>

---

### 2. Get My Songs (Authorized) ✅

```
GET /Music/my-songs?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Songs

**Response:** PagingResult<SongDto> của chính mình

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

**Response:** PagingResult<AlbumDto>

---

### 7. Get My Albums (Authorized) ✅

```
GET /Music/my-albums?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Albums

**Response:** PagingResult<AlbumDto> của chính mình

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

**Response:** PagingResult<PlaylistDto>

---

### 15. Get My Playlists (Authorized) ✅

```
GET /Music/my-playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Playlists

**Response:** PagingResult<PlaylistDto> của chính mình

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

### 17. Add Song to Playlist ✅

```
POST /Interaction/playlist/{playlistId}/add-song/{songId}
```

**Params:**

- `playlistId` (Guid) - ID playlist
- `songId` (Guid) - ID bài hát

---

### 18. Remove Song from Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}/remove-song/{songId}
```

**Params:**

- `playlistId` (Guid)
- `songId` (Guid)

---

## 💿 ALBUM SONG MANAGEMENT

### 19. Remove Song from Album ✅

```
DELETE /Interaction/album/{albumId}/remove-song/{songId}
```

**Params:**

- `albumId` (Guid)
- `songId` (Guid)

**Note:** Chỉ chủ sở hữu album mới có thể xóa bài hát khỏi album

---

## ❤️ LIKE ENDPOINTS

### 20. Toggle Like Song ✅

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

### 21. Get Liked Songs ✅

```
GET /Interaction/liked-songs?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult<Song>

---

## 👥 FOLLOW ENDPOINTS

### 22. Toggle Follow User ✅

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

### 23. Get Following List ✅

```
GET /Interaction/followings?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult<ArtistDto>

---

## 🏷️ GENRE ENDPOINTS

### 24. Get All Genres

```
GET /Music/genres
```

**Response:** IEnumerable<GenreDto>

---

### 25. Create Genre

```
POST /Music/genre
Content-Type: application/json

{
  "Name": "Rock",
  "ImageUrl": "https://..."
}
```

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

### PagingResult<T>

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
| 11  | `/Music/playlists`                             | GET    | ❌   | Tất cả playlist (search)     |
| 12  | `/Music/my-playlists`                          | GET    | ✅   | Playlist của mình (search)   |
| 13  | `/Interaction/playlist`                        | POST   | ✅   | Tạo playlist                 |
| 14  | `/Interaction/playlist/{id}`                   | GET    | ❌   | Chi tiết playlist            |
| 15  | `/Interaction/playlist/{id}`                   | PUT    | ✅   | Cập nhật playlist            |
| 16  | `/Interaction/playlist/{id}`                   | DELETE | ✅   | Xóa playlist                 |
| 17  | `/Interaction/playlist/{id}/add-song/{sid}`    | POST   | ✅   | Thêm bài hát vào playlist    |
| 18  | `/Interaction/playlist/{id}/remove-song/{sid}` | DELETE | ✅   | Xóa bài hát khỏi playlist    |
| 19  | `/Interaction/album/{id}/remove-song/{sid}`    | DELETE | ✅   | Xóa bài hát khỏi album       |
| 20  | `/Interaction/like/{songId}`                   | POST   | ✅   | Thích/bỏ thích bài hát       |
| 21  | `/Interaction/liked-songs`                     | GET    | ✅   | Danh sách bài hát yêu thích  |
| 22  | `/Interaction/follow/{userId}`                 | POST   | ✅   | Theo dõi/bỏ theo dõi user    |
| 23  | `/Interaction/followings`                      | GET    | ✅   | Danh sách user đang theo dõi |
| 24  | `/Music/genres`                                | GET    | ❌   | Tất cả thể loại              |
| 25  | `/Music/genre`                                 | POST   | ✅   | Tạo thể loại                 |

---

**Total: 25 Endpoints** ✅
