# Hướng dẫn tích hợp ZaloOA Service

## Tổng quan

ZaloOA Service là microservice quản lý kết nối và nhắn tin với Zalo Official Account. Service này có thể tích hợp vào hệ thống lớn thông qua:

- **REST API** - Quản lý OA, gửi/nhận tin nhắn
- **RabbitMQ** - Nhận events khi có tin nhắn mới, user follow/unfollow
- **SignalR** - Real-time notification cho frontend

---

## Kiến trúc

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Zalo Server   │────▶│   ZaloOA API    │────▶│   RabbitMQ      │
│   (Webhook)     │     │   (Port 7127)   │     │   (Port 5673)   │
└─────────────────┘     └────────┬────────┘     └────────┬────────┘
                                 │                       │
                                 │ SignalR               │ Events
                                 ▼                       ▼
                        ┌─────────────────┐     ┌─────────────────┐
                        │   Frontend      │     │  Other Services │
                        │   (React)       │     │  (Consumers)    │
                        └─────────────────┘     └─────────────────┘
```

---

## 1. Cấu hình Environment Variables

### ZaloOA API (.env)

```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=ZaloOADb;Username=your_user;Password=your_password

# JWT (phải giống với Identity Service)
Jwt__Secret=YourSuperSecretKeyHere_MustBeAtLeast32Characters!
Jwt__Issuer=Identity.API
Jwt__Audience=Social.Client

# Zalo App Configuration
Zalo__AppId=YOUR_ZALO_APP_ID
Zalo__AppSecret=YOUR_ZALO_APP_SECRET
Zalo__DefaultRedirectUri=https://your-domain.com/api/zalooa/oauth/callback
Zalo__FrontendBaseUrl=https://your-frontend.com

# RabbitMQ
RabbitMQ__Enabled=true
RabbitMQ__Host=localhost
RabbitMQ__Port=5673
RabbitMQ__Username=admin
RabbitMQ__Password=admin123
RabbitMQ__VirtualHost=/
```

---

## 2. RabbitMQ Events

### Exchange & Routing Keys

| Exchange | Type | Routing Key | Description |
|----------|------|-------------|-------------|
| `zalo.events` | topic | `zalo.message.received` | User gửi tin nhắn đến OA |
| `zalo.events` | topic | `zalo.user.follow` | User follow OA |
| `zalo.events` | topic | `zalo.user.unfollow` | User unfollow OA |

### Event Payloads

#### UserMessageReceivedEvent
```json
{
  "OAId": "123456789",
  "OAName": "My OA Name",
  "SenderId": "user_zalo_id",
  "SenderName": "User Display Name",
  "MessageId": "msg_123",
  "MessageType": "text",
  "Content": "Hello!",
  "AttachmentUrl": null,
  "ConversationId": "guid-conversation-id"
}
```

#### UserFollowEvent
```json
{
  "OAId": "123456789",
  "OAName": "My OA Name",
  "UserId": "user_zalo_id",
  "UserName": "User Display Name",
  "IsFollow": true
}
```

### Consumer Example (C#)

```csharp
public class ZaloMessageConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public ZaloMessageConsumer(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"],
            Port = int.Parse(config["RabbitMQ:Port"]),
            UserName = config["RabbitMQ:Username"],
            Password = config["RabbitMQ:Password"]
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange
        _channel.ExchangeDeclare("zalo.events", ExchangeType.Topic, durable: true);

        // Declare queue
        _channel.QueueDeclare("my-service.zalo.messages", durable: true, exclusive: false, autoDelete: false);

        // Bind queue to exchange
        _channel.QueueBind("my-service.zalo.messages", "zalo.events", "zalo.message.received");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = JsonSerializer.Deserialize<UserMessageReceivedEvent>(body);

            Console.WriteLine($"New message from {message.SenderName}: {message.Content}");

            // Process message here...

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume("my-service.zalo.messages", autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }
}
```

---

## 3. REST API Endpoints

### Authentication
Tất cả API (trừ webhook) yêu cầu JWT token trong header:
```
Authorization: Bearer <token>
```

### OA Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/zalooa` | Lấy danh sách OA đã kết nối |
| GET | `/api/zalooa/{id}` | Lấy thông tin OA theo ID |
| DELETE | `/api/zalooa/{id}` | Ngắt kết nối OA |
| GET | `/api/zalooa/oauth/authorize` | Redirect đến Zalo OAuth |
| GET | `/api/zalooa/health` | Health check |

