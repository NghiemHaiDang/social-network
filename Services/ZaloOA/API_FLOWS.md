# ZaloOA Service - API Flows Documentation

## Muc luc

- [1. Tong quan kien truc](#1-tong-quan-kien-truc)
- [2. OAuth2 & Quan ly tai khoan OA](#2-oauth2--quan-ly-tai-khoan-oa)
- [3. Messaging](#3-messaging)
- [4. Webhook](#4-webhook)
- [5. Real-time (SignalR)](#5-real-time-signalr)
- [6. Phu luc](#6-phu-luc)

---

## 1. Tong quan kien truc

### Architecture Overview

```
                                    +-----------------------+
                                    |     Frontend App      |
                                    |  (React / localhost)  |
                                    +----------+------------+
                                               |
                              HTTP REST API     |    WebSocket (SignalR)
                                               |
                                    +----------v------------+
                                    |     ZaloOA.API        |
                                    |  (ASP.NET Core 8.0)   |
                                    |                       |
                                    |  Controllers:         |
                                    |   - ZaloOAController  |
                                    |   - ZaloMessaging-    |
                                    |     Controller        |
                                    |   - ZaloWebhook-      |
                                    |     Controller        |
                                    |                       |
                                    |  Hubs:                |
                                    |   - ChatHub (/hubs/   |
                                    |     chat)             |
                                    +----------+------------+
                                               |
                                    +----------v------------+
                                    |  ZaloOA.Application   |
                                    |                       |
                                    |  Services:            |
                                    |   - ZaloOAService     |
                                    |   - ZaloMessageService|
                                    |   - ZaloWebhookService|
                                    +----+--------+---------+
                                         |        |
                              +----------+        +----------+
                              |                              |
                   +----------v---------+       +-----------v-----------+
                   | ZaloOA.Infra-      |       |   Zalo OpenAPI        |
                   | structure          |       |   (External)          |
                   |                    |       |                       |
                   | - MongoDB Repos    |       | - OAuth Token URL     |
                   | - RabbitMQ Broker  |       | - OpenAPI v2.0/v3.0   |
                   +----------+---------+       +-----------------------+
                              |
                   +----------v---------+
                   |      MongoDB       |
                   |  (4 Collections)   |
                   +--------------------+
```

### Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|---------------|
| API | `ZaloOA.API` | Controllers, SignalR Hub, JWT Auth, CORS, DI setup |
| Application | `ZaloOA.Application` | Business logic, DTOs, Interfaces, Events, Result pattern |
| Infrastructure | `ZaloOA.Infrastructure` | MongoDB repositories, Zalo API client (HttpClient), RabbitMQ broker |
| Domain | `ZaloOA.Domain` | Entities, Enums, Base classes |

### Authentication

- **JWT Bearer**: Header `Authorization: Bearer <token>`
- Issuer: `Identity.API`, Audience: `Social.Client`
- UserId extracted from claim `ClaimTypes.NameIdentifier`
- SignalR token via query string: `?access_token=<token>`

---

## 2. OAuth2 & Quan ly tai khoan OA

> Controller: `ZaloOAController` | Route prefix: `api/zalooa` | Default: `[Authorize]`

---

### 2.1 GET /api/zalooa/oauth/authorize

**Tao URL OAuth - Redirect den trang Zalo**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[AllowAnonymous]` |
| Query Params | `redirectUri?` (string), `userId?` (string) |

**Flow:**

```
Client GET /api/zalooa/oauth/authorize?userId=abc123
  |
  v
ZaloOAController.GetOAuthAuthorizeUrl()
  |-- Xac dinh userId: tu query param hoac JWT claim
  |-- Neu khong co userId -> 400 BadRequest
  |
  v
ZaloOAService.GetOAuth2AuthorizeUrlAsync(userId, redirectUri)
  |-- Tao state = Base64("{userId}|{random_guid}")
  |-- Build URL: {OAuthAuthorizeUrl}?app_id={AppId}&redirect_uri={uri}&state={state}
  |-- Return OAuth2AuthorizeUrlResponse { AuthorizeUrl, State }
  |
  v
Controller: return Redirect(authorizeUrl)
  -> Browser redirect den https://oauth.zaloapp.com/v4/oa/permission?...
```

**Response:** HTTP 302 Redirect to Zalo OAuth page

**Error cases:**
- `400` - userId missing (not authenticated and no query param)

---

### 2.2 GET /api/zalooa/oauth/callback

**Xu ly callback tu Zalo sau khi user cap quyen**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[AllowAnonymous]` |
| Query Params | `code?`, `state?`, `error?`, `oa_id?` |

**Flow:**

```
Zalo redirect -> GET /api/zalooa/oauth/callback?code=xxx&state=yyy
  |
  v
ZaloOAController.OAuthCallback()
  |-- Neu co error param -> Redirect frontend?success=false&error=...
  |-- Neu thieu code hoac state -> Redirect frontend?success=false&error=missing_parameters
  |
  v
ZaloOAService.HandleOAuthCallbackAsync(code, state)
  |
  |-- [1] Decode state -> lay userId
  |     Base64 decode -> split "|" -> parts[0] = userId
  |     Loi decode -> Return Failure("Invalid state parameter")
  |
  |-- [2] Exchange code for token
  |     ZaloApiClient.ExchangeCodeForTokenAsync(code, redirectUri, null)
  |     POST https://oauth.zaloapp.com/v4/oa/access_token
  |     Loi -> Return Failure(message)
  |
  |-- [3] Get OA info
  |     ZaloApiClient.GetOAInfoAsync(accessToken)
  |     GET https://openapi.zalo.me/v2.0/oa/getoa
  |     Loi -> Return Failure(message)
  |
  |-- [4] Kiem tra account da ton tai chua
  |     ZaloOAAccountRepo.GetByUserIdAndOAIdAsync(userId, oaId)
  |
  |     [4a] Da ton tai:
  |       account.UpdateTokens(accessToken, refreshToken, expiresIn)
  |       account.UpdateOAInfo(name, avatar)
  |       repo.Update(account)
  |
  |     [4b] Chua ton tai:
  |       ZaloOAAccount.Create(userId, oaId, name, avatar, tokens, AuthType.OAuth2)
  |       repo.AddAsync(account)
  |
  |-- [5] SaveChangesAsync()
  |-- [6] Return OAuthCallbackResponse
  |       { Success: true, AccountId, OAName, RedirectUrl }
  |
  v
Controller: Redirect(result.RedirectUrl)
  -> Browser redirect den {FrontendBaseUrl}/zalo/callback?success=true&accountId=...&oa_name=...
```

**Response:** HTTP 302 Redirect to frontend callback URL

**Error cases:**
- Redirect with `?success=false&error=...` for: Zalo error, missing params, invalid state, token exchange failure, OA info failure

---

### 2.3 POST /api/zalooa/oauth/connect

**Ket noi OA bang OAuth2 (PKCE flow) - Frontend gui code truc tiep**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Request Body | `ConnectOAuth2Request` |

**Request Body:**
```json
{
  "code": "string (required)",
  "codeVerifier": "string (optional - for PKCE)"
}
```

**Flow:**

```
Client POST /api/zalooa/oauth/connect
  |
  v
ZaloOAController.ConnectWithOAuth2()
  |-- userId = JWT claim
  |
  v
ZaloOAService.ConnectWithOAuth2Async(userId, request)
  |
  |-- [1] Exchange code for token (with optional PKCE codeVerifier)
  |     ZaloApiClient.ExchangeCodeForTokenAsync(code, redirectUri, codeVerifier)
  |     Loi -> Return Failure
  |
  |-- [2] Get OA info
  |     ZaloApiClient.GetOAInfoAsync(accessToken)
  |     Loi -> Return Failure
  |
  |-- [3] Kiem tra account ton tai
  |     [3a] Da ton tai -> UpdateTokens, UpdateOAInfo, Update, Save
  |     [3b] Chua ton tai -> Create moi, AddAsync, Save
  |
  |-- [4] Return ZaloOAResponse
  |
  v
Controller: 200 OK with ZaloOAResponse
```

**Response (200):**
```json
{
  "id": "guid",
  "oaId": "string",
  "name": "string",
  "avatarUrl": "string",
  "authType": 0,
  "status": 0,
  "tokenExpiresAt": "datetime",
  "createdAt": "datetime"
}
```

**Error cases:**
- `400` - Token exchange failed, No access token, Get OA info failed

---

### 2.4 POST /api/zalooa/connect/apikey

**Ket noi OA bang API Key (Access Token truc tiep)**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Request Body | `ConnectApiKeyRequest` |

**Request Body:**
```json
{
  "accessToken": "string (required)",
  "refreshToken": "string (optional)"
}
```

**Flow:**

```
Client POST /api/zalooa/connect/apikey
  |
  v
ZaloOAController.ConnectWithApiKey()
  |-- userId = JWT claim
  |
  v
ZaloOAService.ConnectWithApiKeyAsync(userId, request)
  |
  |-- [1] Verify token bang cach lay OA info
  |     ZaloApiClient.GetOAInfoAsync(request.AccessToken)
  |     Loi -> Return Failure("Invalid access token or failed to get OA information")
  |
  |-- [2] Kiem tra account ton tai
  |     [2a] Da ton tai:
  |       UpdateTokens(accessToken, refreshToken, null)
  |       UpdateOAInfo(name, avatar)
  |       UpdateAuthType(AuthenticationType.ApiKey)
  |       Update + Save
  |
  |     [2b] Chua ton tai:
  |       ZaloOAAccount.Create(..., authType: AuthenticationType.ApiKey)
  |       AddAsync + Save
  |
  |-- [3] Return ZaloOAResponse
  |
  v
Controller: 200 OK with ZaloOAResponse
```

**Response (200):** Same as [2.3](#23-post-apizalooaoauthconnect)

**Error cases:**
- `400` - Invalid access token, Get OA info failed

---

### 2.5 GET /api/zalooa

**Lay danh sach tat ca tai khoan OA da ket noi cua user**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Params | None |

**Flow:**

```
Client GET /api/zalooa
  |
  v
ZaloOAController.GetConnectedAccounts()
  |-- userId = JWT claim
  |
  v
ZaloOAService.GetConnectedAccountsAsync(userId)
  |-- ZaloOAAccountRepo.GetByUserIdAsync(userId)
  |     -> MongoDB query: { UserId: userId }
  |-- Map to ZaloOAResponse list
  |-- Return ZaloOAListResponse { Items, TotalCount }
  |
  v
Controller: 200 OK with ZaloOAListResponse
```

**Response (200):**
```json
{
  "items": [
    {
      "id": "guid",
      "oaId": "string",
      "name": "string",
      "avatarUrl": "string",
      "authType": 0,
      "status": 0,
      "tokenExpiresAt": "datetime",
      "createdAt": "datetime"
    }
  ],
  "totalCount": 1
}
```

---

### 2.6 GET /api/zalooa/{id}

**Lay chi tiet mot tai khoan OA**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Route Params | `id` (Guid) |

**Flow:**

```
Client GET /api/zalooa/{id}
  |
  v
ZaloOAController.GetAccountById(id)
  |-- userId = JWT claim
  |
  v
ZaloOAService.GetAccountByIdAsync(userId, id)
  |-- ZaloOAAccountRepo.GetByIdAndUserIdAsync(id, userId)
  |     -> MongoDB query: { _id: id, UserId: userId }
  |-- Null -> Return Failure("Zalo OA account not found")
  |-- Map to ZaloOAResponse
  |
  v
Controller: 200 OK with ZaloOAResponse | 404 NotFound
```

**Response (200):** Single `ZaloOAResponse` object

**Error cases:**
- `404` - Account not found or does not belong to user

---

### 2.7 DELETE /api/zalooa/{id}

**Ngat ket noi (xoa) tai khoan OA**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Route Params | `id` (Guid) |

**Flow:**

```
Client DELETE /api/zalooa/{id}
  |
  v
ZaloOAController.DisconnectAccount(id)
  |-- userId = JWT claim
  |
  v
ZaloOAService.DisconnectAccountAsync(userId, id)
  |-- ZaloOAAccountRepo.GetByIdAndUserIdAsync(id, userId)
  |-- Null -> Return Failure("Zalo OA account not found")
  |-- repo.Remove(account)
  |-- SaveChangesAsync()
  |-- Return Success
  |
  v
Controller: 204 NoContent | 404 NotFound
```

**Response:** `204 No Content` on success

**Error cases:**
- `404` - Account not found

---

### 2.8 POST /api/zalooa/{id}/refresh-token

**Lam moi access token cua tai khoan OA**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Route Params | `id` (Guid) |

**Flow:**

```
Client POST /api/zalooa/{id}/refresh-token
  |
  v
ZaloOAController.RefreshToken(id)
  |-- userId = JWT claim
  |
  v
ZaloOAService.RefreshTokenAsync(userId, id)
  |
  |-- [1] Tim account
  |     GetByIdAndUserIdAsync(id, userId)
  |     Null -> Return Failure("Zalo OA account not found")
  |
  |-- [2] Kiem tra refresh token
  |     RefreshToken null/empty -> Return Failure("No refresh token available")
  |
  |-- [3] Goi Zalo API refresh
  |     ZaloApiClient.RefreshAccessTokenAsync(account.RefreshToken)
  |
  |     [3a] Loi:
  |       account.MarkAsTokenExpired()    // Status -> TokenExpired
  |       Update + Save
  |       Return Failure(message)
  |
  |     [3b] Thanh cong:
  |       account.UpdateTokens(newAccessToken, newRefreshToken, expiresIn)
  |       Update + Save
  |       Return ZaloOAResponse
  |
  v
Controller: 200 OK with ZaloOAResponse | 400 BadRequest
```

**Response (200):** `ZaloOAResponse` with updated token info

**Error cases:**
- `400` - Account not found, No refresh token, Zalo API refresh failed (account marked as TokenExpired)

---

### 2.9 GET /api/zalooa/health

**Health check endpoint**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[AllowAnonymous]` |
| Params | None |

**Flow:**

```
Client GET /api/zalooa/health
  |
  v
ZaloOAController.HealthCheck()
  |-- Return 200 OK with config info
```

**Response (200):**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "config": {
    "appId": "string",
    "redirectUri": "string",
    "frontendUrl": "string"
  }
}
```

---

## 3. Messaging

> Controller: `ZaloMessagingController` | Route prefix: `api/zalooa/{oaAccountId}` | `[Authorize]`

---

### 3.1 GET /api/zalooa/{oaAccountId}/followers

**Lay danh sach followers cua OA**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Route Params | `oaAccountId` (Guid) |
| Query Params | `offset` (int, default: 0), `limit` (int, default: 20) |

**Flow:**

```
Client GET /api/zalooa/{oaAccountId}/followers?offset=0&limit=20
  |
  v
ZaloMessagingController.GetFollowers(oaAccountId, offset, limit)
  |-- userId = JWT claim
  |
  v
ZaloMessageService.GetFollowersAsync(userId, oaAccountId, request)
  |
  |-- [1] Tim OA account
  |     GetByIdAndUserIdAsync(oaAccountId, userId)
  |     Null -> fallback: GetByIdAsync(oaAccountId) (dev mode)
  |     Van null -> Return Failure
  |
  |-- [2] Lay danh sach followers tu Zalo API
  |     ZaloApiClient.GetFollowersAsync(accessToken, offset, limit)
  |     POST https://openapi.zalo.me/v3.0/oa/user/getlist
  |     Loi -> Return Failure
  |
  |-- [3] Voi moi follower:
  |     |
  |     |-- [3a] Tim ZaloUser trong DB
  |     |     GetByZaloUserIdAndOAIdAsync(zaloUserId, oaId)
  |     |
  |     |-- [3b] Chua co -> Fetch profile tu Zalo API
  |     |     GetUserProfileAsync(accessToken, userId)
  |     |     POST https://openapi.zalo.me/v3.0/oa/user/detail
  |     |     Tao ZaloUser.Create(...) + AddAsync
  |     |
  |     |-- [3c] Da co -> Refresh profile data
  |     |     GetUserProfileAsync(accessToken, userId)
  |     |     zaloUser.UpdateProfile(name, avatar)
  |     |     zaloUser.SetFollower(isFollower)
  |     |     Update
  |     |
  |     |-- [3d] Tim conversation metadata
  |     |     GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUser.Id)
  |     |     -> Lay LastMessagePreview, LastMessageAt, UnreadCount
  |     |
  |     |-- [3e] Build FollowerResponse
  |
  |-- [4] SaveChangesAsync()
  |-- [5] Return FollowerListResponse
  |
  v
Controller: 200 OK with FollowerListResponse
```

**Response (200):**
```json
{
  "followers": [
    {
      "id": "guid",
      "zaloUserId": "string",
      "displayName": "string",
      "avatarUrl": "string",
      "isFollower": true,
      "lastInteractionAt": "datetime",
      "followedAt": "datetime",
      "lastMessagePreview": "string",
      "lastMessageAt": "datetime",
      "unreadCount": 0
    }
  ],
  "totalCount": 100,
  "offset": 0,
  "limit": 20
}
```

**Error cases:**
- `400` - OA account not found, Zalo API error

---

### 3.2 GET /api/zalooa/{oaAccountId}/messages

**Lay lich su tin nhan voi mot Zalo user cu the**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Route Params | `oaAccountId` (Guid) |
| Query Params | `zaloUserId` (string, **required**), `offset` (int, default: 0), `limit` (int, default: 50) |

**Flow:**

```
Client GET /api/zalooa/{oaAccountId}/messages?zaloUserId=xxx&offset=0&limit=50
  |
  v
ZaloMessagingController.GetMessages(oaAccountId, zaloUserId, offset, limit)
  |-- Validate zaloUserId required -> 400 if empty
  |-- userId = JWT claim
  |
  v
ZaloMessageService.GetMessagesAsync(userId, oaAccountId, request)
  |
  |-- [1] Tim OA account
  |     GetByIdAndUserIdAsync(oaAccountId, userId)
  |     Null -> Return Failure
  |
  |-- [2] Tim hoac tao ZaloUser
  |     GetByZaloUserIdAndOAIdAsync(zaloUserId, oaId)
  |     Chua co -> GetUserProfileAsync + ZaloUser.Create + Add + Save
  |
  |-- [3] Tim hoac tao Conversation
  |     GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUser.Id)
  |     Chua co -> ZaloConversation.Create + Add + Save
  |
  |-- [4] Lay tin nhan tu local DB
  |     GetByConversationIdAsync(conversationId, offset, limit)
  |     CountByConversationIdAsync(conversationId)
  |
  |-- [5] Neu khong co tin nhan local -> Sync tu Zalo API
  |     |   ZaloApiClient.GetConversationAsync(accessToken, zaloUserId, offset, limit)
  |     |   GET https://openapi.zalo.me/v3.0/oa/conversation?user_id=...
  |     |
  |     |   Voi moi message tu Zalo:
  |     |     - Kiem tra trung lap: GetByZaloMessageIdAsync(msgId)
  |     |     - Map direction: source=0 -> Inbound, source!=0 -> Outbound
  |     |     - Map type: MapZaloMessageType(type)
  |     |     - Parse timestamp: FromUnixTimeMilliseconds
  |     |     - Tao ZaloMessage.CreateInbound / CreateOutbound
  |     |     - AddAsync
  |     |
  |     |   SaveChangesAsync()
  |     |   Reload local messages
  |
  |-- [6] Reset unread count
  |     Neu conversation.UnreadCount > 0:
  |       conversation.ResetUnreadCount()
  |       Update + Save
  |
  |-- [7] Map to MessageResponse list
  |-- [8] Return MessageListResponse
  |
  v
Controller: 200 OK with MessageListResponse
```

**Response (200):**
```json
{
  "messages": [
    {
      "id": "guid",
      "zaloMessageId": "string",
      "direction": 0,
      "type": 0,
      "content": "string",
      "attachmentUrl": "string",
      "attachmentName": "string",
      "thumbnailUrl": "string",
      "sentAt": "datetime",
      "status": 1,
      "errorMessage": null
    }
  ],
  "totalCount": 100,
  "offset": 0,
  "limit": 50
}
```

**Error cases:**
- `400` - zaloUserId missing, OA account not found, Zalo API error

---

### 3.3 POST /api/zalooa/{oaAccountId}/messages

**Gui tin nhan den Zalo user**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[Authorize]` (JWT required) |
| Route Params | `oaAccountId` (Guid) |
| Request Body | `SendMessageRequest` |

**Request Body:**
```json
{
  "zaloUserId": "string (required)",
  "type": 0,
  "text": "string (required for Text type)",
  "attachmentUrl": "string (required for Image type)",
  "attachmentName": "string (optional)",
  "attachmentId": "string (required for File type)"
}
```

**Flow:**

```
Client POST /api/zalooa/{oaAccountId}/messages
  |
  v
ZaloMessagingController.SendMessage(oaAccountId, request)
  |-- Validate zaloUserId required -> 400 if empty
  |-- userId = JWT claim
  |
  v
ZaloMessageService.SendMessageAsync(userId, oaAccountId, request)
  |
  |-- [1] Tim OA account
  |     GetByIdAndUserIdAsync(oaAccountId, userId)
  |     Null -> Return Failure
  |
  |-- [2] Tim hoac tao ZaloUser
  |     GetByZaloUserIdAndOAIdAsync(zaloUserId, oaId)
  |     Chua co -> GetUserProfileAsync + Create + Add + Save
  |
  |-- [3] Tim hoac tao Conversation
  |     GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUser.Id)
  |     Chua co -> Create + Add + Save
  |
  |-- [4] Tao outbound message (Status = Pending)
  |     ZaloMessage.CreateOutbound(conversationId, type, text, attachmentUrl, attachmentName)
  |     AddAsync + Save
  |
  |-- [5] Gui tin nhan qua Zalo API (theo MessageType)
  |     |
  |     |-- Text:
  |     |     Validate text not empty
  |     |     SendTextMessageAsync(accessToken, zaloUserId, text)
  |     |     POST https://openapi.zalo.me/v3.0/oa/message/cs
  |     |     Body: { recipient: { user_id }, message: { text } }
  |     |
  |     |-- Image:
  |     |     Validate attachmentUrl not empty
  |     |     SendImageMessageAsync(accessToken, zaloUserId, imageUrl, text?)
  |     |     POST https://openapi.zalo.me/v3.0/oa/message/cs
  |     |     Body: { recipient: { user_id }, message: { text, attachment: { type: "template", payload: { template_type: "media", elements: [{ media_type: "image", url }] } } } }
  |     |
  |     |-- File:
  |     |     Validate attachmentId not empty
  |     |     SendFileMessageAsync(accessToken, zaloUserId, attachmentId)
  |     |     POST https://openapi.zalo.me/v3.0/oa/message/cs
  |     |     Body: { recipient: { user_id }, message: { attachment: { type: "file", payload: { token: attachmentId } } } }
  |     |
  |     |-- Other types:
  |     |     MarkAsFailed("Unsupported message type: {type}")
  |     |     Return Failure
  |
  |-- [6] Xu ly ket qua
  |     |
  |     |-- Loi (Error != 0):
  |     |     message.MarkAsFailed(errorMessage)
  |     |     Update + Save
  |     |     Return Failure
  |     |
  |     |-- Thanh cong:
  |     |     message.MarkAsSent(zaloMessageId)     // Status -> Sent
  |     |     conversation.UpdateLastMessage(text, sentAt)
  |     |     zaloUser.UpdateLastInteraction()
  |     |     Update all + Save
  |
  |-- [7] Return SendMessageResponse
  |
  v
Controller: 200 OK with SendMessageResponse
```

**Response (200):**
```json
{
  "messageId": "guid",
  "zaloMessageId": "string",
  "sentAt": "datetime"
}
```

**Error cases:**
- `400` - zaloUserId missing, OA account not found, Text required (for text type), AttachmentUrl required (for image), AttachmentId required (for file), Unsupported message type, Zalo API send failed

---

## 4. Webhook

> Controller: `ZaloWebhookController` | Route prefix: `api/zalooa/webhook` | `[AllowAnonymous]`

---

### 4.1 GET /api/zalooa/webhook

**Xac minh webhook URL - Zalo goi de verify**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[AllowAnonymous]` |
| Query Params | `oa_id?` (string) |

**Flow:**

```
Zalo Server GET /api/zalooa/webhook?oa_id=xxx
  |
  v
ZaloWebhookController.VerifyWebhook(oa_id)
  |-- Log request
  |-- Always return 200 OK (bat buoc de Zalo chap nhan webhook)
  |
  v
Response: 200 OK { status: "ok", message: "Webhook verified successfully" }
```

**Response (200):**
```json
{
  "status": "ok",
  "message": "Webhook verified successfully"
}
```

---

### 4.2 POST /api/zalooa/webhook

**Nhan webhook events tu Zalo**

| Thuoc tinh | Gia tri |
|-----------|---------|
| Auth | `[AllowAnonymous]` |
| Request Body | `ZaloWebhookPayload` |

**Request Body (vi du - user_send_text):**
```json
{
  "app_id": "string",
  "oa_id": "string",
  "event_name": "user_send_text",
  "timestamp": "1704067200000",
  "sender": { "id": "zalo_user_id" },
  "recipient": { "id": "oa_id" },
  "message": {
    "msg_id": "string",
    "text": "Hello!",
    "attachments": []
  }
}
```

**Flow tong quat:**

```
Zalo Server POST /api/zalooa/webhook
  |
  v
ZaloWebhookController.ReceiveWebhook(payload)
  |-- Log event info
  |-- OA ID fallback:
  |     Neu oa_id empty va event = user_send_* -> OAId = recipient.id
  |     Neu oa_id empty va event = oa_send_*   -> OAId = sender.id
  |
  v
ZaloWebhookService.ProcessWebhookAsync(payload)
  |-- Validate OAId not empty
  |-- Tim OA account: FindAsync(x => x.OAId == payload.OAId)
  |-- Null -> Return Success (skip - OA not registered)
  |
  |-- Route theo EventName:
  |     user_send_text      -> HandleUserMessageAsync(..., MessageType.Text)
  |     user_send_image     -> HandleUserMessageAsync(..., MessageType.Image)
  |     user_send_gif       -> HandleUserMessageAsync(..., MessageType.Gif)
  |     user_send_sticker   -> HandleUserMessageAsync(..., MessageType.Sticker)
  |     user_send_file      -> HandleUserMessageAsync(..., MessageType.File)
  |     user_send_audio     -> HandleUserMessageAsync(..., MessageType.Audio)
  |     user_send_video     -> HandleUserMessageAsync(..., MessageType.Video)
  |     user_send_location  -> HandleUserMessageAsync(..., MessageType.Location)
  |     user_send_business_card -> HandleUserMessageAsync(..., MessageType.BusinessCard)
  |     follow              -> HandleFollowEventAsync(..., isFollow: true)
  |     unfollow            -> HandleFollowEventAsync(..., isFollow: false)
  |     _                   -> Return Success (ignore unknown events)
  |
  v
Controller: Always return 200 OK (even on processing errors)
```

#### 4.2.1 HandleUserMessageAsync - Xu ly tin nhan tu user

```
HandleUserMessageAsync(payload, oaAccount, messageType)
  |
  |-- [1] Validate sender ID
  |     senderId = payload.Sender.Id
  |     Empty -> Return Failure("Sender ID is required")
  |
  |-- [2] Get or create ZaloUser
  |     GetByZaloUserIdAndOAIdAsync(senderId, oaId)
  |     Null -> ZaloUser.Create(senderId, oaId, isFollower: true) + Add + Save
  |
  |-- [3] Get or create Conversation
  |     GetByOAAccountIdAndZaloUserIdAsync(oaAccountId, zaloUserId)
  |     Null -> ZaloConversation.Create(oaAccountId, zaloUserId) + Add + Save
  |
  |-- [4] Kiem tra tin nhan trung lap
  |     GetByZaloMessageIdAsync(messageId)
  |     Da ton tai -> Return Success (skip)
  |
  |-- [5] Extract noi dung va attachment
  |     content = message.Text
  |     attachmentUrl = attachments[0].Payload.Url
  |     attachmentName = attachments[0].Payload.Name
  |     thumbnailUrl = attachments[0].Payload.Thumbnail
  |     Neu Location: content = "{latitude},{longitude}"
  |
  |-- [6] Parse timestamp
  |     payload.Timestamp -> DateTimeOffset.FromUnixTimeMilliseconds -> UTC
  |
  |-- [7] Tao inbound message
  |     ZaloMessage.CreateInbound(conversationId, messageId, type, content, sentAt,
  |       attachmentUrl, attachmentName, thumbnailUrl)
  |     AddAsync
  |
  |-- [8] Update conversation metadata
  |     conversation.UpdateLastMessage(previewContent, sentAt)
  |     conversation.IncrementUnreadCount()
  |     Update
  |
  |-- [9] Update user interaction
  |     zaloUser.UpdateLastInteraction()
  |     Update
  |
  |-- [10] SaveChangesAsync()
  |
  |-- [11] SignalR notification
  |     ChatNotificationService.NotifyNewMessageAsync(oaAccountId, notification)
  |     -> HubContext.Clients.Group("oa_{oaAccountId}").SendAsync("NewMessage", notification)
  |
  |-- [12] RabbitMQ event
  |     MessageBroker.PublishAsync("zalo.events", "zalo.message.received", UserMessageReceivedEvent)
  |
  v
Return Success
```

**SignalR Notification payload (NewMessage):**
```json
{
  "messageId": "guid",
  "conversationId": "guid",
  "zaloUserId": "string",
  "senderName": "string",
  "senderAvatar": "string",
  "direction": 0,
  "type": 0,
  "content": "string",
  "attachmentUrl": "string",
  "thumbnailUrl": "string",
  "sentAt": "datetime"
}
```

**RabbitMQ Event (UserMessageReceivedEvent):**
```json
{
  "event_type": "user_message_received",
  "timestamp": "datetime",
  "oa_id": "string",
  "oa_name": "string",
  "sender_id": "string",
  "sender_name": "string",
  "message_id": "string",
  "message_type": "text",
  "content": "string",
  "attachment_url": "string",
  "conversation_id": "guid"
}
```

#### 4.2.2 HandleFollowEventAsync - Xu ly su kien follow/unfollow

```
HandleFollowEventAsync(payload, oaAccount, isFollow)
  |
  |-- [1] Lay follower ID
  |     followerId = payload.Follower.Id ?? payload.Sender.Id
  |     Empty -> Return Failure("Follower ID is required")
  |
  |-- [2] Tim ZaloUser
  |     GetByZaloUserIdAndOAIdAsync(followerId, oaId)
  |
  |     [2a] Chua co + isFollow=true:
  |       ZaloUser.Create(followerId, oaId, isFollower: true) + Add
  |
  |     [2b] Chua co + isFollow=false:
  |       Return Success (ignore - user never in system)
  |
  |     [2c] Da co:
  |       zaloUser.SetFollower(isFollow)
  |       Update
  |
  |-- [3] SaveChangesAsync()
  |
  |-- [4] RabbitMQ event
  |     routingKey = isFollow ? "zalo.user.follow" : "zalo.user.unfollow"
  |     MessageBroker.PublishAsync("zalo.events", routingKey, UserFollowEvent)
  |
  v
Return Success
```

**RabbitMQ Event (UserFollowEvent):**
```json
{
  "event_type": "user_follow",
  "timestamp": "datetime",
  "oa_id": "string",
  "oa_name": "string",
  "user_id": "string",
  "user_name": "string",
  "is_follow": true
}
```

---

## 5. Real-time (SignalR)

> Hub: `ChatHub` | Path: `/hubs/chat` | Currently `[AllowAnonymous]` (testing)

### Connection Lifecycle

```
Client -> WebSocket connect: wss://host/hubs/chat?access_token=JWT_TOKEN
  |
  v
ChatHub.OnConnectedAsync()
  |-- Extract userId from JWT claim
  |-- Neu co userId: Groups.AddToGroupAsync(connectionId, "user_{userId}")
  |-- Log connection

Client disconnect:
  |
  v
ChatHub.OnDisconnectedAsync()
  |-- Groups.RemoveFromGroupAsync(connectionId, "user_{userId}")
  |-- Log disconnection
```

### Client -> Server Methods

#### JoinOAGroup(oaAccountId)

```
Client invoke: "JoinOAGroup", oaAccountId
  |
  v
ChatHub.JoinOAGroup(oaAccountId)
  |-- Groups.AddToGroupAsync(connectionId, "oa_{oaAccountId}")
  |-- Log join
  |
  v
Client now receives messages for this OA account
```

#### LeaveOAGroup(oaAccountId)

```
Client invoke: "LeaveOAGroup", oaAccountId
  |
  v
ChatHub.LeaveOAGroup(oaAccountId)
  |-- Groups.RemoveFromGroupAsync(connectionId, "oa_{oaAccountId}")
  |-- Log leave
```

### Server -> Client Broadcasts

| Method | Group | Trigger | Payload |
|--------|-------|---------|---------|
| `NewMessage` | `oa_{oaAccountId}` | Webhook nhan tin nhan moi tu user | `NewMessageNotification` |
| `MessageStatus` | `oa_{oaAccountId}` | Cap nhat trang thai tin nhan | `MessageStatusNotification` |

**NewMessageNotification:**

| Field | Type | Description |
|-------|------|-------------|
| MessageId | Guid | ID tin nhan trong DB |
| ConversationId | Guid | ID cuoc hoi thoai |
| ZaloUserId | string | Zalo user ID cua nguoi gui |
| SenderName | string? | Ten hien thi nguoi gui |
| SenderAvatar | string? | URL avatar nguoi gui |
| Direction | int | 0 = Inbound, 1 = Outbound |
| Type | int | MessageType enum value |
| Content | string | Noi dung tin nhan |
| AttachmentUrl | string? | URL attachment |
| ThumbnailUrl | string? | URL thumbnail |
| SentAt | DateTime | Thoi gian gui |

**MessageStatusNotification:**

| Field | Type | Description |
|-------|------|-------------|
| MessageId | Guid | ID tin nhan |
| Status | int | MessageStatus enum value |
| ErrorMessage | string? | Chi tiet loi (neu co) |

---

## 6. Phu luc

### 6.1 Enums

#### AuthenticationType
| Value | Name | Description |
|-------|------|-------------|
| 0 | OAuth2 | Ket noi qua OAuth2 flow |
| 1 | ApiKey | Ket noi bang API Key truc tiep |

#### OAStatus
| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Dang hoat dong binh thuong |
| 1 | Inactive | Khong hoat dong |
| 2 | TokenExpired | Token da het han, can refresh |

#### MessageDirection
| Value | Name | Description |
|-------|------|-------------|
| 0 | Inbound | Tin nhan tu Zalo user gui den OA |
| 1 | Outbound | Tin nhan tu OA gui den Zalo user |

#### MessageType
| Value | Name | Description |
|-------|------|-------------|
| 0 | Text | Van ban |
| 1 | Image | Hinh anh |
| 2 | File | Tap tin |
| 3 | Sticker | Sticker |
| 4 | Gif | GIF animation |
| 5 | Audio | Am thanh |
| 6 | Video | Video |
| 7 | Location | Vi tri (lat, long) |
| 8 | BusinessCard | Name card |
| 9 | List | Danh sach |
| 10 | RequestUserInfo | Yeu cau thong tin user |

#### MessageStatus
| Value | Name | Description |
|-------|------|-------------|
| 0 | Pending | Dang cho gui |
| 1 | Sent | Da gui thanh cong |
| 2 | Delivered | Da giao den nguoi nhan |
| 3 | Read | Da doc |
| 4 | Failed | Gui that bai |

#### ConversationStatus
| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Cuoc hoi thoai dang hoat dong |
| 1 | Archived | Da luu tru |

---

### 6.2 Zalo API Endpoints

| Method | URL | Description | Used By |
|--------|-----|-------------|---------|
| POST | `https://oauth.zaloapp.com/v4/oa/access_token` | Exchange code for token / Refresh token | ZaloOAService |
| GET | `https://openapi.zalo.me/v2.0/oa/getoa` | Get OA info | ZaloOAService |
| POST | `https://openapi.zalo.me/v3.0/oa/user/getlist` | Get followers list | ZaloMessageService |
| POST | `https://openapi.zalo.me/v3.0/oa/user/detail` | Get user profile | ZaloMessageService |
| GET | `https://openapi.zalo.me/v3.0/oa/conversation` | Get conversation history | ZaloMessageService |
| POST | `https://openapi.zalo.me/v3.0/oa/message/cs` | Send message (text/image/file) | ZaloMessageService |

**Common Headers:**
- `access_token`: Bearer token cho API calls
- `secret_key`: App secret cho token exchange

---

### 6.3 RabbitMQ Events

**Exchange:** `zalo.events` (Type: Topic, Durable: true, AutoDelete: false)

| Routing Key | Event Class | Trigger |
|-------------|------------|---------|
| `zalo.message.received` | `UserMessageReceivedEvent` | User gui tin nhan den OA (qua webhook) |
| `zalo.user.follow` | `UserFollowEvent` | User follow OA (qua webhook) |
| `zalo.user.unfollow` | `UserFollowEvent` | User unfollow OA (qua webhook) |
| `zalo.message.sent` | `OAMessageSentEvent` | OA gui tin nhan (chua su dung) |

**Message Properties:**
- DeliveryMode: 2 (Persistent)
- Serialization: JSON (camelCase)

---

### 6.4 Database (MongoDB)

**Database Name:** `ZaloOADb`

#### Collection: ZaloOAAccounts

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| UserId | string | ID user he thong (tu Identity service) |
| OAId | string | Zalo OA ID |
| Name | string | Ten OA |
| AvatarUrl | string? | URL avatar OA |
| AccessToken | string | Zalo access token |
| RefreshToken | string? | Zalo refresh token |
| TokenExpiresAt | DateTime? | Thoi diem token het han |
| AuthType | AuthenticationType | Loai xac thuc |
| Status | OAStatus | Trang thai tai khoan |
| CreatedAt | DateTime | Thoi diem tao |
| UpdatedAt | DateTime? | Thoi diem cap nhat |

**Indexes:**
- `UserId` (single)
- `(UserId, OAId)` - Unique compound index

#### Collection: ZaloUsers

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| ZaloUserId | string | Zalo user ID |
| OAId | string | Thuoc OA nao |
| DisplayName | string? | Ten hien thi |
| AvatarUrl | string? | URL avatar |
| IsFollower | bool | Dang follow OA? |
| LastInteractionAt | DateTime? | Lan tuong tac cuoi |
| FollowedAt | DateTime? | Thoi diem follow |
| CreatedAt | DateTime | Thoi diem tao |
| UpdatedAt | DateTime? | Thoi diem cap nhat |

**Indexes:**
- `(ZaloUserId, OAId)` - Unique compound index
- `OAId` (single)

#### Collection: ZaloConversations

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| OAAccountId | Guid | FK -> ZaloOAAccounts |
| ZaloUserId | Guid | FK -> ZaloUsers |
| LastMessagePreview | string? | Preview tin nhan cuoi |
| LastMessageAt | DateTime? | Thoi diem tin nhan cuoi |
| UnreadCount | int | So tin nhan chua doc |
| Status | ConversationStatus | Trang thai hoi thoai |
| CreatedAt | DateTime | Thoi diem tao |
| UpdatedAt | DateTime? | Thoi diem cap nhat |

**Indexes:**
- `(OAAccountId, ZaloUserId)` - Unique compound index
- `OAAccountId` (single)
- `LastMessageAt` (descending)

#### Collection: ZaloMessages

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| ConversationId | Guid | FK -> ZaloConversations |
| ZaloMessageId | string? | Zalo message ID (tu Zalo API) |
| Direction | MessageDirection | Chieu tin nhan |
| Type | MessageType | Loai tin nhan |
| Content | string | Noi dung |
| AttachmentUrl | string? | URL dinh kem |
| AttachmentName | string? | Ten file dinh kem |
| ThumbnailUrl | string? | URL thumbnail |
| SentAt | DateTime | Thoi diem gui |
| Status | MessageStatus | Trang thai gui |
| ErrorMessage | string? | Chi tiet loi |
| CreatedAt | DateTime | Thoi diem tao |
| UpdatedAt | DateTime? | Thoi diem cap nhat |

**Indexes:**
- `ConversationId` (single)
- `ZaloMessageId` (single)
- `SentAt` (single)

---

### 6.5 Webhook Event Names

| Event Name | Category | Description |
|------------|----------|-------------|
| `user_send_text` | Message | User gui van ban |
| `user_send_image` | Message | User gui hinh anh |
| `user_send_gif` | Message | User gui GIF |
| `user_send_sticker` | Message | User gui sticker |
| `user_send_file` | Message | User gui file |
| `user_send_audio` | Message | User gui am thanh |
| `user_send_video` | Message | User gui video |
| `user_send_location` | Message | User gui vi tri |
| `user_send_business_card` | Message | User gui name card |
| `follow` | Follow | User follow OA |
| `unfollow` | Follow | User unfollow OA |
| `oa_send_text` | OA Message | OA gui van ban (defined, not handled) |
| `oa_send_image` | OA Message | OA gui hinh anh (defined, not handled) |
| `oa_send_file` | OA Message | OA gui file (defined, not handled) |
| `oa_send_gif` | OA Message | OA gui GIF (defined, not handled) |
| `oa_send_list` | OA Message | OA gui danh sach (defined, not handled) |

---

### 6.6 Configuration

```
ConnectionStrings:
  DefaultConnection    MongoDB connection string
  DatabaseName         ZaloOADb

Jwt:
  Secret               Signing key (shared with Identity.API)
  Issuer               Identity.API
  Audience             Social.Client

Zalo:
  AppId                Zalo App ID
  AppSecret            Zalo App Secret
  OAuthAuthorizeUrl    https://oauth.zaloapp.com/v4/oa/permission
  OAuthTokenUrl        https://oauth.zaloapp.com/v4/oa/access_token
  OpenApiBaseUrl       https://openapi.zalo.me
  DefaultRedirectUri   Callback URL for OAuth
  FrontendBaseUrl      http://localhost:3000

RabbitMQ:
  Enabled              true/false
  Host                 localhost
  Port                 5672
  Username             guest
  Password             guest
  VirtualHost          /
```
