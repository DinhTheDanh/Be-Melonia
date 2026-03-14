# 💳 Payment & Subscription System - API Documentation

## Tổng quan hệ thống

Hệ thống thanh toán và subscription cho phép người dùng mua các gói để trở thành Artist/ArtistPremium với các tính năng tương ứng. Thanh toán được thực hiện qua VNPay (QR Code / Internet Banking).

---

## 📋 Mục lục

1. [Kiến trúc hệ thống](#1-kiến-trúc-hệ-thống)
2. [Database Schema (SQL)](#2-database-schema-sql)
3. [Các gói Subscription](#3-các-gói-subscription)
4. [Feature-based Authorization](#4-feature-based-authorization)
5. [Luồng thanh toán (Payment Flow)](#5-luồng-thanh-toán-payment-flow)
6. [API Endpoints](#6-api-endpoints)
7. [Song Stats & Artist Stats](#7-song-stats--artist-stats)
8. [Hướng dẫn tích hợp Frontend](#8-hướng-dẫn-tích-hợp-frontend)
9. [Xử lý lỗi & Edge Cases](#9-xử-lý-lỗi--edge-cases)
10. [Cấu hình VNPay](#10-cấu-hình-vnpay)
11. [Bảo mật](#11-bảo-mật)
12. [Prompt xây dựng Frontend thanh toán](#12-prompt-xây-dựng-frontend-thanh-toán)

---

## 1. Kiến trúc hệ thống

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│   Frontend   │────▶│  API Server  │────▶│    VNPay      │
│  (React/Vue) │◀────│  (.NET 8)    │◀────│   Gateway     │
└─────────────┘     └──────────────┘     └──────────────┘
                           │
                    ┌──────┴──────┐
                    │   MySQL DB   │
                    └─────────────┘
```

### Flow tổng quát:

1. User chọn gói → Frontend gọi API tạo payment
2. API tạo Payment (Pending) → Gọi VNPay tạo QR URL
3. API trả QR URL → Frontend redirect user đến VNPay
4. User thanh toán → VNPay redirect user về Return URL
5. Frontend gọi API vnpay-return → API cập nhật Payment = Success → Tạo Subscription → Cập nhật Role

> ⚠️ **IPN (webhook) KHÔNG được sử dụng.** `vnp_IpnUrl` đã bị loại bỏ khỏi request vì gây lỗi chữ ký VNPay. Toàn bộ logic cấp quyền được xử lý trong Return URL.

---

## 2. Database Schema (SQL)

### Tạo bảng (chạy trước khi sử dụng)

```sql
-- Bảng Subscription Plans (Các gói)
CREATE TABLE subscription_plans (
    plan_id CHAR(36) PRIMARY KEY,
    plan_name VARCHAR(100) NOT NULL,
    duration_months INT NOT NULL,
    price BIGINT NOT NULL COMMENT 'Giá VND',
    role_granted VARCHAR(50) NOT NULL COMMENT 'Role được gán: Artist, ArtistPremium',
    upload_limit INT NOT NULL DEFAULT -1 COMMENT '-1 = không giới hạn',
    can_schedule_release TINYINT(1) NOT NULL DEFAULT 0,
    has_advanced_analytics TINYINT(1) NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Bảng Subscriptions (Đăng ký của user)
CREATE TABLE subscriptions (
    subscription_id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    plan_id CHAR(36) NOT NULL,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Active' COMMENT 'Active, Expired, Cancelled, Replaced',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_subscriptions_user_id (user_id),
    INDEX idx_subscriptions_status (status),
    INDEX idx_subscriptions_end_date (end_date),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (plan_id) REFERENCES subscription_plans(plan_id)
);

-- Bảng Payments (Lịch sử thanh toán)
CREATE TABLE payments (
    payment_id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    plan_id CHAR(36) NOT NULL,
    amount BIGINT NOT NULL COMMENT 'Số tiền VND',
    status VARCHAR(20) NOT NULL DEFAULT 'Pending' COMMENT 'Pending, Success, Failed',
    payment_method VARCHAR(50) NOT NULL DEFAULT 'VNPay',
    transaction_id VARCHAR(255) NULL COMMENT 'Mã giao dịch từ VNPay',
    order_id VARCHAR(100) NOT NULL UNIQUE COMMENT 'Mã đơn hàng gửi VNPay',
    payment_url TEXT NULL COMMENT 'URL thanh toán QR',
    response_code VARCHAR(10) NULL COMMENT 'Mã response từ VNPay',
    secure_hash VARCHAR(512) NULL,
    ip_address VARCHAR(50) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_payments_user_id (user_id),
    INDEX idx_payments_order_id (order_id),
    INDEX idx_payments_status (status),
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (plan_id) REFERENCES subscription_plans(plan_id)
);
```

### Seed data (3 gói mặc định)

```sql
INSERT INTO subscription_plans (plan_id, plan_name, duration_months, price, role_granted, upload_limit, can_schedule_release, has_advanced_analytics, is_active, created_at, updated_at) VALUES
(UUID(), '1 Tháng', 1, 49000, 'Artist', 10, 0, 0, 1, NOW(), NOW()),
(UUID(), '6 Tháng', 6, 199000, 'Artist', -1, 1, 1, 1, NOW(), NOW()),
(UUID(), '1 Năm', 12, 349000, 'ArtistPremium', -1, 1, 1, 1, NOW(), NOW());
```

---

## 3. Các gói Subscription

| Gói         | Giá (VND) | Role          | Upload Limit   | Lên lịch phát hành | Analytics nâng cao |
| ----------- | --------- | ------------- | -------------- | ------------------ | ------------------ |
| **1 Tháng** | 49,000    | Artist        | Tối đa 10 bài  | ❌                 | ❌                 |
| **6 Tháng** | 199,000   | Artist        | Không giới hạn | ✅                 | ✅                 |
| **1 Năm**   | 349,000   | ArtistPremium | Không giới hạn | ✅                 | ✅                 |

### Khi subscription hết hạn:

- Role tự động chuyển về `User`
- Tất cả quyền Artist/ArtistPremium bị thu hồi
- Bài hát đã upload vẫn giữ nguyên

---

## 4. Feature-based Authorization

Hệ thống **KHÔNG** hard-code logic theo Role. Thay vào đó, sử dụng **Feature-based Authorization** dựa trên Subscription Plan.

### 3 Feature chính:

| Feature                | Mô tả                        | Gói 1 Tháng | Gói 6 Tháng | Gói 1 Năm |
| ---------------------- | ---------------------------- | ----------- | ----------- | --------- |
| `UploadLimit`          | Số bài upload tối đa         | 10          | -1 (∞)      | -1 (∞)    |
| `CanScheduleRelease`   | Quyền lên lịch phát hành     | false       | true        | true      |
| `HasAdvancedAnalytics` | Quyền xem analytics nâng cao | false       | true        | true      |

### Cách Frontend kiểm tra quyền:

```javascript
// Gọi API lấy toàn bộ features 1 lần
const response = await fetch("/api/v1/subscription/features", {
  credentials: "include",
});
const features = await response.json();

// Kiểm tra quyền upload
if (!features.CanUpload) {
  showMessage("Bạn đã đạt giới hạn upload. Nâng cấp gói để upload thêm.");
}

// Kiểm tra quyền lên lịch phát hành
if (!features.CanScheduleRelease) {
  showMessage("Tính năng này chỉ dành cho gói 6 Tháng trở lên.");
}

// Kiểm tra analytics nâng cao
if (!features.HasAdvancedAnalytics) {
  showMessage("Nâng cấp gói để xem analytics nâng cao.");
}
```

### Response mẫu `GET /api/v1/subscription/features`:

```json
{
  "UploadLimit": 10,
  "CurrentUploadCount": 3,
  "CanUpload": true,
  "CanScheduleRelease": false,
  "HasAdvancedAnalytics": false,
  "HasActiveSubscription": true,
  "CurrentPlanName": "1 Tháng",
  "SubscriptionEndDate": "2026-04-08T00:00:00Z"
}
```

---

## 5. Luồng thanh toán (Payment Flow)

### 5.1 Luồng chính (Happy Path)

```
Frontend                    API Server                     VNPay
   │                            │                            │
   │  1. POST /payment/create   │                            │
   │  { planId, paymentMethod } │                            │
   │───────────────────────────▶│                            │
   │                            │  2. Validate plan          │
   │                            │  3. Create Payment(Pending)│
   │                            │  4. Generate VNPay URL     │
   │                            │  (KHÔNG gửi vnp_IpnUrl)   │
   │  5. Return { paymentUrl }  │                            │
   │◀───────────────────────────│                            │
   │                            │                            │
   │  6. Redirect to VNPay URL  │                            │
   │───────────────────────────────────────────────────────▶│
   │                            │                            │
   │  7. User thanh toán (OTP)  │                            │
   │                            │                            │
   │  8. VNPay redirect browser về Return URL                │
   │◀───────────────────────────────────────────────────────│
   │                            │                            │
   │  9. GET /payment/vnpay-return?vnp_ResponseCode=00&...   │
   │───────────────────────────▶│                            │
   │                            │  10. Update Payment=Success│
   │                            │  11. Create Subscription   │
   │                            │  12. Update User Role      │
   │  13. Return payment result │                            │
   │◀───────────────────────────│                            │
```

> **QUAN TRỌNG:** Bước 10-12 (cấp quyền) xảy ra trong `ProcessVnPayReturnAsync`, KHÔNG phải IPN.
> API kiểm tra `vnp_ResponseCode == "00"` → tạo subscription + cập nhật role.
> Nếu payment đã xử lý rồi (status != Pending) → trả kết quả cũ (idempotent).

### 5.2 Luồng thất bại

```
   │  ... bước 1-8 như trên     │                            │
   │                            │                            │
   │  9. GET /payment/vnpay-return?vnp_ResponseCode=24&...   │
   │───────────────────────────▶│                            │
   │                            │  10. Update Payment=Failed │
   │  11. Return payment result │                            │
   │◀───────────────────────────│                            │
```

---

## 6. API Endpoints

### 6.1 Payment APIs

#### `POST /api/v1/payment/create` — Tạo thanh toán mới

**Auth:** Required (JWT Cookie)

**Request Body:**

```json
{
  "PlanId": "uuid-of-plan",
  "PaymentMethod": "VNPay"
}
```

**Success Response (200):**

```json
{
  "PaymentId": "payment-uuid",
  "OrderId": "20260308120000_abc123def456",
  "Amount": 49000,
  "Status": "Pending",
  "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=4900000&vnp_Command=pay&...",
  "PaymentMethod": "VNPay",
  "CreatedAt": "2026-03-08T12:00:00Z"
}
```

**Frontend xử lý:**

```javascript
const createPayment = async (planId) => {
  const res = await fetch("/api/v1/payment/create", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({ PlanId: planId, PaymentMethod: "VNPay" }),
  });

  const data = await res.json();

  if (res.ok) {
    // Redirect user đến VNPay để thanh toán
    window.location.href = data.PaymentUrl;
  } else {
    showError(data.Message);
  }
};
```

---

#### `GET /api/v1/payment/vnpay-ipn` — VNPay Webhook (IPN)

**Auth:** None

> ⚠️ **KHÔNG SỬ DỤNG:** Endpoint này vẫn tồn tại nhưng **không được VNPay gọi** vì `vnp_IpnUrl` đã bị loại bỏ khỏi payment request (gây lỗi chữ ký). Toàn bộ logic cấp quyền đã chuyển sang `vnpay-return`.

---

#### `GET /api/v1/payment/vnpay-return` — VNPay Return URL ⭐

**Auth:** None (VNPay redirect user về)

> **⭐ QUAN TRỌNG:** Đây là endpoint chính xử lý thanh toán. Khi VNPay redirect user về, endpoint này sẽ:
>
> 1. Update Payment status (Success/Failed)
> 2. Tạo Subscription (nếu thành công)
> 3. Cập nhật User Role (Artist/ArtistPremium)

**Query Parameters:** Tất cả params từ VNPay (vnp_ResponseCode, vnp_TxnRef, vnp_TransactionNo, ...)

**Khi thanh toán thành công (vnp_ResponseCode = "00"):**

```json
{
  "PaymentId": "payment-uuid",
  "OrderId": "20260308120000abc123def456",
  "Amount": 49000,
  "Status": "Success",
  "PaymentMethod": "VNPay",
  "CreatedAt": "2026-03-08T12:00:00Z"
}
```

**Khi thanh toán thất bại (vnp_ResponseCode != "00"):**

```json
{
  "PaymentId": "payment-uuid",
  "OrderId": "20260308120000abc123def456",
  "Amount": 49000,
  "Status": "Failed",
  "PaymentMethod": "VNPay",
  "CreatedAt": "2026-03-08T12:00:00Z"
}
```

**Idempotent:** Nếu payment đã xử lý rồi (status != Pending), trả về kết quả hiện tại mà không xử lý lại.

**Frontend xử lý (trang /payment/result):**

```javascript
// Trang này được VNPay redirect về: /payment/result?vnp_Amount=...&vnp_ResponseCode=...
const PaymentResult = () => {
  const [result, setResult] = useState(null);

  useEffect(() => {
    const params = window.location.search;
    fetch(`/api/v1/payment/vnpay-return${params}`)
      .then((res) => res.json())
      .then((data) => setResult(data));
  }, []);

  if (!result) return <Loading />;

  return (
    <div>
      {result.Status === "Success" ? (
        <div>
          <h2>✅ Thanh toán thành công!</h2>
          <p>Gói của bạn đã được kích hoạt.</p>
        </div>
      ) : (
        <div>
          <h2>❌ Thanh toán thất bại</h2>
          <p>Vui lòng thử lại hoặc liên hệ hỗ trợ.</p>
        </div>
      )}
    </div>
  );
};
```

---

#### `GET /api/v1/payment/history` — Lịch sử thanh toán

**Auth:** Required

**Response (200):**

```json
[
  {
    "PaymentId": "payment-uuid",
    "OrderId": "20260308120000_abc123def456",
    "PlanName": "1 Tháng",
    "Amount": 49000,
    "Status": "Success",
    "PaymentMethod": "VNPay",
    "TransactionId": "14138828",
    "CreatedAt": "2026-03-08T12:00:00Z"
  }
]
```

---

#### `GET /api/v1/payment/{paymentId}` — Chi tiết giao dịch

**Auth:** Required (chỉ xem được payment của mình)

**Response (200):**

```json
{
  "PaymentId": "payment-uuid",
  "OrderId": "20260308120000_abc123def456",
  "Amount": 49000,
  "Status": "Success",
  "PaymentUrl": "",
  "PaymentMethod": "VNPay",
  "CreatedAt": "2026-03-08T12:00:00Z"
}
```

---

### 6.2 Subscription APIs

#### `GET /api/v1/subscription/plans` — Danh sách gói (trang pricing)

**Auth:** None (public)

**Response (200):**

```json
[
  {
    "PlanId": "plan-uuid-1",
    "PlanName": "1 Tháng",
    "DurationMonths": 1,
    "Price": 49000,
    "RoleGranted": "Artist",
    "UploadLimit": 10,
    "CanScheduleRelease": false,
    "HasAdvancedAnalytics": false
  },
  {
    "PlanId": "plan-uuid-2",
    "PlanName": "6 Tháng",
    "DurationMonths": 6,
    "Price": 199000,
    "RoleGranted": "Artist",
    "UploadLimit": -1,
    "CanScheduleRelease": true,
    "HasAdvancedAnalytics": true
  },
  {
    "PlanId": "plan-uuid-3",
    "PlanName": "1 Năm",
    "DurationMonths": 12,
    "Price": 349000,
    "RoleGranted": "ArtistPremium",
    "UploadLimit": -1,
    "CanScheduleRelease": true,
    "HasAdvancedAnalytics": true
  }
]
```

**Frontend:**

```javascript
const PricingPage = () => {
  const [plans, setPlans] = useState([]);

  useEffect(() => {
    fetch("/api/v1/subscription/plans")
      .then((res) => res.json())
      .then((data) => setPlans(data));
  }, []);

  const handleSelectPlan = (planId) => {
    createPayment(planId); // Gọi API tạo payment -> redirect VNPay
  };

  return (
    <div className="pricing-grid">
      {plans.map((plan) => (
        <PricingCard
          key={plan.PlanId}
          plan={plan}
          onSelect={() => handleSelectPlan(plan.PlanId)}
        />
      ))}
    </div>
  );
};
```

---

#### `GET /api/v1/subscription/active` — Subscription đang hoạt động

**Auth:** Required

**Response (200) — Có subscription:**

```json
{
  "SubscriptionId": "sub-uuid",
  "PlanId": "plan-uuid",
  "PlanName": "6 Tháng",
  "RoleGranted": "Artist",
  "StartDate": "2026-03-08T12:00:00Z",
  "EndDate": "2026-09-08T12:00:00Z",
  "Status": "Active",
  "DaysRemaining": 183,
  "UploadLimit": -1,
  "CanScheduleRelease": true,
  "HasAdvancedAnalytics": true
}
```

**Response (404) — Không có subscription:**

```json
{
  "Message": "Bạn chưa có gói subscription nào đang hoạt động"
}
```

---

#### `GET /api/v1/subscription/history` — Lịch sử subscription

**Auth:** Required

**Response (200):**

```json
[
  {
    "SubscriptionId": "sub-uuid-1",
    "PlanId": "plan-uuid",
    "PlanName": "1 Tháng",
    "RoleGranted": "Artist",
    "StartDate": "2026-01-08T12:00:00Z",
    "EndDate": "2026-02-08T12:00:00Z",
    "Status": "Expired",
    "DaysRemaining": 0,
    "UploadLimit": 10,
    "CanScheduleRelease": false,
    "HasAdvancedAnalytics": false
  },
  {
    "SubscriptionId": "sub-uuid-2",
    "PlanId": "plan-uuid",
    "PlanName": "6 Tháng",
    "RoleGranted": "Artist",
    "StartDate": "2026-03-08T12:00:00Z",
    "EndDate": "2026-09-08T12:00:00Z",
    "Status": "Active",
    "DaysRemaining": 183,
    "UploadLimit": -1,
    "CanScheduleRelease": true,
    "HasAdvancedAnalytics": true
  }
]
```

---

### 6.3 Feature Authorization APIs

#### `GET /api/v1/subscription/features` — Toàn bộ quyền user

**Auth:** Required

> 💡 **Gợi ý:** Frontend nên gọi API này 1 lần khi user login hoặc khi cần check quyền, rồi cache lại (store/context).

**Response (200):**

```json
{
  "UploadLimit": 10,
  "CurrentUploadCount": 3,
  "CanUpload": true,
  "CanScheduleRelease": false,
  "HasAdvancedAnalytics": false,
  "HasActiveSubscription": true,
  "CurrentPlanName": "1 Tháng",
  "SubscriptionEndDate": "2026-04-08T00:00:00Z"
}
```

**Khi user KHÔNG có subscription:**

```json
{
  "UploadLimit": 0,
  "CurrentUploadCount": 0,
  "CanUpload": false,
  "CanScheduleRelease": false,
  "HasAdvancedAnalytics": false,
  "HasActiveSubscription": false,
  "CurrentPlanName": null,
  "SubscriptionEndDate": null
}
```

---

#### `GET /api/v1/subscription/features/can-upload` — Check quyền upload

**Auth:** Required

**Response (200):**

```json
true // hoặc false
```

---

#### `GET /api/v1/subscription/features/can-schedule` — Check quyền lên lịch

**Auth:** Required

**Response (200):**

```json
true // hoặc false
```

---

#### `GET /api/v1/subscription/features/has-analytics` — Check analytics nâng cao

**Auth:** Required

**Response (200):**

```json
true // hoặc false
```

---

## 7. Song Stats & Artist Stats

Hệ thống đã tích hợp thống kê **lượt thích** và **lượt nghe** cho từng bài hát, cũng như thống kê tổng hợp cho từng nghệ sĩ. Dữ liệu được tính toán real-time từ các bảng `user_likes` và `user_song_stats` có sẵn trong database.

### 7.1 Bảng liên quan (đã có sẵn trong DB)

| Bảng              | Mô tả                                                                                                                               |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `user_likes`      | Lưu mỗi lần user like bài hát (`user_id`, `song_id`, `liked_at`)                                                                    |
| `user_song_stats` | Lưu thống kê nghe của từng user cho từng bài (`user_id`, `song_id`, `play_count`, `total_listen_time`, `last_played`, `skip_count`) |
| `user_follows`    | Lưu quan hệ follow giữa các user (`follower_id`, `following_id`, `followed_at`)                                                     |
| `song_artists`    | Bảng nối many-to-many giữa `songs` và `users` (artist)                                                                              |

> Không cần tạo bảng mới — các bảng trên đã tồn tại. Chỉ cần đảm bảo frontend ghi dữ liệu vào `user_song_stats` khi user nghe bài hát.

### 7.2 SongDto — Thống kê bài hát

Mỗi bài hát trả về từ API giờ đều kèm `LikeCount` và `ListenCount`:

```json
{
  "Id": "song-uuid",
  "Title": "Đừng Làm Trái Tim Anh Đau",
  "Thumbnail": "https://res.cloudinary.com/...",
  "FileUrl": "https://res.cloudinary.com/...",
  "Duration": 245,
  "AlbumId": "album-uuid",
  "AlbumTitle": "Album Tình Yêu",
  "ArtistNames": "Sơn Tùng M-TP, Hải Tú",
  "ArtistIds": [],
  "LikeCount": 1520,
  "ListenCount": 45230,
  "CreatedAt": "2026-03-01T10:00:00Z",
  "UpdatedAt": null
}
```

| Trường        | Kiểu  | Nguồn dữ liệu                                                   | Mô tả                                           |
| ------------- | ----- | --------------------------------------------------------------- | ----------------------------------------------- |
| `LikeCount`   | `int` | `SELECT COUNT(*) FROM user_likes WHERE song_id = ?`             | Tổng số lượt thích bài hát                      |
| `ListenCount` | `int` | `SELECT SUM(play_count) FROM user_song_stats WHERE song_id = ?` | Tổng lượt nghe (tổng play_count từ tất cả user) |

#### Các API trả về SongDto (đều đã có LikeCount, ListenCount):

| API                                             | Mô tả                       |
| ----------------------------------------------- | --------------------------- |
| `GET /api/v1/music/songs`                       | Danh sách tất cả bài hát    |
| `GET /api/v1/artist/{artistId}/songs`           | Bài hát của 1 artist        |
| `GET /api/v1/music/my-songs`                    | Bài hát của user đang login |
| `GET /api/v1/interaction/liked-songs`           | Bài hát đã thích            |
| `GET /api/v1/interaction/playlist/{playlistId}` | Bài hát trong playlist      |
| `GET /api/v1/recommendation/songs/{userId}`     | Bài hát gợi ý               |

#### Frontend sử dụng:

```jsx
const SongCard = ({ song }) => (
  <div className="song-card">
    <img src={song.Thumbnail} alt={song.Title} />
    <h4>{song.Title}</h4>
    <p>{song.ArtistNames}</p>
    <div className="stats">
      <span>❤️ {song.LikeCount.toLocaleString()}</span>
      <span>🎧 {song.ListenCount.toLocaleString()}</span>
    </div>
  </div>
);
```

### 7.3 ArtistDto — Thống kê nghệ sĩ

Mỗi artist trả về từ API giờ đều kèm `FollowerCount`, `SongCount`, `TotalLikes`, `TotalListens`:

```json
{
  "UserId": "artist-uuid",
  "FullName": "Sơn Tùng M-TP",
  "Avatar": "https://res.cloudinary.com/...",
  "Banner": "https://res.cloudinary.com/...",
  "Bio": "Ca sĩ, nhạc sĩ, nhà sản xuất âm nhạc Việt Nam",
  "ArtistType": "Singer",
  "FollowerCount": 5200,
  "SongCount": 48,
  "TotalLikes": 125000,
  "TotalListens": 8500000
}
```

| Trường          | Kiểu  | Nguồn dữ liệu                                                                              | Mô tả                                     |
| --------------- | ----- | ------------------------------------------------------------------------------------------ | ----------------------------------------- |
| `FollowerCount` | `int` | `SELECT COUNT(*) FROM user_follows WHERE following_id = ?`                                 | Số người theo dõi artist                  |
| `SongCount`     | `int` | `SELECT COUNT(DISTINCT song_id) FROM song_artists WHERE artist_id = ?`                     | Số bài hát artist đã phát hành            |
| `TotalLikes`    | `int` | `SELECT COUNT(*) FROM user_likes JOIN song_artists ON ... WHERE artist_id = ?`             | Tổng lượt thích tất cả bài hát của artist |
| `TotalListens`  | `int` | `SELECT SUM(play_count) FROM user_song_stats JOIN song_artists ON ... WHERE artist_id = ?` | Tổng lượt nghe tất cả bài hát của artist  |

#### Các API trả về ArtistDto (đều đã có stats):

| API                                  | Mô tả                         |
| ------------------------------------ | ----------------------------- |
| `GET /api/v1/artist`                 | Danh sách tất cả artists      |
| `GET /api/v1/interaction/followings` | Danh sách artists đang follow |

#### Frontend sử dụng:

```jsx
const ArtistCard = ({ artist }) => (
  <div className="artist-card">
    <img src={artist.Avatar} alt={artist.FullName} />
    <h3>{artist.FullName}</h3>
    <p>{artist.ArtistType}</p>
    <div className="stats-grid">
      <div className="stat">
        <span className="stat-value">{formatNumber(artist.FollowerCount)}</span>
        <span className="stat-label">Followers</span>
      </div>
      <div className="stat">
        <span className="stat-value">{artist.SongCount}</span>
        <span className="stat-label">Bài hát</span>
      </div>
      <div className="stat">
        <span className="stat-value">{formatNumber(artist.TotalLikes)}</span>
        <span className="stat-label">Lượt thích</span>
      </div>
      <div className="stat">
        <span className="stat-value">{formatNumber(artist.TotalListens)}</span>
        <span className="stat-label">Lượt nghe</span>
      </div>
    </div>
  </div>
);

// Helper format số lớn
const formatNumber = (num) => {
  if (num >= 1_000_000) return (num / 1_000_000).toFixed(1) + "M";
  if (num >= 1_000) return (num / 1_000).toFixed(1) + "K";
  return num.toString();
};
```

### 7.4 ArtistStatsDto — Query riêng stats cho 1 artist

Dùng nội bộ (backend) để batch query stats cho nhiều artists cùng lúc (1 query thay vì N queries):

```csharp
// Backend sử dụng batch query cho hiệu năng tối ưu
// Khi GET /api/v1/artist trả về 10 artists → chỉ 1 query lấy stats cho cả 10
Dictionary<Guid, ArtistStatsDto> GetArtistsStatsBatchAsync(IEnumerable<Guid> artistIds);
```

```json
// ArtistStatsDto response (chỉ dùng nội bộ)
{
  "ArtistId": "artist-uuid",
  "FollowerCount": 5200,
  "SongCount": 48,
  "TotalLikes": 125000,
  "TotalListens": 8500000
}
```

### 7.5 Cách dữ liệu được tính toán (SQL)

#### Song LikeCount & ListenCount:

```sql
-- Được tính bằng correlated subquery trong mỗi SongDto query
SELECT
    s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
    s.created_at as CreatedAt, s.updated_at as UpdatedAt,
    s.album_id as AlbumId, al.title as AlbumTitle,
    GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
    -- ⭐ Song Stats
    (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) as LikeCount,
    (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount
FROM songs s
LEFT JOIN song_artists sa ON s.song_id = sa.song_id
LEFT JOIN users u ON sa.artist_id = u.user_id
LEFT JOIN albums al ON s.album_id = al.album_id
WHERE s.is_public = 1
GROUP BY s.song_id, ...
```

#### Artist Stats (batch query cho N artists):

```sql
SELECT
    u.user_id as ArtistId,
    (SELECT COUNT(*) FROM user_follows uf WHERE uf.following_id = u.user_id) as FollowerCount,
    (SELECT COUNT(DISTINCT sa.song_id) FROM song_artists sa WHERE sa.artist_id = u.user_id) as SongCount,
    (SELECT COUNT(*) FROM user_likes ul
     JOIN song_artists sa ON ul.song_id = sa.song_id
     WHERE sa.artist_id = u.user_id) as TotalLikes,
    (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss
     JOIN song_artists sa ON uss.song_id = sa.song_id
     WHERE sa.artist_id = u.user_id) as TotalListens
FROM users u
WHERE u.user_id IN @ArtistIds
```

### 7.6 Lưu ý quan trọng

> ⚠️ **Frontend cần ghi dữ liệu `user_song_stats`:**  
> Hiện tại backend chỉ **đọc** từ bảng `user_song_stats` để tính `ListenCount`. Frontend/player cần gọi API để ghi `play_count` khi user nghe xong bài hát. Nếu chưa có API ghi play count, `ListenCount` sẽ luôn = 0.

> ⚠️ **Hiệu năng:**  
> Stats được tính real-time bằng subquery. Với lượng dữ liệu lớn (>100K bài hát), cân nhắc:
>
> - Thêm index trên `user_likes(song_id)` và `user_song_stats(song_id)`
> - Hoặc sử dụng materialized view / cache (Redis) cho Top Songs

### 7.7 API Record Play — Ghi nhận lượt nghe

> ⚠️ **Quan trọng:** Frontend **PHẢI** gọi API này khi user nghe nhạc, nếu không `ListenCount` sẽ luôn = 0.

**Endpoint:**

```
POST /api/v1/interaction/record-play
```

**Auth:** ✅ JWT Cookie

**Request Body:**

```json
{
  "SongId": "uuid-of-song",
  "DurationListened": 180,
  "Completed": true,
  "Source": "playlist"
}
```

| Trường             | Kiểu     | Bắt buộc | Mô tả                                                               |
| ------------------ | -------- | -------- | ------------------------------------------------------------------- |
| `SongId`           | `Guid`   | ✅       | ID bài hát                                                          |
| `DurationListened` | `int`    | ✅       | Số giây user đã nghe                                                |
| `Completed`        | `bool`   | ✅       | `true` nếu nghe hết bài, `false` nếu bị skip                        |
| `Source`           | `string` | ❌       | Nguồn phát: `"playlist"`, `"album"`, `"search"`, `"recommendation"` |

**Response (200 OK):**

```json
{
  "Message": "Đã ghi nhận lượt nghe"
}
```

**Cơ chế backend khi gọi API này:**

1. **Upsert `user_song_stats`**: tăng `play_count + 1`, cộng dồn `total_listen_time`, cập nhật `last_played`, nếu skip thì tăng `skip_count`
2. **Insert `listening_history`**: thêm 1 bản ghi chi tiết (để dùng cho recommendation system)

**Frontend nên gọi khi nào?**

- Khi bài hát kết thúc tự nhiên → `Completed: true`
- Khi user skip sang bài khác (đã nghe > 30 giây) → `Completed: false`
- **KHÔNG gọi** nếu user nghe dưới 10 giây (tránh spam)

**Ví dụ React integration:**

```javascript
const recordPlay = async (songId, duration, completed, source) => {
  // Chỉ ghi nhận nếu nghe > 10 giây
  if (duration < 10) return;

  try {
    await fetch("/api/v1/interaction/record-play", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify({
        SongId: songId,
        DurationListened: Math.round(duration),
        Completed: completed,
        Source: source,
      }),
    });
  } catch (error) {
    // Không block UI nếu ghi nhận thất bại
    console.warn("Record play failed:", error);
  }
};

// Trong audio player component:
audioRef.current.onended = () => {
  recordPlay(currentSong.Id, audioRef.current.duration, true, "playlist");
  playNext();
};

// Khi user skip:
const handleSkip = () => {
  const listened = audioRef.current.currentTime;
  recordPlay(currentSong.Id, listened, false, "playlist");
  playNext();
};
```

---

## 8. Hướng dẫn tích hợp Frontend

### Bước 1: Trang Pricing (Chọn gói)

```javascript
// services/subscriptionService.js
export const getPlans = () =>
  fetch("/api/v1/subscription/plans").then((res) => res.json());

export const createPayment = async (planId) => {
  const res = await fetch("/api/v1/payment/create", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({ PlanId: planId, PaymentMethod: "VNPay" }),
  });
  return res.json();
};
```

### Bước 2: Redirect đến VNPay

```javascript
const handleBuyPlan = async (planId) => {
  try {
    const data = await createPayment(planId);

    if (data.PaymentUrl) {
      // Redirect user đến trang thanh toán VNPay
      window.location.href = data.PaymentUrl;
    }
  } catch (error) {
    showToast("Lỗi tạo thanh toán. Vui lòng thử lại.");
  }
};
```

### Bước 3: Trang kết quả thanh toán

```javascript
// pages/PaymentResult.jsx
import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";

const PaymentResult = () => {
  const [searchParams] = useSearchParams();
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const queryString = window.location.search;

    fetch(`/api/v1/payment/vnpay-return${queryString}`)
      .then((res) => res.json())
      .then((data) => {
        setResult(data);
        setLoading(false);
      })
      .catch(() => {
        setResult({ Status: "Error" });
        setLoading(false);
      });
  }, []);

  if (loading) return <Spinner />;

  return (
    <div className="payment-result">
      {result.Status === "Success" ? (
        <>
          <SuccessIcon />
          <h2>Thanh toán thành công!</h2>
          <p>Số tiền: {result.Amount?.toLocaleString()}₫</p>
          <p>Mã giao dịch: {result.OrderId}</p>
          <Button onClick={() => navigate("/dashboard")}>Về trang chủ</Button>
        </>
      ) : (
        <>
          <ErrorIcon />
          <h2>Thanh toán thất bại</h2>
          <p>Vui lòng thử lại hoặc chọn phương thức thanh toán khác.</p>
          <Button onClick={() => navigate("/pricing")}>Thử lại</Button>
        </>
      )}
    </div>
  );
};
```

### Bước 4: Kiểm tra quyền (Feature Guard)

```javascript
// hooks/useFeatures.js
import { createContext, useContext, useEffect, useState } from "react";

const FeatureContext = createContext(null);

export const FeatureProvider = ({ children }) => {
  const [features, setFeatures] = useState(null);

  const refreshFeatures = async () => {
    try {
      const res = await fetch("/api/v1/subscription/features", {
        credentials: "include",
      });
      if (res.ok) {
        const data = await res.json();
        setFeatures(data);
      }
    } catch (error) {
      console.error("Failed to load features");
    }
  };

  useEffect(() => {
    refreshFeatures();
  }, []);

  return (
    <FeatureContext.Provider value={{ features, refreshFeatures }}>
      {children}
    </FeatureContext.Provider>
  );
};

export const useFeatures = () => useContext(FeatureContext);
```

### Bước 5: Sử dụng trong component

```jsx
// components/UploadSong.jsx
const UploadSong = () => {
  const { features } = useFeatures();

  if (!features?.HasActiveSubscription) {
    return <UpgradePrompt message="Mua gói để upload bài hát" />;
  }

  if (!features?.CanUpload) {
    return (
      <UpgradePrompt
        message={`Bạn đã upload ${features.CurrentUploadCount}/${features.UploadLimit} bài. Nâng cấp gói để upload thêm.`}
      />
    );
  }

  return <UploadForm />;
};

// components/ScheduleRelease.jsx
const ScheduleRelease = () => {
  const { features } = useFeatures();

  if (!features?.CanScheduleRelease) {
    return (
      <UpgradePrompt message="Nâng cấp gói 6 Tháng trở lên để lên lịch phát hành" />
    );
  }

  return <ScheduleForm />;
};

// components/AdvancedAnalytics.jsx
const AdvancedAnalytics = () => {
  const { features } = useFeatures();

  if (!features?.HasAdvancedAnalytics) {
    return <UpgradePrompt message="Nâng cấp gói để xem analytics nâng cao" />;
  }

  return <AnalyticsDashboard />;
};
```

### Bước 6: Hiển thị subscription hiện tại

```jsx
// components/SubscriptionStatus.jsx
const SubscriptionStatus = () => {
  const [subscription, setSubscription] = useState(null);

  useEffect(() => {
    fetch("/api/v1/subscription/active", { credentials: "include" })
      .then((res) => {
        if (res.ok) return res.json();
        return null;
      })
      .then((data) => setSubscription(data));
  }, []);

  if (!subscription) {
    return (
      <div className="no-subscription">
        <p>Bạn chưa có gói nào.</p>
        <Button href="/pricing">Mua gói ngay</Button>
      </div>
    );
  }

  return (
    <div className="subscription-card">
      <h3>Gói: {subscription.PlanName}</h3>
      <p>Role: {subscription.RoleGranted}</p>
      <p>
        Hết hạn: {new Date(subscription.EndDate).toLocaleDateString("vi-VN")}
      </p>
      <p>Còn lại: {subscription.DaysRemaining} ngày</p>
      <ProgressBar
        value={subscription.DaysRemaining}
        max={
          subscription.PlanName === "1 Năm"
            ? 365
            : subscription.PlanName === "6 Tháng"
              ? 180
              : 30
        }
      />
    </div>
  );
};
```

---

## 9. Xử lý lỗi & Edge Cases

### Các error response chung

| Status Code | Khi nào                 | Response                                |
| ----------- | ----------------------- | --------------------------------------- |
| 400         | Dữ liệu không hợp lệ    | `{ "Message": "Yêu cầu không hợp lệ" }` |
| 401         | Chưa đăng nhập          | `{ "Message": "Vui lòng đăng nhập" }`   |
| 403         | Không có quyền          | `{ "Message": "Không có quyền" }`       |
| 404         | Không tìm thấy resource | `{ "Message": "Không tìm thấy" }`       |

### Edge Cases quan trọng:

1. **User tạo payment nhưng không thanh toán:**
   - Payment sẽ giữ Status = Pending
   - Nếu user tạo lại payment cho cùng plan trong 15 phút → trả về URL cũ

2. **User mua gói mới khi đang có gói cũ:**
   - Gói cũ bị chuyển status = "Replaced"
   - Gói mới được tạo với StartDate = now

3. **Subscription hết hạn:**
   - Background job chạy mỗi 30 phút kiểm tra
   - Tự động chuyển role về "User"
   - Admin role KHÔNG bị ảnh hưởng

4. **User mở Return URL nhiều lần (duplicate):**
   - Hệ thống kiểm tra payment status
   - Nếu đã xử lý (status != Pending) → trả kết quả hiện tại (idempotent)
   - Subscription và role KHÔNG bị tạo/cập nhật lại

5. **VNPay IPN không hoạt động:**
   - Đã bỏ `vnp_IpnUrl` khỏi request vì gây lỗi chữ ký
   - Toàn bộ logic cấp quyền xử lý trong Return URL
   - Khi deploy production có thể bổ sung IPN

---

## 10. Cấu hình VNPay

### Thông tin Sandbox hiện tại (đã cấu hình)

| Trường         | Giá trị                                              |
| -------------- | ---------------------------------------------------- |
| `TmnCode`      | `80ANYI0X`                                           |
| `HashSecret`   | `D7ZACNMZXODL8VXW95MOU72E0OZBHLPW`                   |
| `BaseUrl`      | `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html` |
| Merchant Admin | https://sandbox.vnpayment.vn/merchantv2/             |
| Tên đăng nhập  | `dinhthedanh1@gmail.com`                             |

### appsettings.json (cấu hình hiện tại)

```json
"VnPay": {
  "TmnCode": "80ANYI0X",
  "HashSecret": "D7ZACNMZXODL8VXW95MOU72E0OZBHLPW",
  "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
  "ReturnUrl": "http://localhost:3000/payment/result",
  "IpnUrl": "",
  "Version": "2.1.0",
  "Command": "pay",
  "CurrCode": "VND",
  "Locale": "vn"
}
```

> **Lưu ý:** `IpnUrl` để trống vì `vnp_IpnUrl` đã bị loại bỏ khỏi payment request (gây lỗi chữ ký VNPay). Không cần ngrok.

---

### Hướng dẫn setup đầy đủ (từng bước)

> **Không cần ngrok!** Hệ thống không sử dụng IPN (webhook). Toàn bộ logic cấp quyền xử lý trong Return URL, chạy localhost bình thường.

#### Bước 1 — Chạy API

```bash
cd MUSIC.STREAMING.WEBSITE.API
dotnet run
```

#### Bước 2 — Thanh toán test với thẻ

Thực hiện thanh toán test qua VNPay sandbox:

| Thông tin      | Giá trị               |
| -------------- | --------------------- |
| Ngân hàng      | NCB                   |
| Số thẻ         | `9704198526191432198` |
| Tên chủ thẻ    | `NGUYEN VAN A`        |
| Ngày phát hành | `07/15`               |
| Mật khẩu OTP   | `123456`              |

---

### Flow đầy đủ khi test

```
1. Frontend gọi POST /api/v1/payment/create → nhận PaymentUrl
2. window.location.href = PaymentUrl → user thấy trang VNPay
3. Nhập thẻ NCB test → xác nhận OTP 123456
4. VNPay redirect browser → http://localhost:3000/payment/result?vnp_ResponseCode=00&...
5. Frontend gọi GET /api/v1/payment/vnpay-return?vnp_...
   → Backend cập nhật Payment=Success → tạo Subscription → cập nhật Role
   → Trả về { Status: "Success" }
6. Frontend hiển thị kết quả → refresh features/profile
```

> **Tại sao không dùng IPN?**
>
> - `vnp_IpnUrl` gây lỗi chữ ký (Sai chữ ký) khi gửi trong payment request
> - VNPay Sandbox không cho phép cấu hình IPN URL riêng trên dashboard
> - Return URL đủ tin cậy cho sandbox testing
> - Khi deploy production, có thể bổ sung IPN sau

---

### Tài liệu tham khảo VNPay

| Tài liệu             | Link                                                            |
| -------------------- | --------------------------------------------------------------- |
| Tài liệu tích hợp    | https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html  |
| Code demo            | https://sandbox.vnpayment.vn/apis/vnpay-demo/code-demo-tích-hợp |
| Merchant Admin       | https://sandbox.vnpayment.vn/merchantv2/                        |
| SIT Testing          | https://sandbox.vnpayment.vn/vnpaygw-sit-testing/user/login     |
| Demo cổng thanh toán | https://sandbox.vnpayment.vn/apis/vnpay-demo/                   |

---

### Khi deploy production

- Đổi `BaseUrl` → `https://pay.vnpay.vn/vpcpay.html`
- Thay `TmnCode` + `HashSecret` bằng credentials production (đăng ký tại VNPay)
- `ReturnUrl` phải là domain frontend production
- Cân nhắc bổ sung IPN (webhook) để đảm bảo xử lý thanh toán khi user đóng browser trước khi redirect

---

## 11. Bảo mật

### Các biện pháp bảo mật đã tích hợp:

1. **HMAC-SHA512 Signature:** Ký payment URL bằng HMAC-SHA512 trên URL-encoded query string
2. **Idempotent Return Processing:** Xử lý duplicate return request an toàn (nếu payment đã xử lý → trả kết quả cũ)
3. **IP Logging:** Ghi nhận IP người tạo giao dịch
4. **Payment Isolation:** User chỉ xem được payment của chính mình
5. **Secure Configuration:** VNPay secret key lấy từ config, không hard-code
6. **Payment URL Expiry:** URL thanh toán tự hết hạn sau 15 phút
7. **Auto Role Revert:** Background job mỗi 30 phút kiểm tra subscription hết hạn → revert role về User

### Khuyến nghị thêm:

- Sử dụng HTTPS cho tất cả endpoint
- Giới hạn rate limit cho endpoint tạo payment
- Monitor và alert khi có nhiều payment failed bất thường
- Backup database định kỳ
- Hash sensitive data trong log (không log full payment URL)

---

## 12. Prompt xây dựng Frontend thanh toán

> **Dùng prompt dưới đây để giao cho frontend developer (hoặc AI) xây dựng các trang liên quan đến thanh toán & subscription.**

---

### Prompt cho Frontend Developer

```
Xây dựng hệ thống thanh toán & subscription cho website nghe nhạc (React + TypeScript).
Backend API đã sẵn sàng tại: http://localhost:5111/api/v1
Auth: JWT lưu trong HttpOnly cookie → mọi request dùng credentials: "include".

=== CÁC TRANG CẦN TẠO ===

1. TRANG PRICING (/pricing)
   - Gọi GET /subscription/plans để lấy danh sách gói
   - Hiển thị 3 card gói:
     • 1 Tháng – 49,000₫ → Role: Artist
     • 6 Tháng – 199,000₫ → Role: ArtistPremium
     • 1 Năm – 349,000₫ → Role: ArtistPremium
   - Mỗi card hiển thị: tên gói, giá, thời hạn, role được cấp, danh sách tính năng
   - Tính năng Artist: Upload bài hát (tối đa 50)
   - Tính năng ArtistPremium: Upload không giới hạn, lên lịch phát hành, analytics nâng cao
   - Button "Mua ngay" → gọi POST /payment/create với body:
     { "PlanId": "<guid>", "PaymentMethod": "VNPay" }
   - Response trả về { PaymentUrl: "https://sandbox.vnpayment.vn/..." }
   - Redirect user: window.location.href = PaymentUrl
   - Nếu user đang có subscription active → hiển thị badge "Đang sử dụng" trên gói hiện tại
   - Gọi GET /subscription/active để check (nếu 404 = chưa có gói)

2. TRANG KẾT QUẢ THANH TOÁN (/payment/result)
   - VNPay sẽ redirect user về URL này kèm query params
   - Lấy toàn bộ query string từ URL
   - Gọi GET /payment/vnpay-return?<toàn bộ query params gốc từ VNPay>
   - ⚠️ QUAN TRỌNG: API này sẽ TỰ ĐỘNG:
     + Update payment status (Success/Failed)
     + Tạo Subscription (nếu thành công)
     + Cập nhật User Role (Artist/ArtistPremium)
   - Response sẽ trả về:
     {
       "Success": true/false,
       "OrderId": "...",
       "Amount": 49000,
       "Message": "Thanh toán thành công" hoặc lý do thất bại
     }
   - Nếu Success = true:
     → Hiển thị icon thành công, số tiền, mã giao dịch
     → Button "Về trang chủ" và "Xem subscription"
     → Gọi lại GET /subscription/features để refresh quyền user
   - Nếu Success = false:
     → Hiển thị icon thất bại, message lỗi
     → Button "Thử lại" → navigate(/pricing)

3. TRANG LỊCH SỬ THANH TOÁN (/payment/history)
   - Gọi GET /payment/history
   - Hiển thị bảng: Ngày, Gói, Số tiền, Trạng thái (Success/Failed/Pending), Mã GD
   - Click vào 1 dòng → gọi GET /payment/{paymentId} hiển thị chi tiết

4. COMPONENT SUBSCRIPTION STATUS (trong trang Profile)
   - Gọi GET /subscription/active
   - Nếu có: hiển thị tên gói, role, ngày hết hạn, số ngày còn lại, progress bar
   - Nếu không có: hiển thị "Chưa có gói" + button "Mua gói ngay"
   - Gọi GET /subscription/history để hiển thị lịch sử các gói đã mua

=== FEATURE GUARD (BẮT BUỘC) ===

Gọi GET /subscription/features 1 lần khi app load, lưu vào Context/Store.
Response mẫu:
{
  "CurrentRole": "ArtistPremium",
  "HasActiveSubscription": true,
  "CanUpload": true,
  "UploadLimit": -1,
  "CurrentUploadCount": 5,
  "CanScheduleRelease": true,
  "HasAdvancedAnalytics": true,
  "SubscriptionEndDate": "2026-09-08T...",
  "DaysRemaining": 183
}

Áp dụng guard:
- Trang Upload Song: if (!features.CanUpload) → hiện "Mua gói để upload"
- Nếu CanUpload nhưng CurrentUploadCount >= UploadLimit → hiện "Đã đạt giới hạn, nâng cấp gói"
- Trang Schedule Release: if (!features.CanScheduleRelease) → hiện "Nâng cấp gói 6 Tháng+"
- Trang Analytics: if (!features.HasAdvancedAnalytics) → hiện "Nâng cấp gói"
- Refresh features sau khi thanh toán thành công

=== TÍCH HỢP RECORD PLAY (AUDIO PLAYER) ===

Khi user nghe bài hát, gọi POST /interaction/record-play:
- Khi bài hát kết thúc tự nhiên:
  { "SongId": "<guid>", "DurationListened": <tổng giây>, "Completed": true, "Source": "playlist" }
- Khi user skip/chuyển bài:
  { "SongId": "<guid>", "DurationListened": <giây đã nghe>, "Completed": false, "Source": "search" }
- Source có thể là: "playlist", "search", "recommendation", "album", "artist_page"
- Gọi API này lúc: bài hát ended, user click next/prev, user chọn bài khác

=== ROUTING ===

/pricing              → Trang chọn gói
/payment/result       → Kết quả thanh toán (VNPay redirect về đây)
/payment/history      → Lịch sử thanh toán
/profile/subscription → Quản lý subscription trong profile

=== LƯU Ý QUAN TRỌNG ===

- Mọi API đều dùng credentials: "include" (cookie auth)
- Base URL API: http://localhost:5111/api/v1
- Tiền tệ: VND, format số dùng toLocaleString("vi-VN")
- Nếu response status 401 → redirect về /login
- Nếu response status 403 → hiển thị "Không có quyền"
- Sau thanh toán thành công, cần refresh: features, user profile, subscription status
```

---

## Checklist trước khi deploy

### Backend & Database

- [x] Tạo bảng database (`subscription_plans`, `subscriptions`, `payments`, `listening_history`)
- [x] Insert seed data cho 3 gói (1 Tháng/49K, 6 Tháng/199K, 1 Năm/349K)
- [x] Cấu hình VNPay `TmnCode` = `80ANYI0X`
- [x] Cấu hình VNPay `HashSecret` = `D7ZACNMZXODL8VXW95MOU72E0OZBHLPW`
- [x] API Record Play (`POST /interaction/record-play`) đã tạo
- [x] VNPay payment URL hoạt động (redirect, OTP, thanh toán OK)
- [x] Return URL xử lý cấp quyền (tạo subscription + cập nhật role)
- [x] Bỏ `vnp_IpnUrl` khỏi request (gây lỗi chữ ký)
- [x] HMAC-SHA512 ký trên URL-encoded query string

### Frontend

- [ ] Tạo trang pricing (hiển thị 3 gói, gọi `GET /subscription/plans`)
- [ ] Tạo trang payment result (`/payment/result`, gọi `GET /payment/vnpay-return`)
- [ ] Tích hợp Feature Guard cho upload/schedule/analytics (dùng `GET /subscription/features`)
- [ ] Tích hợp Record Play trong audio player (gọi `POST /interaction/record-play`)
- [ ] Hiển thị subscription status trong profile

### Testing

- [x] Test thanh toán sandbox với thẻ NCB test (OTP 123456 → thành công)
- [ ] Test luồng: success + role update, fail, duplicate, expired subscription

---

_Tài liệu được tạo ngày: 08/03/2026_
_Cập nhật: 09/03/2026_
_Version: 1.5 — Chuyển logic cấp quyền từ IPN sang Return URL, bỏ vnp_IpnUrl, cập nhật HashSecret, cập nhật flow diagram_