### Messaging

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/zalooa/{oaId}/followers` | Lấy danh sách followers |
| GET | `/api/zalooa/{oaId}/messages?zaloUserId=xxx` | Lấy lịch sử tin nhắn |
| POST | `/api/zalooa/{oaId}/messages` | Gửi tin nhắn |

### Send Message Request
```json
{
  "zaloUserId": "user_zalo_id",
  "type": 0,
  "text": "Hello!",
  "attachmentUrl": null
}
```

Message Types:
- `0` - Text
- `1` - Image
- `2` - File
- `3` - Sticker

### Webhook (Public - No Auth)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/zalooa/webhook` | Zalo webhook verification |
| POST | `/api/zalooa/webhook` | Nhận events từ Zalo |

---

## 4. SignalR Integration

### Hub URL
```
wss://your-domain.com/hubs/chat
```

### Connection (JavaScript)
```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://your-domain.com/hubs/chat', {
    accessTokenFactory: () => localStorage.getItem('auth_token')
  })
  .withAutomaticReconnect()
  .build();

// Join OA group to receive messages
await connection.start();
await connection.invoke('JoinOAGroup', oaAccountId);

// Listen for new messages
connection.on('NewMessage', (notification) => {
  console.log('New message:', notification);
});
```

### NewMessage Notification
```json
{
  "messageId": "guid",
  "conversationId": "guid",
  "zaloUserId": "user_zalo_id",
  "senderName": "User Name",
  "senderAvatar": "https://...",
  "direction": 0,
  "type": 0,
  "content": "Hello!",
  "attachmentUrl": null,
  "thumbnailUrl": null,
  "sentAt": "2024-01-01T00:00:00Z"
}
```

Direction:
- `0` - Inbound (user gửi đến OA)
- `1` - Outbound (OA gửi đi)

---

## 5. Database Schema

### Tables

```sql
-- Bảng lưu OA accounts
ZaloOAAccounts (
  Id UUID PRIMARY KEY,
  UserId VARCHAR(450),        -- User ID từ Identity Service
  OAId VARCHAR(100),          -- Zalo OA ID
  Name VARCHAR(500),
  AvatarUrl VARCHAR(2000),
  AccessToken TEXT,
  RefreshToken TEXT,
  TokenExpiresAt TIMESTAMP,
  AuthType INT,               -- 0: OAuth2, 1: ApiKey
  Status INT,                 -- 0: Active, 1: Inactive, 2: TokenExpired
  CreatedAt TIMESTAMP,
  UpdatedAt TIMESTAMP
)

-- Bảng lưu Zalo users (followers)
ZaloUsers (
  Id UUID PRIMARY KEY,
  ZaloUserId VARCHAR(100),
  OAId VARCHAR(100),
  DisplayName VARCHAR(500),
  AvatarUrl VARCHAR(2000),
  IsFollower BOOLEAN,
  LastInteractionAt TIMESTAMP,
  FollowedAt TIMESTAMP,
  CreatedAt TIMESTAMP,
  UpdatedAt TIMESTAMP
)

-- Bảng lưu conversations
ZaloConversations (
  Id UUID PRIMARY KEY,
  OAAccountId UUID REFERENCES ZaloOAAccounts,
  ZaloUserId UUID REFERENCES ZaloUsers,
  LastMessagePreview VARCHAR(200),
  LastMessageAt TIMESTAMP,
  UnreadCount INT,
  Status INT,
  CreatedAt TIMESTAMP,
  UpdatedAt TIMESTAMP
)

-- Bảng lưu messages
ZaloMessages (
  Id UUID PRIMARY KEY,
  ConversationId UUID REFERENCES ZaloConversations,
  ZaloMessageId VARCHAR(100),
  Direction INT,              -- 0: Inbound, 1: Outbound
  Type INT,                   -- 0: Text, 1: Image, 2: File, etc.
  Content TEXT,
  AttachmentUrl VARCHAR(2000),
  AttachmentName VARCHAR(500),
  ThumbnailUrl VARCHAR(2000),
  SentAt TIMESTAMP,
  Status INT,
  ErrorMessage VARCHAR(1000),
  CreatedAt TIMESTAMP,
  UpdatedAt TIMESTAMP
)
```

