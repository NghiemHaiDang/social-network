# Social Network - Microservices Architecture

## Mục Lục

- [Tổng Quan Kiến Trúc](#tổng-quan-kiến-trúc)
- [Cấu Trúc Thư Mục](#cấu-trúc-thư-mục)
- [Các Services](#các-services)
- [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
- [Cài Đặt và Cấu Hình](#cài-đặt-và-cấu-hình)
- [Chạy Dự Án](#chạy-dự-án)
- [API Endpoints](#api-endpoints)
- [Database](#database)
- [Troubleshooting](#troubleshooting)

---

## Tổng Quan Kiến Trúc

```
                                    ┌─────────────────────────────────┐
                                    │         API Gateway             │
                                    │         (Ocelot)                │
                                    │    Port: 5000 (HTTP)            │
                                    └─────────────┬───────────────────┘
                                                  │
                    ┌─────────────────────────────┼─────────────────────────────┐
                    │                             │                             │
                    ▼                             ▼                             ▼
      ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
      │    Identity API       │   │     ZaloOA API        │   │   Future Services     │
      │    Port: 5001         │   │     Port: 5051        │   │                       │
      │                       │   │                       │   │                       │
      │  - Authentication     │   │  - Zalo OAuth         │   │                       │
      │  - User Management    │   │  - Messaging          │   │                       │
      │  - JWT Tokens         │   │  - Webhooks           │   │                       │
      └───────────┬───────────┘   └───────────┬───────────┘   └───────────────────────┘
                  │                           │
                  └─────────────┬─────────────┘
                                │
                  ┌─────────────┼─────────────┐
                  │             │             │
                  ▼             ▼             ▼
            ┌──────────┐ ┌──────────┐ ┌──────────┐
            │PostgreSQL│ │ RabbitMQ │ │  Redis   │
            │  :5432   │ │  :5672   │ │  :6379   │
            └──────────┘ └──────────┘ └──────────┘
```

### Công Nghệ Sử Dụng

| Component | Technology |
|-----------|------------|
| API Gateway | Ocelot + Polly |
| Backend Services | ASP.NET Core 8.0 |
| Database | PostgreSQL 16 |
| Message Broker | RabbitMQ |
| Cache | Redis |
| Real-time | SignalR |
| Authentication | JWT Bearer |
| Container | Docker |

---

## Cấu Trúc Thư Mục

```
social-network/
├── Services/
│   ├── ApiGateway/                    # API Gateway (Ocelot)
│   │   └── ApiGateway/
│   │       ├── Program.cs
│   │       ├── ocelot.json            # Routes cho Docker
│   │       ├── ocelot.Development.json # Routes cho Local
│   │       └── Dockerfile
│   │
│   ├── Identity/                      # Identity Service
│   │   ├── Identity.API/              # Web API Layer
│   │   ├── Identity.Application/      # Business Logic
│   │   ├── Identity.Domain/           # Entities & Interfaces
│   │   └── Identity.Infrastructure/   # Data Access & External Services
│   │
│   └── ZaloOA/                        # Zalo OA Service
│       ├── ZaloOA.API/
│       ├── ZaloOA.Application/
│       ├── ZaloOA.Domain/
│       └── ZaloOA.Infrastructure/
│
├── BuildingBlocks/                    # Shared Libraries
│   ├── BuildingBlocks.Common/
│   └── BuildingBlocks.EventBus/
│
├── certs/                             # HTTPS Certificates
├── docker-compose.yml
├── Social-Network.slnx                # Solution File
├── .env                               # Environment Variables (không commit)
└── README.md
```

### Clean Architecture (Mỗi Service)

```
Service/
├── API/                 # Presentation Layer
│   ├── Controllers/     # HTTP Endpoints
│   ├── Hubs/           # SignalR Hubs
│   └── Program.cs      # App Configuration
│
├── Application/         # Application Layer
│   ├── DTOs/           # Data Transfer Objects
│   ├── Interfaces/     # Service Contracts
│   └── Services/       # Business Logic
│
├── Domain/              # Domain Layer
│   ├── Entities/       # Domain Models
│   └── Enums/          # Enumerations
│
└── Infrastructure/      # Infrastructure Layer
    ├── Data/           # DbContext
    ├── Repositories/   # Data Access
    ├── Migrations/     # EF Migrations
    └── Services/       # External Services
```

---

## Các Services

### 1. API Gateway (Port 5000)

Entry point duy nhất cho tất cả requests từ client.

**Chức năng:**
- Routing requests đến các downstream services
- JWT Authentication tại gateway level
- Rate limiting
- Circuit breaker (Polly)
- WebSocket proxy cho SignalR

### 2. Identity API (Port 5001)

Quản lý authentication và user.

**Chức năng:**
- Đăng ký / Đăng nhập
- JWT Token generation
- Refresh Token
- User profile management

### 3. ZaloOA API (Port 5051)

Tích hợp Zalo Official Account.

**Chức năng:**
- Zalo OAuth 2.0
- Gửi/nhận tin nhắn
- Webhook xử lý events từ Zalo
- Real-time notifications (SignalR)

---

## Yêu Cầu Hệ Thống

### Development

- .NET 8.0 SDK
- PostgreSQL 16+ (hoặc Docker)
- Node.js 18+ (cho frontend - nếu có)
- IDE: Visual Studio 2022 / VS Code / Rider

### Docker

- Docker Desktop 4.x+
- Docker Compose v2+

---

## Cài Đặt và Cấu Hình

### Bước 1: Clone Repository

```bash
git clone <repository-url>
cd social-network
```

### Bước 2: Tạo File Environment

Tạo file `.env` ở thư mục root:

```env
# ===========================================
# DATABASE
# ===========================================
POSTGRES_USER=antcoai
POSTGRES_PASSWORD=your_secure_password    # Đổi password
POSTGRES_DB=IdentityDb

# Database Host (Docker: postgres, Local: localhost)
DB_HOST=postgres
DB_PORT=5432

# Database Names
IDENTITY_DB_NAME=IdentityDb
ZALOOA_DB_NAME=ZaloOADb

# ===========================================
# JWT CONFIGURATION
# ===========================================
JWT_SECRET=YourSuperSecretKeyHere_MustBeAtLeast32Characters!  # Đổi secret key
JWT_ISSUER=social-network
JWT_AUDIENCE=social-network-users

# ===========================================
# ZALO OA CONFIGURATION
# Lấy từ https://developers.zalo.me
# ===========================================
ZALO_APP_ID=your_zalo_app_id              # Thay bằng App ID thật
ZALO_APP_SECRET=your_zalo_app_secret      # Thay bằng App Secret thật
ZALO_REDIRECT_URI=https://your-domain.com/api/zalooa/oauth/callback
ZALO_FRONTEND_URL=http://localhost:3000

# ===========================================
# RABBITMQ
# ===========================================
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=admin123                 # Đổi password

# RabbitMQ Host (Docker: rabbitmq, Local: localhost)
RABBITMQ_HOST=rabbitmq
RABBITMQ_INTERNAL_PORT=5672

# ===========================================
# HTTPS CERTIFICATE
# ===========================================
CERT_PASSWORD=password

# ===========================================
# PORTS (External - Host machine)
# ===========================================
GATEWAY_HTTP_PORT=5000
IDENTITY_HTTP_PORT=5001
IDENTITY_HTTPS_PORT=5002
ZALOOA_HTTP_PORT=5051
ZALOOA_HTTPS_PORT=5052
POSTGRES_PORT=5432
REDIS_PORT=6379
RABBITMQ_PORT=5673
RABBITMQ_UI_PORT=15672
```

### Bước 4: Tạo HTTPS Certificate (Optional)

```bash
# Tạo thư mục certs
mkdir certs

# Tạo development certificate
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p password
dotnet dev-certs https --trust
```

---

## Chạy Dự Án

### Môi Trường 1: Docker (Recommended)

**Chạy tất cả services:**

```bash
# Build và start tất cả
docker-compose up --build

# Hoặc chạy background
docker-compose up -d --build
```

**Chạy từng service:**

```bash
# Chỉ infrastructure (DB, Redis, RabbitMQ)
docker-compose up postgres redis rabbitmq

# Thêm API Gateway
docker-compose up api-gateway

# Thêm Identity API
docker-compose up identity-api

# Thêm ZaloOA API
docker-compose up zalooa-api
```

**Dừng services:**

```bash
# Dừng tất cả
docker-compose down

# Dừng và xóa volumes (reset data)
docker-compose down -v
```

**Xem logs:**

```bash
# Tất cả services
docker-compose logs -f

# Chỉ một service
docker-compose logs -f api-gateway
```

### Môi Trường 2: Local Development

**Bước 1: Chạy Infrastructure bằng Docker**

```bash
# Chỉ chạy PostgreSQL, Redis, RabbitMQ
docker-compose up -d postgres redis rabbitmq
```

**Bước 2: Chạy các Services**

```bash
# Terminal 1: Identity API
cd Services/Identity/Identity.API
dotnet run

# Terminal 2: ZaloOA API
cd Services/ZaloOA/ZaloOA.API
dotnet run

# Terminal 3: API Gateway
cd Services/ApiGateway/ApiGateway
dotnet run
```

**Hoặc chạy từ Solution:**

```bash
# Mở Visual Studio / Rider
# Chọn multiple startup projects:
# - ApiGateway
# - Identity.API
# - ZaloOA.API
```

### Môi Trường 3: Hybrid (Local API + Docker Infrastructure)

Phù hợp khi debug API services.

```bash
# 1. Chạy infrastructure
docker-compose up -d postgres redis rabbitmq

# 2. Chạy API services local với hot reload
cd Services/Identity/Identity.API
dotnet watch run
```

---

## API Endpoints

### Qua API Gateway (Port 5000)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Đăng ký user mới |
| POST | `/api/auth/login` | No | Đăng nhập |
| POST | `/api/auth/refresh-token` | No | Làm mới token |
| GET | `/api/users` | Yes | Danh sách users |
| GET | `/api/users/{id}` | Yes | Thông tin user |
| GET | `/api/zalooa/oauth/url` | Yes | Lấy Zalo OAuth URL |
| GET | `/api/zalooa/oauth/callback` | No | Zalo OAuth callback |
| POST | `/api/zalooa/webhook` | No | Zalo webhook |
| POST | `/api/messaging/send` | Yes | Gửi tin nhắn Zalo |
| WS | `/hubs/chat` | No* | SignalR Hub |

*WebSocket authentication qua query string `?access_token=xxx`

### Direct Access (Development Only)

| Service | HTTP | HTTPS |
|---------|------|-------|
| Identity API | http://localhost:5001 | https://localhost:5002 |
| ZaloOA API | http://localhost:5051 | https://localhost:5052 |

---

## Database

### Migrations

```bash
# Identity Service
cd Services/Identity/Identity.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../Identity.API
dotnet ef database update --startup-project ../Identity.API

# ZaloOA Service
cd Services/ZaloOA/ZaloOA.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../ZaloOA.API
dotnet ef database update --startup-project ../ZaloOA.API
```

### Connection Strings

**Local:**
```
Host=localhost;Port=5432;Database=IdentityDb;Username=antcoai;Password=your_password
```

**Docker:**
```
Host=postgres;Port=5432;Database=IdentityDb;Username=antcoai;Password=your_password
```

---

## Troubleshooting

### 1. Port Already in Use

```bash
# Windows: Tìm process đang dùng port
netstat -ano | findstr :5000

# Kill process
taskkill /PID <PID> /F
```

### 2. Docker Network Issues

```bash
# Recreate networks
docker-compose down
docker network prune
docker-compose up --build
```

### 3. Database Connection Failed

```bash
# Kiểm tra PostgreSQL container
docker-compose logs postgres

# Kiểm tra connection
docker exec -it socialDB psql -U antcoai -d IdentityDb
```

### 4. JWT Token Invalid

- Kiểm tra `JWT_SECRET` giống nhau ở tất cả services
- Kiểm tra `JWT_ISSUER` và `JWT_AUDIENCE` khớp nhau
- Token có thể đã hết hạn

### 5. Zalo OAuth Error

- Kiểm tra `ZALO_APP_ID` và `ZALO_APP_SECRET`
- Kiểm tra Redirect URI đã đăng ký trên Zalo Developers
- Sử dụng ngrok cho local development:
  ```bash
  ngrok http 5000
  # Cập nhật ZALO_REDIRECT_URI với ngrok URL
  ```

### 6. RabbitMQ Connection Failed

```bash
# Kiểm tra RabbitMQ status
docker-compose logs rabbitmq

# Truy cập Management UI
# http://localhost:15672
# Username: admin, Password: admin123
```

---

## Health Checks

| Service | Health Endpoint |
|---------|-----------------|
| API Gateway | http://localhost:5000/health |
| Identity API | http://localhost:5001/health |
| ZaloOA API | http://localhost:5051/health |

---

## Useful Commands

```bash
# Build solution
dotnet build Social-Network.slnx

# Run tests
dotnet test Social-Network.slnx

# Clean build
dotnet clean Social-Network.slnx

# Docker: Rebuild single service
docker-compose up --build api-gateway

# Docker: View resource usage
docker stats

# Docker: Enter container shell
docker exec -it api-gateway /bin/bash
```

---

## Contributing

1. Tạo branch mới từ `main`
2. Commit changes với message rõ ràng
3. Tạo Pull Request

---

## License

MIT License
