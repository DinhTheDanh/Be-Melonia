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
│     • Access Token (trong Cookie jwt) - hết hạn sau 60 phút     │
│     • Refresh Token (trong HTTP-Only Cookie) - hết hạn sau 7 ngày│
│     ↓                                                           │
│  3. Cookie tự động lưu cả Access Token và Refresh Token         │
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
  "Identifier": "user@example.com",
  "Password": "password123"
}
```

> `Identifier` có thể là **Email** hoặc **Username**

**Response (200 OK):**

```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "abc123def456...",
  "FullName": "Nguyễn Văn A",
  "Avatar": "https://ui-avatars.com/api/?name=Nguyen+Van+A&background=random&color=fff&size=128&bold=true",
  "Role": "Artist",
  "IsNewUser": false
}
```

**Cookies được set:**

```
Set-Cookie: jwt=eyJhbGciOi...; Path=/; Expires=...
Set-Cookie: refresh_token=abc123...; HttpOnly; Path=/; Expires=... (7 ngày)
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

**Response (200 OK):**

```json
{
  "FullName": "Nguyễn Văn A",
  "Avatar": "https://lh3.googleusercontent.com/...",
  "Role": "User",
  "IsNewUser": true
}
```

> **Lưu ý:** Google Login **không trả Token trong body** mà chỉ set vào Cookie (`jwt` và `refresh_token`).
> `IsNewUser = true` khi user lần đầu đăng nhập bằng Google (tự tạo tài khoản).

---

### 3. Register

```
POST /Auth/register
Content-Type: application/json

{
  "Username": "nguyenvana",
  "Email": "user@example.com",
  "Password": "password123",
  "FullName": "Nguyễn Văn A",
  "Avatar": "https://... (optional)",
  "FavoriteGenreIds": ["genre-guid-1", "genre-guid-2"]
}
```

**Response (200 OK):**

```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "abc123def456...",
  "FullName": "Nguyễn Văn A",
  "Avatar": "https://ui-avatars.com/api/?name=Nguyen+Van+A&background=random&color=fff&size=128&bold=true",
  "Role": "User",
  "IsNewUser": true
}
```

> Avatar tự tạo từ UI Avatars nếu không truyền. Cookie `jwt` được set tự động.

---

### 4. Refresh Token

```
POST /Auth/refresh-token
```

**Note:**

- Không cần body
- Refresh token được gửi tự động qua Cookie `refresh_token`
- FE chỉ cần gọi khi nhận lỗi 401 (Access Token hết hạn)
- Server kiểm tra blacklist Redis trước khi xử lý

**Response (200 OK):**

```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "newRefreshToken..."
}
```

> Cả 2 cookie (`jwt` và `refresh_token`) đều được cập nhật mới.

---

### 5. Logout ✅

```
POST /Auth/logout
```

**Response (200 OK):**

```json
{
  "Message": "Đăng xuất thành công"
}
```

> Xóa refresh token khỏi database, blacklist token trong Redis (7 ngày), xóa cookie `jwt` và `refresh_token`.

---

### 6. Set User Role ✅

```
POST /Auth/set-role
Content-Type: application/json

{
  "Role": "Artist"
}
```

> `Role` chấp nhận: `"User"` hoặc `"Artist"`

**Response (200 OK):**

```json
{
  "Message": "Cập nhật vai trò thành công"
}
```

> Cookie `jwt` được cập nhật với token mới chứa Role mới.

---

### 7. Change Password ✅

```
PUT /Auth/change-password
Content-Type: application/json

{
  "CurrentPassword": "oldpassword123",
  "NewPassword": "newpassword123",
  "ConfirmPassword": "newpassword123"
}
```

**Response (200 OK):**

```json
{
  "Message": "Đổi mật khẩu thành công. Email thông báo đã được gửi."
}
```

---

### 8. Forgot Password

```
POST /Auth/forgot-password
Content-Type: application/json

{
  "Email": "user@example.com"
}
```

**Response (200 OK):**

```json
{
  "Message": "Gửi Email thành công!!"
}
```

> Gửi email chứa link reset password. Token được lưu trong Redis với TTL 15 phút.

---

### 9. Reset Password

```
POST /Auth/reset-password
Content-Type: application/json

{
  "Token": "reset-token-from-email",
  "NewPassword": "newpassword123"
}
```

**Response (200 OK):**

```json
{
  "Message": "Đặt lại mật khẩu thành công. Vui lòng đăng nhập."
}
```

---

## 👤 USER ENDPOINTS

### 10. Get Profile ✅

```
GET /User/profile
```

**Response (200 OK):**

```json
{
  "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
  "Username": "dinhthedanh",
  "Email": "danh@example.com",
  "FullName": "Dinh The Danh",
  "Avatar": "https://ui-avatars.com/api/?name=Dinh+The+Danh&background=random&color=fff&size=128&bold=true",
  "Banner": "https://res.cloudinary.com/.../banner.jpg",
  "Bio": "Mô tả về bản thân",
  "ArtistType": "Singer",
  "Role": "Artist"
}
```

---

### 11. Update Profile ✅

```
PUT /User/profile
Content-Type: application/json

{
  "FullName": "Dinh The Danh",
  "Bio": "Mô tả mới",
  "Avatar": "https://...",
  "Banner": "https://...",
  "ArtistType": "Singer"
}
```

> `FullName` là required, còn lại optional.

**Response (200 OK):**

```json
{
  "Message": "Cập nhật hồ sơ thành công!"
}
```

