# 🎵 Music Streaming Website - API Documentation

## Base URL

```
http://localhost:5111/api/v1
```

## Authentication

- Endpoints với ✅ cần token JWT trong Cookie `jwt`
- Gửi Authorization Header: `Authorization: Bearer {token}` hoặc Cookie

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
  "title": "Bài hát mới",
  "albumId": "guid-or-null",
  "fileUrl": "https://...",
  "duration": 180,
  "lyrics": "...",
  "fileHash": "hash",
  "thumbnail": "https://...",
  "artistIds": ["guid1", "guid2"]
}
```

---

### 4. Delete Song ✅

```
DELETE /Music/song/{songId}
```

**Params:**

- `songId` (Guid) - ID bài hát cần xóa

**Note:** Chỉ chủ sở hữu bài hát mới có thể xóa

---

## 📀 ALBUM ENDPOINTS

### 5. Get All Albums (Search)

```
GET /Music/albums?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên album
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult<AlbumDto>

---

### 6. Get My Albums (Authorized) ✅

```
GET /Music/my-albums?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Albums

**Response:** PagingResult<AlbumDto> của chính mình

---

### 7. Create Album ✅

```
POST /Music/album
Content-Type: application/json

{
  "title": "Album mới",
  "thumbnail": "https://...",
  "releaseDate": "2026-01-30"
}
```

---

### 8. Delete Album ✅

```
DELETE /Music/album/{albumId}
```

**Params:**

- `albumId` (Guid) - ID album cần xóa

**Note:** Chỉ chủ sở hữu album mới có thể xóa

---

## 📋 PLAYLIST ENDPOINTS

### 9. Get All Playlists (Search)

```
GET /Music/playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên playlist
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult<PlaylistDto>

---

### 10. Get My Playlists (Authorized) ✅

```
GET /Music/my-playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Playlists

**Response:** PagingResult<PlaylistDto> của chính mình

---

### 11. Create Playlist ✅

```
POST /Interaction/playlist
Content-Type: application/json

{
  "title": "Playlist mới",
  "description": "Mô tả (optional)"
}
```

---

### 12. Get Playlist Details ✅

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
  "playlist": {
    "playlistId": "guid",
    "title": "...",
    "createdAt": "2026-01-30",
    "createdBy": "Tên người dùng"
  },
  "songs": {
    "data": [...],
    "totalRecords": 10,
    "totalPages": 1,
    "fromRecord": 1,
    "toRecord": 10
  }
}
```

---

### 13. Update Playlist ✅

```
PUT /Interaction/playlist/{playlistId}
Content-Type: application/json

{
  "title": "Tên playlist mới"
}
```

---

### 14. Delete Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}
```

**Note:** Chỉ chủ sở hữu playlist mới có thể xóa

---

## 🎶 PLAYLIST SONG MANAGEMENT

### 15. Add Song to Playlist ✅

```
POST /Interaction/playlist/{playlistId}/add-song/{songId}
```

**Params:**

- `playlistId` (Guid) - ID playlist
- `songId` (Guid) - ID bài hát

---

### 16. Remove Song from Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}/remove-song/{songId}
```

**Params:**

- `playlistId` (Guid)
- `songId` (Guid)

---

## 💿 ALBUM SONG MANAGEMENT

### 17. Remove Song from Album ✅

```
DELETE /Interaction/album/{albumId}/remove-song/{songId}
```

**Params:**

- `albumId` (Guid)
- `songId` (Guid)

**Note:** Chỉ chủ sở hữu album mới có thể xóa bài hát khỏi album

---

## ❤️ LIKE ENDPOINTS

### 18. Toggle Like Song ✅

```
POST /Interaction/like/{songId}
```

**Params:**

- `songId` (Guid) - ID bài hát

**Response:**

```json
{
  "isLiked": true,
  "message": "Đã thích bài hát"
}
```

---

### 19. Get Liked Songs ✅

```
GET /Interaction/liked-songs?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult<Song>

---

## 👥 FOLLOW ENDPOINTS

### 20. Toggle Follow User ✅

```
POST /Interaction/follow/{targetUserId}
```

**Params:**

- `targetUserId` (Guid) - ID người dùng cần theo dõi

**Response:**

```json
{
  "isFollowing": true,
  "message": "Đã theo dõi"
}
```

---

### 21. Get Following List ✅

```
GET /Interaction/followings?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult<ArtistDto>

