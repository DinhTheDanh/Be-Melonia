# Admin API Documentation

> Tài liệu API cho backend team - Các endpoint dành cho Admin Dashboard.
> Base URL: `http://localhost:5111/api/v1`

---

## 1. Dashboard Stats

### `GET /Admin/dashboard`

Lấy thống kê tổng quan cho dashboard admin.

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Response 200:**

```json
{
  "TotalUsers": 1250,
  "TotalArtists": 85,
  "TotalSongs": 3420,
  "TotalSubscriptions": 340,
  "TotalRevenue": 156000000,
  "NewUsersToday": 12,
  "NewSongsToday": 5
}
```

---

## 2. User Management

### `GET /Admin/users`

Lấy danh sách tất cả users (có phân trang, tìm kiếm).

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Query Parameters:**
| Param | Type | Default | Description |
|------------|--------|---------|--------------------------|
| keyword | string | "" | Tìm theo tên, email |
| pageIndex | int | 1 | Trang hiện tại |
| pageSize | int | 15 | Số item mỗi trang |
| role | string | "" | Lọc theo role (User, Artist, ArtistPremium, Admin) |

**Response 200:**

```json
{
  "Data": [
    {
      "UserId": "guid-string",
      "FullName": "Nguyễn Văn A",
      "UserName": "nguyenvana",
      "Email": "user@example.com",
      "AvatarUrl": "https://...",
      "Role": "User",
      "IsBanned": false,
      "CreatedAt": "2024-01-15T10:30:00Z"
    }
  ],
  "TotalCount": 1250,
  "PageIndex": 1,
  "PageSize": 15
}
```

### `POST /Admin/users/{userId}/toggle-ban`

Ban hoặc Unban một user.

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Response 200:**

```json
{
  "Success": true,
  "Message": "User has been banned",
  "IsBanned": true
}
```

---

## 3. Artist Management

### `GET /Admin/artists`

Lấy danh sách artists (có phân trang, tìm kiếm).

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Query Parameters:**
| Param | Type | Default | Description |
|------------|--------|---------|--------------------------|
| keyword | string | "" | Tìm theo tên artist |
| pageIndex | int | 1 | Trang hiện tại |
| pageSize | int | 15 | Số item mỗi trang |

**Response 200:**

```json
{
  "Data": [
    {
      "UserId": "guid-string",
      "ArtistId": "guid-string",
      "FullName": "Artist Name",
      "ArtistName": "Stage Name",
      "Email": "artist@example.com",
      "AvatarUrl": "https://...",
      "SongCount": 25,
      "FollowerCount": 1200,
      "TotalListens": 50000,
      "CreatedAt": "2024-01-10T08:00:00Z"
    }
  ],
  "TotalCount": 85,
  "PageIndex": 1,
  "PageSize": 15
}
```

---

## 4. Song Management

### `GET /Admin/songs`

Lấy danh sách tất cả bài hát (có phân trang, tìm kiếm).

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Query Parameters:**
| Param | Type | Default | Description |
|------------|--------|---------|------------------------------|
| keyword | string | "" | Tìm theo tên bài hát, artist |
| pageIndex | int | 1 | Trang hiện tại |
| pageSize | int | 15 | Số item mỗi trang |

**Response 200:**

```json
{
  "Data": [
    {
      "Id": "guid-string",
      "SongId": "guid-string",
      "Title": "Song Name",
      "ArtistNames": "Artist 1, Artist 2",
      "Thumbnail": "https://...",
      "FileUrl": "https://...",
      "Duration": 245,
      "ListenCount": 5000,
      "LikeCount": 320,
      "GenreName": "Pop",
      "CreatedAt": "2024-02-01T12:00:00Z"
    }
  ],
  "TotalCount": 3420,
  "PageIndex": 1,
  "PageSize": 15
}
```

### `DELETE /Admin/songs/{songId}`

Xóa bài hát (admin force delete).

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Response 200:**

```json
{
  "Success": true,
  "Message": "Song deleted successfully"
}
```

---

## 5. Subscription Management

### `GET /Admin/subscriptions`

Lấy danh sách tất cả subscriptions (có phân trang).

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Query Parameters:**
| Param | Type | Default | Description |
|------------|--------|---------|--------------------------------------|
| keyword | string | "" | Tìm theo tên user |
| pageIndex | int | 1 | Trang hiện tại |
| pageSize | int | 15 | Số item mỗi trang |
| status | string | "" | Lọc theo status (Active, Expired, Cancelled) |

**Response 200:**

```json
{
  "Data": [
    {
      "SubscriptionId": "guid-string",
      "UserId": "guid-string",
      "UserName": "Nguyễn Văn A",
      "FullName": "Nguyễn Văn A",
      "PlanId": "guid-string",
      "PlanName": "1 Year Premium",
      "Status": "Active",
      "StartDate": "2024-06-01T00:00:00Z",
      "EndDate": "2025-06-01T00:00:00Z",
      "CreatedAt": "2024-06-01T00:00:00Z"
    }
  ],
  "TotalCount": 340,
  "PageIndex": 1,
  "PageSize": 15
}
```

---

## 6. Payment Management

### `GET /Admin/payments`

Lấy danh sách tất cả payments (có phân trang, lọc).

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Query Parameters:**
| Param | Type | Default | Description |
|------------|--------|---------|------------------------------------------|
| keyword | string | "" | Tìm theo tên user |
| pageIndex | int | 1 | Trang hiện tại |
| pageSize | int | 15 | Số item mỗi trang |
| status | string | "" | Lọc theo status (Pending, Success, Failed, Rejected) |

**Response 200:**

```json
{
  "Data": [
    {
      "PaymentId": "guid-string",
      "UserId": "guid-string",
      "UserName": "Nguyễn Văn A",
      "FullName": "Nguyễn Văn A",
      "Amount": 349000,
      "PlanName": "1 Year Premium",
      "PlanId": "guid-string",
      "Status": "Pending",
      "PaymentMethod": "VNPay",
      "TransactionId": "vnpay-txn-id",
      "CreatedAt": "2024-06-15T14:30:00Z"
    }
  ],
  "TotalCount": 150,
  "PageIndex": 1,
  "PageSize": 15
}
```

### `GET /Admin/payments/pending`

Lấy danh sách payments đang chờ duyệt.

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Query Parameters:**
| Param | Type | Default | Description |
|------------|--------|---------|----------------------|
| pageIndex | int | 1 | Trang hiện tại |
| pageSize | int | 15 | Số item mỗi trang |

**Response 200:** _(Cùng format với GET /Admin/payments, chỉ trả về status=Pending)_

### `POST /Admin/payments/{paymentId}/approve`

Duyệt payment và kích hoạt subscription.

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Response 200:**

```json
{
  "Success": true,
  "Message": "Payment approved and subscription activated"
}
```

### `POST /Admin/payments/{paymentId}/reject`

Từ chối payment.

**Headers:**

- `Authorization: Bearer <token>` (role: Admin)

**Response 200:**

```json
{
  "Success": true,
  "Message": "Payment rejected"
}
```

---

## Notes

- Tất cả endpoints yêu cầu role **Admin** trong JWT token.
- Claim role: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
- Response format tuân theo convention hiện tại: `{ Data: [...], TotalCount, PageIndex, PageSize }`
- Nếu user không phải Admin → trả về **403 Forbidden**.
- Fallback: Frontend đang dùng public API (`/Music/songs`, `/Artist`) cho tabs Artists và Songs nếu Admin API chưa sẵn sàng. Khi Admin API ready, frontend sẽ tự động dùng Admin API.