---

### 12. Update Interests ✅

```
POST /User/update-interests
Content-Type: application/json

["genre-guid-1", "genre-guid-2", "genre-guid-3"]
```

> Body là mảng Guid của các genre yêu thích.

**Response (200 OK):**

```json
{
  "Message": "Cập nhật sở thích thành công!"
}
```

---

## 🎤 ARTIST ENDPOINTS

### 13. Get Artists (Search)

```
GET /Artist?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên nghệ sĩ
- `pageIndex` (int) - Trang (mặc định 1)
- `pageSize` (int) - Số bản ghi/trang (mặc định 10)

**Response:** PagingResult\<ArtistDto\>

```json
{
  "Data": [
    {
      "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
      "FullName": "Dinh The Danh",
      "Avatar": "https://ui-avatars.com/api/?name=Dinh+The+Danh&...",
      "Banner": "https://res.cloudinary.com/.../banner.jpg",
      "Bio": "Ca sĩ, nhạc sĩ",
      "ArtistType": "Singer"
    }
  ],
  "TotalRecords": 15,
  "TotalPages": 2,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

### 14. Get Songs by Artist

```
GET /Artist/{artistId}/songs?pageIndex=1&pageSize=10
```

**Params:**

- `artistId` (Guid) - ID nghệ sĩ
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<SongDto\>

```json
{
  "Data": [
    {
      "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
      "Title": "Người Ấy",
      "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
      "FileUrl": "https://res.cloudinary.com/.../song.mp3",
      "Duration": 261,
      "AlbumId": "1ef6127f-1415-4bb2-ac51-2e7a75cd9689",
      "AlbumTitle": "abc",
      "ArtistNames": "Dinh The Danh",
      "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
      "CreatedAt": "2026-01-27T00:07:34",
      "UpdatedAt": "2026-03-01T15:25:19"
    }
  ],
  "TotalRecords": 8,
  "TotalPages": 1,
  "FromRecord": 1,
  "ToRecord": 8
}
```

---

## 📁 FILE ENDPOINTS

### 15. Upload Image

```
POST /File/upload-image
Content-Type: multipart/form-data

file: [binary image file]
```

**Response (200 OK):**

```json
{
  "Url": "https://res.cloudinary.com/xxx/image/upload/v123/music-streaming/image.webp"
}
```

---

### 16. Upload Audio

```
POST /File/upload-audio
Content-Type: multipart/form-data

file: [binary audio file]
```

**Response (200 OK):**

```json
{
  "Url": "https://res.cloudinary.com/xxx/video/upload/v123/music-streaming/songs/audio.mp3"
}
```

---

### 17. Get Cloudinary Signature (Client-side Upload)

```
GET /File/signature
```

**Response (200 OK):**

```json
{
  "CloudName": "your-cloud-name",
  "ApiKey": "123456789",
  "Timestamp": 1709654400,
  "Signature": "a1b2c3d4e5f6...",
  "Folder": "music-streaming/songs"
}
```

> Dùng cho FE upload trực tiếp lên Cloudinary (không qua server). Gửi kèm `Signature`, `Timestamp`, `ApiKey` trong form data lên Cloudinary.

---

## 🎵 MUSIC ENDPOINTS

### 18. Get All Songs (Search)

```
GET /Music/songs?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên bài hát
- `pageIndex` (int) - Trang (mặc định 1)
- `pageSize` (int) - Số bản ghi/trang (mặc định 10)

**Response:** PagingResult\<SongDto\>

```json
{
  "Data": [
    {
      "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
      "Title": "Người Ấy",
      "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
      "FileUrl": "https://res.cloudinary.com/.../song.mp3",
      "Duration": 261,
      "AlbumId": "1ef6127f-1415-4bb2-ac51-2e7a75cd9689",
      "AlbumTitle": "abc",
      "ArtistNames": "Dinh The Danh",
      "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
      "CreatedAt": "2026-01-27T00:07:34",
      "UpdatedAt": "2026-03-01T15:25:19"
    }
  ],
  "TotalRecords": 50,
  "TotalPages": 5,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

### 19. Get My Songs ✅

```
GET /Music/my-songs?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Songs

**Response:** PagingResult\<SongDto\> — cấu trúc giống Get All Songs, chỉ lọc bài hát của user hiện tại.

---

### 20. Create Song ✅

```
POST /Music/song
Content-Type: application/json

{
  "Title": "Bài hát mới",
  "FileUrl": "https://res.cloudinary.com/.../song.mp3",
  "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
  "AlbumId": "guid-or-null",
  "Thumbnail": "https://... (optional, tự tạo nếu null)",
  "Duration": 180,
  "GenreIds": ["genre-guid-1", "genre-guid-2"],
  "Lyrics": "Lời bài hát... (optional)",
  "FileHash": "4a0dcd618452d123fab76723c54ae035 (optional)"
}
```

**Response (200 OK):** Song entity

```json
{
  "SongId": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
  "Title": "Bài hát mới",
  "AlbumId": null,
  "FileUrl": "https://res.cloudinary.com/.../song.mp3",
  "Thumbnail": "https://api.dicebear.com/7.x/shapes/svg?seed=...",
  "Duration": 180,
  "Lyrics": "Lời bài hát...",
  "IsPublic": true,
  "CreatedAt": "2026-03-07T10:00:00",
  "UpdatedAt": null,
  "FileHash": "4a0dcd618452d123fab76723c54ae035"
}
```

> **Lưu ý:** Response trả về **Song entity** (có `SongId`), KHÔNG phải SongDto. Nếu không truyền Thumbnail, server tự tạo DiceBear avatar.

---

### 21. Update Song ✅

```
PUT /Music/song/{songId}
Content-Type: application/json

{
  "Title": "Tên bài hát mới",
  "Thumbnail": "https://...",
  "Lyrics": "Lời bài hát...",
  "IsPublic": true,
  "AlbumId": "guid | null",
  "GenreIds": ["guid1", "guid2"]
}
```

> Tất cả fields đều **optional**. Chỉ chủ sở hữu bài hát mới có thể chỉnh sửa.

**Response (200 OK):**

```json
{
  "Message": "Cập nhật bài hát thành công"
}
```

---

### 22. Delete Song ✅

```
DELETE /Music/song/{songId}
```

**Response (200 OK):**

```json
{
  "Message": "Xóa bài hát thành công"
}
```

> Chỉ chủ sở hữu bài hát mới có thể xóa.

---

### 23. Check File Hash

```
GET /Music/check-hash/{hash}
```

> Kiểm tra file nhạc đã tồn tại chưa (tránh upload trùng).

**Response — File đã tồn tại:**

```json
{
  "Exists": true,
  "FileUrl": "https://res.cloudinary.com/.../song.mp3",
  "Duration": 261
}
```

**Response — File chưa tồn tại:**

```json
{
  "Exists": false
}
```

---

## 📀 ALBUM ENDPOINTS

### 24. Get All Albums (Search)

```
GET /Music/albums?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên album
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<AlbumDto\>

```json
{
  "Data": [
    {
      "AlbumId": "94f4b80a-0a95-42d7-858a-a0a4f80d80ab",
      "Title": "bcd",
      "Thumbnail": "https://res.cloudinary.com/.../album.webp",
      "ArtistId": "514685e6-5141-40c6-84c6-37c43da959aa",
      "ArtistName": "Dinh The Danh",
      "ReleaseDate": "2026-02-07T22:01:48",
      "CreatedAt": "2026-02-07T22:01:48",
      "UpdatedAt": null
    }
  ],
  "TotalRecords": 10,
  "TotalPages": 1,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

### 25. Get My Albums ✅

```
GET /Music/my-albums?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Albums

**Response:** PagingResult\<AlbumDto\> — cấu trúc giống Get All Albums, chỉ lọc album của user hiện tại.

---

### 26. Create Album ✅

```
POST /Music/album
Content-Type: application/json

{
  "Title": "Album mới",
  "Thumbnail": "https://... (optional, tự tạo nếu null)"
}
```

**Response (200 OK):** Album entity

```json
{
  "AlbumId": "94f4b80a-0a95-42d7-858a-a0a4f80d80ab",
  "Title": "Album mới",
  "ArtistId": "514685e6-5141-40c6-84c6-37c43da959aa",
  "Thumbnail": "https://api.dicebear.com/7.x/shapes/svg?seed=...",
  "ReleaseDate": "2026-03-07T10:00:00",
  "CreatedAt": "2026-03-07T10:00:00",
  "UpdatedAt": null
}
```

> Nếu không truyền Thumbnail, server tự tạo DiceBear avatar.

---

### 27. Update Album ✅

```
PUT /Music/album/{albumId}
Content-Type: application/json

{
  "Title": "Tên album mới",
  "Thumbnail": "https://...",
  "ReleaseDate": "2026-01-30"
}
```

> `Title` required, `Thumbnail` và `ReleaseDate` optional. Chỉ chủ sở hữu mới được sửa.

**Response (200 OK):**

```json
{
  "Message": "Cập nhật album thành công"
}
```

---

### 28. Delete Album ✅

```
DELETE /Music/album/{albumId}
```

**Response (200 OK):**

```json
{
  "Message": "Xóa album thành công"
}
```

> Chỉ chủ sở hữu album mới có thể xóa.

---

### 29. Get Album Details

```
GET /Music/album/{albumId}?pageIndex=1&pageSize=10
```

**Params:**

- `albumId` (Guid) - ID album
- `pageIndex` (int) - Trang danh sách bài hát (mặc định 1)
- `pageSize` (int) - Số bài hát/trang (mặc định 10)

**Response (200 OK):**

```json
{
  "Album": {
    "AlbumId": "94f4b80a-0a95-42d7-858a-a0a4f80d80ab",
    "Title": "bcd",
    "Thumbnail": "https://res.cloudinary.com/.../album.webp",
    "ArtistId": "514685e6-5141-40c6-84c6-37c43da959aa",
    "ArtistName": "Dinh The Danh",
    "ReleaseDate": "2026-02-07T22:01:48",
    "CreatedAt": "2026-02-07T22:01:48",
    "UpdatedAt": null
  },
  "Songs": {
    "Data": [
      {
        "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
        "Title": "Người Ấy",
        "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
        "FileUrl": "https://res.cloudinary.com/.../song.mp3",
        "Duration": 261,
        "AlbumId": "94f4b80a-0a95-42d7-858a-a0a4f80d80ab",
        "AlbumTitle": "bcd",
        "ArtistNames": "Dinh The Danh",
        "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
        "CreatedAt": "2026-01-27T00:07:34",
        "UpdatedAt": null
      }
    ],
    "TotalRecords": 5,
    "TotalPages": 1,
    "FromRecord": 1,
    "ToRecord": 5
  }
}
```

**Response (404):**

```json
{
  "Message": "Album không tồn tại"
}
```

---

### 30. Add Song to Album ✅

```
POST /Music/album/{albumId}/add-song/{songId}
```

**Response (200 OK):**

```json
{
  "Message": "Thêm bài hát vào album thành công"
}
```

> Chỉ chủ sở hữu album VÀ bài hát mới có thể thực hiện. Bài hát sẽ được gắn `album_id`.

---

### 31. Remove Song from Album ✅

```
DELETE /Interaction/album/{albumId}/remove-song/{songId}
```

**Response (200 OK):**

```json
{
  "Message": "Đã xóa bài hát khỏi album thành công"
}
```

> Chỉ chủ sở hữu album mới có thể xóa bài hát khỏi album.

---

## 📋 PLAYLIST ENDPOINTS

### 32. Get All Playlists (Search)

```
GET /Music/playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:**

- `keyword` (string, optional) - Tìm kiếm theo tên playlist
- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<PlaylistDto\>

```json
{
  "Data": [
    {
      "PlaylistId": "8129dfc2-221a-4e09-9ca1-cff1f71e27cd",
      "Title": "Danh sách phát của tôi #1",
      "Thumbnail": "https://res.cloudinary.com/.../playlist.jpg",
      "Description": "Mô tả playlist",
      "CreatedBy": "Dinh The Danh",
      "CreatedAt": "2026-02-24T21:46:13",
      "UpdatedAt": "2026-03-01T16:43:20",
      "SongCount": 5
    }
  ],
  "TotalRecords": 3,
  "TotalPages": 1,
  "FromRecord": 1,
  "ToRecord": 3
}
```

---

### 33. Get My Playlists ✅

```
GET /Music/my-playlists?keyword=&pageIndex=1&pageSize=10
```

**Params:** Giống Get All Playlists

**Response:** PagingResult\<PlaylistDto\> — cấu trúc giống Get All Playlists, chỉ lọc playlist của user hiện tại.

---

### 34. Create Playlist ✅

```
POST /Interaction/playlist
Content-Type: application/json

{
  "Title": "Playlist mới",
  "Thumbnail": "https://... (optional)",
  "IsPublic": true
}
```

**Response (200 OK):** Playlist entity

```json
{
  "PlaylistId": "8129dfc2-221a-4e09-9ca1-cff1f71e27cd",
  "Title": "Playlist mới",
  "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
  "Thumbnail": null,
  "Description": null,
  "IsPublic": true,
  "CreatedAt": "2026-03-07T10:00:00",
  "UpdatedAt": null
}
```

---

### 35. Get Playlist Details ✅

```
GET /Interaction/playlist/{playlistId}?pageIndex=1&pageSize=10
```

**Params:**

- `playlistId` (Guid) - ID playlist
- `pageIndex` (int)
- `pageSize` (int)

**Response (200 OK):**

```json
{
  "Playlist": {
    "PlaylistId": "8129dfc2-221a-4e09-9ca1-cff1f71e27cd",
    "Title": "Danh sách phát của tôi #1",
    "Thumbnail": "https://res.cloudinary.com/.../playlist.jpg",
    "Description": "Mô tả playlist",
    "CreatedAt": "2026-02-24T21:46:13",
    "CreatedBy": "Dinh The Danh",
    "CreatedById": "514685e6-5141-40c6-84c6-37c43da959aa"
  },
  "Songs": {
    "Data": [
      {
        "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
        "Title": "Người Ấy",
        "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
        "FileUrl": "https://res.cloudinary.com/.../song.mp3",
        "Duration": 261,
        "AlbumId": "1ef6127f-1415-4bb2-ac51-2e7a75cd9689",
        "AlbumTitle": "abc",
        "ArtistNames": "Dinh The Danh",
        "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
        "CreatedAt": "2026-01-27T00:07:34",
        "UpdatedAt": null
      }
    ],
    "TotalRecords": 10,
    "TotalPages": 1,
    "FromRecord": 1,
    "ToRecord": 10
  }
}
```

**Response (404):**

```json
{
  "Message": "Playlist không tồn tại"
}
```

---

### 36. Update Playlist ✅

```
PUT /Interaction/playlist/{playlistId}
Content-Type: application/json

{
  "Title": "Tên playlist mới",
  "Thumbnail": "https://...",
  "Description": "Mô tả playlist",
  "IsPublic": false
}
```

> `Title` required, `Thumbnail`, `Description` và `IsPublic` optional. Chỉ chủ sở hữu mới được sửa.

**Response (200 OK):**

```json
{
  "Message": "Cập nhật playlist thành công",
  "Data": {
    "PlaylistId": "8129dfc2-221a-4e09-9ca1-cff1f71e27cd",
    "Title": "Tên playlist mới",
    "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
    "Thumbnail": "https://...",
    "Description": "Mô tả playlist",
    "IsPublic": false,
    "CreatedAt": "2026-02-24T21:46:13",
    "UpdatedAt": "2026-03-07T10:00:00"
  }
}
```

---

### 37. Toggle Playlist Visibility ✅

```
PATCH /Interaction/playlist/{playlistId}/toggle-visibility
```

> Chuyển đổi trạng thái công khai/riêng tư của playlist. Chỉ chủ sở hữu mới có quyền thay đổi.

**Response (200 OK):**

```json
{
  "IsPublic": false,
  "Message": "Playlist đã được đặt thành riêng tư"
}
```

hoặc:

```json
{
  "IsPublic": true,
  "Message": "Playlist đã được đặt thành công khai"
}
```

**Response (403):**

```json
{
  "Message": "Bạn không có quyền chỉnh sửa playlist này."
}
```

**Response (404):**

```json
{
  "Message": "Playlist không tồn tại."
}
```

---

### 38. Delete Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}
```

**Response (200 OK):**

```json
{
  "Message": "Đã xóa playlist thành công"
}
```

> Chỉ chủ sở hữu playlist mới có thể xóa.

---

## 🎶 PLAYLIST SONG MANAGEMENT

### 39. Add Song to Playlist ✅

```
POST /Interaction/playlist/{playlistId}/add-song/{songId}
```

**Response (200 OK):**

```json
{
  "Message": "Thêm bài hát vào playlist thành công"
}
```

---

### 40. Remove Song from Playlist ✅

```
DELETE /Interaction/playlist/{playlistId}/remove-song/{songId}
```

**Response (200 OK):**

```json
{
  "Message": "Xóa bài hát khỏi playlist thành công"
}
```

---

## ❤️ LIKE ENDPOINTS

### 41. Toggle Like Song ✅

```
POST /Interaction/like/{songId}
```

**Response (200 OK) — Thích:**

```json
{
  "IsLiked": true,
  "Message": "Đã thích bài hát"
}
```

**Response (200 OK) — Bỏ thích:**

```json
{
  "IsLiked": false,
  "Message": "Đã bỏ thích bài hát"
}
```

---

### 42. Get Liked Songs ✅

```
GET /Interaction/liked-songs?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<SongDto\>

```json
{
  "Data": [
    {
      "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
      "Title": "Người Ấy",
      "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
      "FileUrl": "https://res.cloudinary.com/.../song.mp3",
      "Duration": 261,
      "AlbumId": "1ef6127f-1415-4bb2-ac51-2e7a75cd9689",
      "AlbumTitle": "abc",
      "ArtistNames": "Dinh The Danh",
      "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
      "CreatedAt": "2026-01-27T00:07:34",
      "UpdatedAt": "2026-03-01T15:25:19"
    }
  ],
  "TotalRecords": 20,
  "TotalPages": 2,
  "FromRecord": 1,
  "ToRecord": 10
}
```

---

## 👥 FOLLOW ENDPOINTS

### 43. Toggle Follow User ✅

```
POST /Interaction/follow/{targetUserId}
```

**Response (200 OK) — Theo dõi:**

```json
{
  "IsFollowing": true,
  "Message": "Đã theo dõi"
}
```

**Response (200 OK) — Bỏ theo dõi:**

```json
{
  "IsFollowing": false,
  "Message": "Đã bỏ theo dõi"
}
```

---

### 44. Get Following List ✅

```
GET /Interaction/followings?pageIndex=1&pageSize=10
```

**Params:**

- `pageIndex` (int)
- `pageSize` (int)

**Response:** PagingResult\<ArtistDto\>

```json
{
  "Data": [
    {
      "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
      "FullName": "Dinh The Danh",
      "Avatar": "https://ui-avatars.com/api/?name=Dinh+The+Danh&...",
      "Banner": "https://res.cloudinary.com/.../banner.jpg",
      "Bio": "Ca sĩ, nhạc sĩ",
      "ArtistType": "Singer"
    }
  ],
  "TotalRecords": 5,
  "TotalPages": 1,
  "FromRecord": 1,
  "ToRecord": 5
}
```

---

## 🏷️ GENRE ENDPOINTS

### 45. Get All Genres

```
GET /Music/genres
```

**Response (200 OK):** Array\<GenreDto\>

```json
[
  {
    "Id": "b1c2d3e4-5678-90ab-cdef-1234567890ab",
    "Name": "Pop",
    "ImageUrl": "https://res.cloudinary.com/.../pop.jpg"
  },
  {
    "Id": "c2d3e4f5-6789-01bc-defa-2345678901bc",
    "Name": "Rock",
    "ImageUrl": "https://res.cloudinary.com/.../rock.jpg"
  }
]
```

> **Lưu ý:** Trả về mảng trực tiếp, KHÔNG wrap trong PagingResult.

---

### 46. Create Genre ✅

```
POST /Music/genre
Content-Type: application/json

{
  "Name": "Rock",
  "ImageUrl": "https://..."
}
```

**Response (200 OK):**

```json
{
  "Message": "Tạo thể loại thành công",
  "Data": {
    "Id": "b1c2d3e4-5678-90ab-cdef-1234567890ab",
    "Name": "Rock",
    "ImageUrl": "https://..."
  }
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

### 47. Get Recommended Songs ✅

```
GET /Recommendation/songs/{userId}?topN=20
```

**Params:**

- `userId` (Guid) - ID người dùng cần lấy đề xuất
- `topN` (int, optional) - Số bài hát đề xuất tối đa (mặc định 20)

**Thuật toán đề xuất dựa trên:**

1. Bảng `user_song_stats`: Lấy top nghệ sĩ và thể loại mà user nghe nhiều nhất
2. Lấy bài hát từ các nghệ sĩ + thể loại yêu thích
3. Loại bỏ bài hát user đã nghe nhiều
4. Cache kết quả trong Redis 10 phút

> **FE cần:** Gửi `userId` hợp lệ (lấy từ JWT hoặc profile). Nếu user chưa có lịch sử nghe → trả về mảng rỗng.

**Response (200 OK):**

```json
{
  "Message": "Lấy danh sách bài hát đề xuất thành công",
  "Data": [
    {
      "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
      "Title": "Bài hát đề xuất",
      "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
      "FileUrl": "https://res.cloudinary.com/.../song.mp3",
      "Duration": 240,
      "AlbumId": null,
      "AlbumTitle": null,
      "ArtistNames": "Artist 1, Artist 2",
      "ArtistIds": ["guid1", "guid2"],
      "CreatedAt": "2026-01-30T10:00:00",
      "UpdatedAt": null
    }
  ]
}
```

---

### 48. Get Recommended Albums ✅

```
GET /Recommendation/albums/{userId}?topN=10
```

**Params:**

- `userId` (Guid) - ID người dùng cần lấy đề xuất
- `topN` (int, optional) - Số album đề xuất tối đa (mặc định 10)

**Thuật toán đề xuất dựa trên:**

1. Bảng `user_song_stats`: Lấy top nghệ sĩ mà user nghe nhiều nhất
2. Lấy album của các nghệ sĩ đó
3. Cache kết quả trong Redis 10 phút

> **FE cần:** Gửi `userId` hợp lệ. Nếu user chưa có lịch sử nghe → Data sẽ là mảng rỗng.

**Response (200 OK):**

```json
{
  "Message": "Lấy danh sách album đề xuất thành công",
  "Data": [
    {
      "AlbumId": "94f4b80a-0a95-42d7-858a-a0a4f80d80ab",
      "Title": "Album đề xuất",
      "Thumbnail": "https://res.cloudinary.com/.../album.webp",
      "ArtistId": "514685e6-5141-40c6-84c6-37c43da959aa",
      "ArtistName": "Dinh The Danh",
      "ReleaseDate": "2026-01-30T00:00:00",
      "CreatedAt": "2026-01-30T10:00:00",
      "UpdatedAt": null
    }
  ]
}
```

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
  "Id": "a6f970fe-7549-41e1-8244-d1a2aa5aae1d",
  "Title": "Người Ấy",
  "Thumbnail": "https://res.cloudinary.com/.../image.jpg",
  "FileUrl": "https://res.cloudinary.com/.../song.mp3",
  "Duration": 261,
  "AlbumId": "1ef6127f-1415-4bb2-ac51-2e7a75cd9689",
  "AlbumTitle": "abc",
  "ArtistNames": "Dinh The Danh",
  "ArtistIds": ["514685e6-5141-40c6-84c6-37c43da959aa"],
  "CreatedAt": "2026-01-27T00:07:34",
  "UpdatedAt": "2026-03-01T15:25:19"
}
```

| Field       | Type     | Nullable | Mô tả                                     |
| ----------- | -------- | -------- | ----------------------------------------- |
| Id          | Guid     | ❌       | ID bài hát                                |
| Title       | string   | ❌       | Tên bài hát                               |
| Thumbnail   | string   | ✅       | Ảnh bìa                                   |
| FileUrl     | string   | ❌       | URL file nhạc (Cloudinary)                |
| Duration    | int      | ❌       | Thời lượng (giây)                         |
| AlbumId     | Guid     | ✅       | ID album chứa bài hát (null nếu không có) |
| AlbumTitle  | string   | ✅       | Tên album (null nếu không có)             |
| ArtistNames | string   | ✅       | Tên nghệ sĩ, phân cách bằng ", "          |
| ArtistIds   | Guid[]   | ❌       | Danh sách ID nghệ sĩ                      |
| CreatedAt   | DateTime | ❌       | Ngày tạo                                  |
| UpdatedAt   | DateTime | ✅       | Ngày cập nhật                             |

### AlbumDto

```json
{
  "AlbumId": "94f4b80a-0a95-42d7-858a-a0a4f80d80ab",
  "Title": "bcd",
  "Thumbnail": "https://res.cloudinary.com/.../album.webp",
  "ArtistId": "514685e6-5141-40c6-84c6-37c43da959aa",
  "ArtistName": "Dinh The Danh",
  "ReleaseDate": "2026-02-07T22:01:48",
  "CreatedAt": "2026-02-07T22:01:48",
  "UpdatedAt": null
}
```

| Field       | Type     | Nullable | Mô tả          |
| ----------- | -------- | -------- | -------------- |
| AlbumId     | Guid     | ❌       | ID album       |
| Title       | string   | ❌       | Tên album      |
| Thumbnail   | string   | ✅       | Ảnh bìa        |
| ArtistId    | Guid     | ❌       | ID nghệ sĩ tạo |
| ArtistName  | string   | ❌       | Tên nghệ sĩ    |
| ReleaseDate | DateTime | ❌       | Ngày phát hành |
| CreatedAt   | DateTime | ❌       | Ngày tạo       |
| UpdatedAt   | DateTime | ✅       | Ngày cập nhật  |

### PlaylistDto

```json
{
  "PlaylistId": "8129dfc2-221a-4e09-9ca1-cff1f71e27cd",
  "Title": "Danh sách phát của tôi #1",
  "Thumbnail": "https://res.cloudinary.com/.../playlist.jpg",
  "Description": "Mô tả playlist",
  "CreatedBy": "Dinh The Danh",
  "CreatedAt": "2026-02-24T21:46:13",
  "UpdatedAt": "2026-03-01T16:43:20",
  "SongCount": 5
}
```

| Field       | Type     | Nullable | Mô tả                     |
| ----------- | -------- | -------- | ------------------------- |
| PlaylistId  | Guid     | ❌       | ID playlist               |
| Title       | string   | ❌       | Tên playlist              |
| Thumbnail   | string   | ✅       | Ảnh bìa                   |
| Description | string   | ✅       | Mô tả                     |
| CreatedBy   | string   | ❌       | Tên người tạo             |
| CreatedAt   | DateTime | ❌       | Ngày tạo                  |
| UpdatedAt   | DateTime | ✅       | Ngày cập nhật             |
| SongCount   | int      | ❌       | Số bài hát trong playlist |

### PlaylistInfoDto (dùng trong Playlist Details)

```json
{
  "PlaylistId": "8129dfc2-221a-4e09-9ca1-cff1f71e27cd",
  "Title": "Danh sách phát của tôi #1",
  "Thumbnail": "https://res.cloudinary.com/.../playlist.jpg",
  "Description": "Mô tả playlist",
  "CreatedAt": "2026-02-24T21:46:13",
  "CreatedBy": "Dinh The Danh",
  "CreatedById": "514685e6-5141-40c6-84c6-37c43da959aa"
}
```

| Field       | Type     | Nullable | Mô tả         |
| ----------- | -------- | -------- | ------------- |
| PlaylistId  | Guid     | ❌       | ID playlist   |
| Title       | string   | ❌       | Tên playlist  |
| Thumbnail   | string   | ✅       | Ảnh bìa       |
| Description | string   | ✅       | Mô tả         |
| CreatedAt   | DateTime | ❌       | Ngày tạo      |
| CreatedBy   | string   | ✅       | Tên người tạo |
| CreatedById | Guid     | ✅       | ID người tạo  |

### GenreDto

```json
{
  "Id": "b1c2d3e4-5678-90ab-cdef-1234567890ab",
  "Name": "Pop",
  "ImageUrl": "https://res.cloudinary.com/.../pop.jpg"
}
```

| Field    | Type   | Nullable | Mô tả        |
| -------- | ------ | -------- | ------------ |
| Id       | Guid   | ❌       | ID thể loại  |
| Name     | string | ✅       | Tên thể loại |
| ImageUrl | string | ✅       | Ảnh thể loại |

### ArtistDto

```json
{
  "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
  "FullName": "Dinh The Danh",
  "Avatar": "https://ui-avatars.com/api/?name=Dinh+The+Danh&...",
  "Bio": "Ca sĩ, nhạc sĩ",
  "ArtistType": "Singer"
}
```

| Field      | Type   | Nullable | Mô tả                       |
| ---------- | ------ | -------- | --------------------------- |
| UserId     | Guid   | ❌       | ID nghệ sĩ                  |
| FullName   | string | ❌       | Tên nghệ sĩ                 |
| Avatar     | string | ❌       | Ảnh đại diện                |
| Bio        | string | ❌       | Mô tả                       |
| ArtistType | string | ✅       | Loại nghệ sĩ (Singer, etc.) |

### UserProfileDto

```json
{
  "UserId": "514685e6-5141-40c6-84c6-37c43da959aa",
  "Username": "dinhthedanh",
  "Email": "danh@example.com",
  "FullName": "Dinh The Danh",
  "Avatar": "https://...",
  "Bio": "Mô tả về bản thân",
  "Role": "Artist"
}
```

| Field    | Type   | Nullable | Mô tả         |
| -------- | ------ | -------- | ------------- |
| UserId   | Guid   | ❌       | ID người dùng |
| Username | string | ✅       | Tên đăng nhập |
| Email    | string | ✅       | Email         |
| FullName | string | ✅       | Họ tên        |
| Avatar   | string | ✅       | Ảnh đại diện  |
| Bio      | string | ✅       | Mô tả         |
| Role     | string | ✅       | Vai trò       |

### AuthResponseDto

```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "abc123def456...",
  "FullName": "Nguyễn Văn A",
  "Avatar": "https://...",
  "Role": "Artist",
  "IsNewUser": false
}
```

| Field        | Type   | Nullable | Mô tả                             |
| ------------ | ------ | -------- | --------------------------------- |
| Token        | string | ❌       | JWT Access Token                  |
| RefreshToken | string | ✅       | Refresh Token                     |
| FullName     | string | ❌       | Họ tên                            |
| Avatar       | string | ❌       | Ảnh đại diện                      |
| Role         | string | ❌       | Vai trò (User / Artist / Admin)   |
| IsNewUser    | bool   | ❌       | true nếu lần đầu đăng nhập Google |

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

| Field        | Type | Mô tả                             |
| ------------ | ---- | --------------------------------- |
| Data         | T[]  | Mảng dữ liệu                      |
| TotalRecords | int  | Tổng số bản ghi                   |
| TotalPages   | int  | Tổng số trang                     |
| FromRecord   | int  | Bản ghi bắt đầu (vd: 1, 11, 21)   |
| ToRecord     | int  | Bản ghi kết thúc (vd: 10, 20, 30) |

---

## 🔐 Error Responses

### 400 Bad Request

```json
{
  "Message": "Thông báo lỗi cụ thể"
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

### 404 Not Found

```json
{
  "Message": "Không tìm thấy tài nguyên"
}
```

---

## 📌 Summary Table

| #   | Endpoint                                       | Method | Auth | Desc                           |
| --- | ---------------------------------------------- | ------ | ---- | ------------------------------ |
| -   | **AUTH**                                       |        |      |                                |
| 1   | `/Auth/login`                                  | POST   | ❌   | Đăng nhập                      |
| 2   | `/Auth/google-login`                           | POST   | ❌   | Đăng nhập Google               |
| 3   | `/Auth/register`                               | POST   | ❌   | Đăng ký                        |
| 4   | `/Auth/refresh-token`                          | POST   | ❌   | Làm mới token                  |
| 5   | `/Auth/logout`                                 | POST   | ✅   | Đăng xuất                      |
| 6   | `/Auth/set-role`                               | POST   | ✅   | Cập nhật vai trò               |
| 7   | `/Auth/change-password`                        | PUT    | ✅   | Đổi mật khẩu                   |
| 8   | `/Auth/forgot-password`                        | POST   | ❌   | Quên mật khẩu                  |
| 9   | `/Auth/reset-password`                         | POST   | ❌   | Đặt lại mật khẩu               |
| -   | **USER**                                       |        |      |                                |
| 10  | `/User/profile`                                | GET    | ✅   | Xem hồ sơ                      |
| 11  | `/User/profile`                                | PUT    | ✅   | Cập nhật hồ sơ                 |
| 12  | `/User/update-interests`                       | POST   | ✅   | Cập nhật sở thích genre        |
| -   | **ARTIST**                                     |        |      |                                |
| 13  | `/Artist`                                      | GET    | ❌   | Tìm kiếm nghệ sĩ               |
| 14  | `/Artist/{id}/songs`                           | GET    | ❌   | Bài hát của nghệ sĩ            |
| -   | **FILE**                                       |        |      |                                |
| 15  | `/File/upload-image`                           | POST   | ❌   | Upload ảnh lên Cloudinary      |
| 16  | `/File/upload-audio`                           | POST   | ❌   | Upload nhạc lên Cloudinary     |
| 17  | `/File/signature`                              | GET    | ❌   | Lấy signature upload trực tiếp |
| -   | **MUSIC (SONGS)**                              |        |      |                                |
| 18  | `/Music/songs`                                 | GET    | ❌   | Tất cả bài hát (search)        |
| 19  | `/Music/my-songs`                              | GET    | ✅   | Bài hát của mình               |
| 20  | `/Music/song`                                  | POST   | ✅   | Tạo bài hát                    |
| 21  | `/Music/song/{id}`                             | PUT    | ✅   | Cập nhật bài hát               |
| 22  | `/Music/song/{id}`                             | DELETE | ✅   | Xóa bài hát                    |
| 23  | `/Music/check-hash/{hash}`                     | GET    | ❌   | Kiểm tra file trùng            |
| -   | **MUSIC (ALBUMS)**                             |        |      |                                |
| 24  | `/Music/albums`                                | GET    | ❌   | Tất cả album (search)          |
| 25  | `/Music/my-albums`                             | GET    | ✅   | Album của mình                 |
| 26  | `/Music/album`                                 | POST   | ✅   | Tạo album                      |
| 27  | `/Music/album/{id}`                            | PUT    | ✅   | Cập nhật album                 |
| 28  | `/Music/album/{id}`                            | DELETE | ✅   | Xóa album                      |
| 29  | `/Music/album/{id}`                            | GET    | ❌   | Chi tiết album + bài hát       |
| 30  | `/Music/album/{id}/add-song/{sid}`             | POST   | ✅   | Thêm bài hát vào album         |
| 31  | `/Interaction/album/{id}/remove-song/{sid}`    | DELETE | ✅   | Xóa bài hát khỏi album         |
| -   | **MUSIC (PLAYLISTS)**                          |        |      |                                |
| 32  | `/Music/playlists`                             | GET    | ❌   | Tất cả playlist (search)       |
| 33  | `/Music/my-playlists`                          | GET    | ✅   | Playlist của mình              |
| 34  | `/Interaction/playlist`                        | POST   | ✅   | Tạo playlist                   |
| 35  | `/Interaction/playlist/{id}`                   | GET    | ✅   | Chi tiết playlist              |
| 36  | `/Interaction/playlist/{id}`                   | PUT    | ✅   | Cập nhật playlist              |
| 37  | `/Interaction/playlist/{id}`                   | DELETE | ✅   | Xóa playlist                   |
| 38  | `/Interaction/playlist/{id}/add-song/{sid}`    | POST   | ✅   | Thêm bài hát vào playlist      |
| 39  | `/Interaction/playlist/{id}/remove-song/{sid}` | DELETE | ✅   | Xóa bài hát khỏi playlist      |
| -   | **LIKE / FOLLOW**                              |        |      |                                |
| 40  | `/Interaction/like/{songId}`                   | POST   | ✅   | Thích/bỏ thích bài hát         |
| 41  | `/Interaction/liked-songs`                     | GET    | ✅   | Danh sách bài hát yêu thích    |
| 42  | `/Interaction/follow/{userId}`                 | POST   | ✅   | Theo dõi/bỏ theo dõi user      |
| 43  | `/Interaction/followings`                      | GET    | ✅   | Danh sách user đang theo dõi   |
| -   | **GENRE**                                      |        |      |                                |
| 44  | `/Music/genres`                                | GET    | ❌   | Tất cả thể loại                |
| 45  | `/Music/genre`                                 | POST   | ✅   | Tạo thể loại                   |
| -   | **RECOMMENDATION**                             |        |      |                                |
| 46  | `/Recommendation/songs/{userId}`               | GET    | ✅   | Đề xuất bài hát                |
| 47  | `/Recommendation/albums/{userId}`              | GET    | ✅   | Đề xuất album                  |

---

**Total: 9 Auth + 3 User + 2 Artist + 3 File + 30 Feature = 47 Endpoints** ✅