---

## 🏷️ GENRE ENDPOINTS

### 22. Get All Genres

```
GET /Music/genres
```

**Response:** IEnumerable<GenreDto>

---

### 23. Create Genre

```
POST /Music/genre
Content-Type: application/json

{
  "name": "Rock",
  "imageUrl": "https://..."
}
```

---

## 📝 DATA MODELS

### SongDto

```json
{
  "id": "guid",
  "title": "Tên bài hát",
  "thumbnail": "https://...",
  "fileUrl": "https://...",
  "duration": 180,
  "artistNames": "Artist 1, Artist 2",
  "artistIds": ["guid1", "guid2"]
}
```

### AlbumDto

```json
{
  "albumId": "guid",
  "title": "Tên album",
  "thumbnail": "https://...",
  "releaseDate": "2026-01-30",
  "artistName": "Tên nghệ sĩ"
}
```

### PlaylistDto

```json
{
  "playlistId": "guid",
  "title": "Tên playlist",
  "description": "Mô tả",
  "createdBy": "Tên người dùng",
  "createdAt": "2026-01-30",
  "songCount": 5
}
```

### PagingResult<T>

```json
{
  "data": [...],
  "totalRecords": 100,
  "totalPages": 10,
  "fromRecord": 1,
  "toRecord": 10
}
```

---

## 🔐 Error Responses

### 400 Bad Request

```json
{
  "message": "Thông báo lỗi"
}
```

### 401 Unauthorized

```json
{
  "message": "Cần đăng nhập"
}
```

### 403 Forbidden

```json
{
  "message": "Bạn không có quyền truy cập"
}
```

---

## 📌 Summary Table

| #   | Endpoint                                       | Method | Auth | Desc                         |
| --- | ---------------------------------------------- | ------ | ---- | ---------------------------- |
| 1   | `/Music/songs`                                 | GET    | ❌   | Tất cả bài hát (search)      |
| 2   | `/Music/my-songs`                              | GET    | ✅   | Bài hát của mình (search)    |
| 3   | `/Music/song`                                  | POST   | ✅   | Tạo bài hát                  |
| 4   | `/Music/song/{id}`                             | DELETE | ✅   | Xóa bài hát                  |
| 5   | `/Music/albums`                                | GET    | ❌   | Tất cả album (search)        |
| 6   | `/Music/my-albums`                             | GET    | ✅   | Album của mình (search)      |
| 7   | `/Music/album`                                 | POST   | ✅   | Tạo album                    |
| 8   | `/Music/album/{id}`                            | DELETE | ✅   | Xóa album                    |
| 9   | `/Music/playlists`                             | GET    | ❌   | Tất cả playlist (search)     |
| 10  | `/Music/my-playlists`                          | GET    | ✅   | Playlist của mình (search)   |
| 11  | `/Interaction/playlist`                        | POST   | ✅   | Tạo playlist                 |
| 12  | `/Interaction/playlist/{id}`                   | GET    | ❌   | Chi tiết playlist            |
| 13  | `/Interaction/playlist/{id}`                   | PUT    | ✅   | Cập nhật playlist            |
| 14  | `/Interaction/playlist/{id}`                   | DELETE | ✅   | Xóa playlist                 |
| 15  | `/Interaction/playlist/{id}/add-song/{sid}`    | POST   | ✅   | Thêm bài hát vào playlist    |
| 16  | `/Interaction/playlist/{id}/remove-song/{sid}` | DELETE | ✅   | Xóa bài hát khỏi playlist    |
| 17  | `/Interaction/album/{id}/remove-song/{sid}`    | DELETE | ✅   | Xóa bài hát khỏi album       |
| 18  | `/Interaction/like/{songId}`                   | POST   | ✅   | Thích/bỏ thích bài hát       |
| 19  | `/Interaction/liked-songs`                     | GET    | ✅   | Danh sách bài hát yêu thích  |
| 20  | `/Interaction/follow/{userId}`                 | POST   | ✅   | Theo dõi/bỏ theo dõi user    |
| 21  | `/Interaction/followings`                      | GET    | ✅   | Danh sách user đang theo dõi |
| 22  | `/Music/genres`                                | GET    | ❌   | Tất cả thể loại              |
| 23  | `/Music/genre`                                 | POST   | ✅   | Tạo thể loại                 |

---

**Total: 23 Endpoints** ✅
