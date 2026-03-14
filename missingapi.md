# 🚨 Missing API Endpoints — Cần Backend Bổ Sung

## 1. Lọc bài hát theo thể loại (Genre)

### Yêu cầu

Frontend cần lọc bài hát theo thể loại khi người dùng bấm vào genre card ở trang Search.

### Endpoint cần thêm param

```
GET /Music/songs?genreId={genreId}&keyword=&pageIndex=1&pageSize=10
```

**Params mới:**

| Param     | Type            | Required | Mô tả                                                                              |
| --------- | --------------- | -------- | ---------------------------------------------------------------------------------- |
| `genreId` | `Guid` (string) | Optional | Lọc bài hát thuộc thể loại này. Nếu không truyền thì trả về tất cả bài hát như cũ. |

**Ví dụ request:**

```
GET /Music/songs?genreId=18b30c78-eb1b-11f0-aba2-fa3f7a3ec2f0&pageIndex=1&pageSize=50
```

**Response:** Giữ nguyên format `PagingResult<SongDto>` như hiện tại:

```json
{
  "Data": [
    {
      "Id": "...",
      "Title": "...",
      "Thumbnail": "...",
      "FileUrl": "...",
      "Duration": 261,
      "AlbumId": "...",
      "AlbumTitle": "...",
      "ArtistNames": "...",
      "ArtistIds": ["..."],
      "CreatedAt": "...",
      "UpdatedAt": "..."
    }
  ],
  "TotalRecords": 50,
  "TotalPages": 5,
  "FromRecord": 1,
  "ToRecord": 10
}
```

**Logic Backend:**

- JOIN bảng `SongGenres` (hoặc tương đương) để lọc bài hát có `GenreId` = param
- Kết hợp với `keyword` nếu cả hai đều có (AND condition)
- Giữ nguyên phân trang (pageIndex, pageSize)

---

## 2. Admin Dashboard Stats

### Yêu cầu

Frontend gọi `GET /Admin/dashboard` để lấy thống kê tổng quan.

### Endpoint

```
GET /Admin/dashboard
Authorization: Bearer {admin_token}
```

**Response mong đợi (200 OK):**

```json
{
  "TotalUsers": 150,
  "TotalArtists": 25,
  "TotalSongs": 500,
  "TotalSubscriptions": 45
}
```

> **Lưu ý:** Nếu endpoint này chưa được implement, frontend có fallback lấy count từ các endpoint riêng lẻ nhưng sẽ chậm hơn. Nên ưu tiên implement endpoint này.

---

## 3. Admin Users List

### Endpoint

```
GET /Admin/users?pageIndex=1&pageSize=15
Authorization: Bearer {admin_token}
```

**Response mong đợi (200 OK):** `PagingResult<UserDto>`

```json
{
  "Data": [
    {
      "UserId": "...",
      "FullName": "...",
      "UserName": "...",
      "Email": "...",
      "AvatarUrl": "...",
      "Role": "User|Artist|ArtistPremium|Admin",
      "IsBanned": false,
      "CreatedAt": "2026-01-01T00:00:00"
    }
  ],
  "TotalRecords": 150,
  "TotalPages": 10,
  "FromRecord": 1,
  "ToRecord": 15
}
```

---

## 4. Admin Set Role cho User khác

### Yêu cầu

Hiện tại `POST /Auth/set-role` chỉ cho phép user tự đổi role của mình (User ↔ Artist). Admin cần endpoint riêng để set role cho bất kỳ user nào.

### Endpoint

```
POST /Admin/users/{userId}/set-role
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**

```json
{
  "Role": "User" // "User" | "Artist" | "ArtistPremium" | "Admin"
}
```

**Response (200 OK):**

```json
{
  "Message": "Role updated successfully",
  "UserId": "...",
  "NewRole": "ArtistPremium"
}
```

**Logic Backend:**

- Chỉ Admin mới có quyền gọi endpoint này
- Validate `Role` phải nằm trong danh sách hợp lệ: `User`, `Artist`, `ArtistPremium`, `Admin`
- Cập nhật Role trong bảng `Users` (hoặc `AspNetUserRoles`)
- Nếu user đang online, invalidate hoặc update claims

---

## 5. Admin Cập Nhật Trạng Thái Thanh Toán

### Yêu cầu

Admin cần đổi trạng thái (status) của payment bất kỳ, không chỉ approve/reject pending.

### Endpoint

```
PUT /Admin/payments/{paymentId}/status
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**

```json
{
  "Status": "Success" // "Pending" | "Success" | "Failed" | "Rejected" | "Cancelled"
}
```

**Response (200 OK):**

```json
{
  "Message": "Payment status updated",
  "PaymentId": "...",
  "NewStatus": "Success",
  "RoleUpdated": true
}
```

**Logic Backend:**

- Nếu status chuyển sang `Success` → tự động cập nhật Role user dựa trên plan đã mua (ví dụ: Premium → `ArtistPremium`)
- Nếu status chuyển sang `Cancelled` / `Rejected` → có thể revert role nếu cần
- Ghi log thay đổi status

---

## 6. Tự Động Hủy Thanh Toán Pending Quá Hạn

### Yêu cầu

Nếu 1 payment đang `Pending` quá 15 ngày → tự động chuyển sang `Cancelled` và thông báo user.

### Option A: Background Job (Khuyến nghị)

Backend tạo scheduled job (Hangfire / Quartz / cron) chạy mỗi ngày:

```csharp
// Pseudocode
var expiredPayments = db.Payments
    .Where(p => p.Status == "Pending" && p.CreatedAt < DateTime.UtcNow.AddDays(-15))
    .ToList();

foreach (var payment in expiredPayments)
{
    payment.Status = "Cancelled";
    // Send notification to user
    notificationService.Send(payment.UserId, new Notification {
        Title = "Payment Cancelled",
        Message = $"Your pending payment for {payment.PlanName} has been cancelled (exceeded 15 days).",
        Type = "payment_expired"
    });
}
db.SaveChanges();
```

### Option B: Manual Admin Trigger Endpoint

```
POST /Admin/payments/cancel-expired?daysThreshold=15
Authorization: Bearer {admin_token}
```

**Response (200 OK):**

```json
{
  "CancelledCount": 3,
  "CancelledPaymentIds": ["...", "...", "..."]
}
```

---

## 7. Hệ Thống Thông Báo (Notification System) — MỚI HOÀN TOÀN

### 7.1 Database Schema

Tạo bảng `Notifications`:

```sql
CREATE TABLE Notifications (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId          UNIQUEIDENTIFIER NOT NULL,       -- FK → Users.UserId
    Title           NVARCHAR(255)    NOT NULL,
    Message         NVARCHAR(1000)   NOT NULL,
    Type            VARCHAR(50)      NOT NULL,        -- 'payment_reminder' | 'payment_approved' | 'payment_rejected' | 'payment_expired' | 'role_changed' | 'system' | 'general'
    IsRead          BIT              NOT NULL DEFAULT 0,
    RelatedEntityId UNIQUEIDENTIFIER NULL,            -- Optional: PaymentId, SubscriptionId, etc.
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_Notifications_UserId_CreatedAt ON Notifications (UserId, CreatedAt DESC);
CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications (UserId, IsRead);
```

### 7.2 API Endpoints

#### a) Lấy danh sách thông báo của user hiện tại

```
GET /Notification?pageIndex=1&pageSize=20
Authorization: Bearer {user_token}
```

**Response (200 OK):**

```json
{
  "Data": [
    {
      "Id": "abc123",
      "Title": "Payment Approved",
      "Message": "Your payment for Premium 1 Month has been approved!",
      "Type": "payment_approved",
      "IsRead": false,
      "RelatedEntityId": "payment-id-xxx",
      "CreatedAt": "2026-03-09T10:30:00Z"
    }
  ],
  "TotalRecords": 15,
  "TotalPages": 1,
  "FromRecord": 1,
  "ToRecord": 15
}
```

#### b) Đếm thông báo chưa đọc

```
GET /Notification/unread-count
Authorization: Bearer {user_token}
```

**Response (200 OK):**

```json
{
  "UnreadCount": 5
}
```

#### c) Đánh dấu 1 thông báo đã đọc

```
POST /Notification/{notificationId}/read
Authorization: Bearer {user_token}
```

**Response (200 OK):**

```json
{
  "Message": "Notification marked as read"
}
```

#### d) Đánh dấu tất cả đã đọc

```
POST /Notification/read-all
Authorization: Bearer {user_token}
```

**Response (200 OK):**

```json
{
  "Message": "All notifications marked as read",
  "UpdatedCount": 5
}
```

#### e) Admin gửi thông báo cho user

```
POST /Admin/notifications/send
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**

```json
{
  "UserId": "target-user-id",
  "Title": "Payment Reminder",
  "Message": "You have a pending payment for Premium. Please complete your payment.",
  "Type": "payment_reminder"
}
```

**Response (201 Created):**

```json
{
  "Id": "new-notification-id",
  "Message": "Notification sent successfully"
}
```

### 7.3 Real-time (SignalR) — Khuyến nghị

Để thông báo hiện real-time mà không cần polling:

#### SignalR Hub

```csharp
public class NotificationHub : Hub
{
    // Client kết nối và join group theo UserId
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }
}
```

#### Gửi từ Backend

```csharp
await _hubContext.Clients.Group($"user_{userId}")
    .SendAsync("ReceiveNotification", notification);
```

#### Frontend sẽ kết nối

```javascript
// Sử dụng @microsoft/signalr
const connection = new HubConnectionBuilder()
  .withUrl("http://localhost:5111/hubs/notification", {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

connection.on("ReceiveNotification", (notification) => {
  // Thêm vào list + tăng badge count
});
```

> **Lưu ý:** Frontend hiện đang poll mỗi 30s (`setInterval(fetchUnreadCount, 30000)`). Khi SignalR được implement, sẽ chuyển sang real-time hoàn toàn.

### 7.4 Tích hợp gửi thông báo tự động

Backend cần tự động gửi notification trong các trường hợp:

| Sự kiện                          | Type               | Title             | Message                                                                  |
| -------------------------------- | ------------------ | ----------------- | ------------------------------------------------------------------------ |
| Admin approve payment            | `payment_approved` | Payment Approved  | Your payment for {PlanName} has been approved!                           |
| Admin reject payment             | `payment_rejected` | Payment Rejected  | Your payment for {PlanName} was rejected                                 |
| Payment pending > 7 ngày         | `payment_reminder` | Payment Reminder  | You have a pending payment for {PlanName}. Please complete your payment. |
| Payment auto-cancelled (15 ngày) | `payment_expired`  | Payment Cancelled | Your pending payment for {PlanName} has expired and was cancelled        |
| Admin thay đổi role user         | `role_changed`     | Role Updated      | Your role has been updated to {NewRole}                                  |