---

## 6. Docker Deployment

### docker-compose.yml

```yaml
version: '3.8'

services:
  zalooa-api:
    build:
      context: .
      dockerfile: Services/ZaloOA/ZaloOA.API/Dockerfile
    ports:
      - "7127:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=ZaloOADb;Username=postgres;Password=postgres
      - Jwt__Secret=${JWT_SECRET}
      - Jwt__Issuer=Identity.API
      - Jwt__Audience=Social.Client
      - Zalo__AppId=${ZALO_APP_ID}
      - Zalo__AppSecret=${ZALO_APP_SECRET}
      - Zalo__DefaultRedirectUri=${ZALO_REDIRECT_URI}
      - Zalo__FrontendBaseUrl=${FRONTEND_URL}
      - RabbitMQ__Enabled=true
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Port=5672
      - RabbitMQ__Username=admin
      - RabbitMQ__Password=admin123
    depends_on:
      - postgres
      - rabbitmq

  postgres:
    image: postgres:15
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: ZaloOADb
    volumes:
      - postgres_data:/var/lib/postgresql/data

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123

volumes:
  postgres_data:
```

---

## 7. Zalo OA Admin Configuration

### Bước 1: Tạo Zalo App
1. Vào https://developers.zalo.me
2. Tạo ứng dụng mới
3. Lấy **App ID** và **App Secret**

### Bước 2: Cấu hình OAuth
1. Vào **Cài đặt** → **Đăng nhập OAuth**
2. Thêm **Redirect URI**: `https://your-domain.com/api/zalooa/oauth/callback`

### Bước 3: Cấu hình Webhook
1. Vào **Cài đặt** → **Webhook**
2. **Webhook URL**: `https://your-domain.com/api/zalooa/webhook`
3. Bật các events:
   - `user_send_text`
   - `user_send_image`
   - `user_send_file`
   - `user_send_sticker`
   - `user_send_gif`
   - `follow`
   - `unfollow`

### Bước 4: Xin quyền OA
Trong **Quản lý quyền**, xin các quyền:
- `send_message` - Gửi tin nhắn
- `manage_oa` - Quản lý OA

---

## 8. Checklist tích hợp

- [ ] Cấu hình database connection
- [ ] Cấu hình JWT (giống Identity Service)
- [ ] Cấu hình Zalo App credentials
- [ ] Cấu hình RabbitMQ connection
- [ ] Setup webhook URL trong Zalo OA Admin
- [ ] Tạo RabbitMQ consumer trong service khác (nếu cần)
- [ ] Tích hợp SignalR vào frontend (nếu cần real-time)
- [ ] Test OAuth flow
- [ ] Test webhook nhận tin nhắn
- [ ] Test gửi tin nhắn

---

## 9. Troubleshooting

### Webhook không nhận được
1. Kiểm tra ngrok/domain có accessible không
2. Kiểm tra webhook URL đúng chưa
3. Kiểm tra các events đã bật trong Zalo OA Admin
4. Xem logs backend: `[WEBHOOK] ========== Webhook Received ==========`

### SignalR không real-time
1. Kiểm tra frontend join đúng OA group
2. So sánh OA ID trong logs backend và frontend
3. Kiểm tra WebSocket connection trong browser DevTools

### Token expired
1. Gọi API refresh token: `POST /api/zalooa/{id}/refresh-token`
2. Hoặc kết nối lại OA

### RabbitMQ không nhận events
1. Kiểm tra exchange `zalo.events` đã tạo
2. Kiểm tra queue đã bind đúng routing key
3. Kiểm tra credentials RabbitMQ

---

## 10. Support

Nếu gặp vấn đề, kiểm tra logs:
- Backend console: `[WEBHOOK]`, `[SIGNALR]`, `[CHATHUB]`
- Browser console: `[SignalR]`, `[ZaloAPI]`
- RabbitMQ Management: http://localhost:15672
