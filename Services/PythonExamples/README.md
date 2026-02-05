# Python Integration with ZaloOA Microservice

This directory contains Python examples for integrating with the ZaloOA service.

## Architecture

```
┌─────────────────┐     Webhook      ┌─────────────────┐
│      Zalo       │ ───────────────> │   ZaloOA API    │
│     Server      │                  │   (C# .NET)     │
└─────────────────┘                  └────────┬────────┘
                                              │
                                              │ Publish Events
                                              ▼
                                     ┌─────────────────┐
                                     │    RabbitMQ     │
                                     └────────┬────────┘
                                              │
                        ┌─────────────────────┼───────────────────┐
                        ▼                     ▼                   ▼
               ┌─────────────┐       ┌─────────────┐     ┌─────────────┐
               │  AI Bot     │       │  Analytics  │     │  Your       │
               │  (Python)   │       │  (Python)   │     │  Service    │
               └──────┬──────┘       └─────────────┘     └─────────────┘
                      │
                      │ REST API
                      ▼
               ┌─────────────────┐
               │   ZaloOA API    │
               └─────────────────┘
```

## Setup

```bash
# Install dependencies
pip install -r requirements.txt

# Or install individually
pip install pika requests
```

## Available Events (via RabbitMQ)

| Event Type | Routing Key | Description |
|------------|-------------|-------------|
| `user_message_received` | `zalo.message.received` | User sends message to OA |
| `user_follow` | `zalo.user.follow` | User follows OA |
| `user_unfollow` | `zalo.user.unfollow` | User unfollows OA |
| `oa_message_sent` | `zalo.message.sent` | OA sends message |

### Event Payload Examples

**user_message_received:**
```json
{
  "event_type": "user_message_received",
  "timestamp": "2024-01-30T10:00:00Z",
  "oa_id": "3183111066133648138",
  "oa_name": "AntCo AI",
  "sender_id": "2632749882749131756",
  "sender_name": "John Doe",
  "message_id": "msg_123",
  "message_type": "text",
  "content": "Hello!",
  "conversation_id": "guid-here"
}
```

**user_follow:**
```json
{
  "event_type": "user_follow",
  "timestamp": "2024-01-30T10:00:00Z",
  "oa_id": "3183111066133648138",
  "oa_name": "AntCo AI",
  "user_id": "2632749882749131756",
  "user_name": "John Doe",
  "is_follow": true
}
```

## Usage

### 1. Event Consumer (Subscribe to events)

```python
# Run the consumer
python zalo_consumer.py
```

The consumer will:
- Connect to RabbitMQ
- Subscribe to Zalo events
- Process incoming messages
- Example: Auto-reply with AI bot

### 2. API Client (Send messages)

```python
from zalo_api_client import ZaloOAClient, MessageType

# Initialize client
client = ZaloOAClient("https://your-api-url/api/zalooa")

# Get connected accounts
accounts = client.get_accounts()

# Get followers
followers = client.get_followers(account_id="guid")

# Send text message
client.send_text(
    oa_account_id="account-guid",
    zalo_user_id="user-zalo-id",
    text="Hello from Python!"
)

# Send image
client.send_image(
    oa_account_id="account-guid",
    zalo_user_id="user-zalo-id",
    image_url="https://example.com/image.jpg",
    caption="Check this out!"
)
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/zalooa` | Get all connected OA accounts |
| GET | `/api/zalooa/{id}` | Get specific OA account |
| DELETE | `/api/zalooa/{id}` | Disconnect OA account |
| POST | `/api/zalooa/{id}/refresh-token` | Refresh access token |
| GET | `/api/zalooa/{id}/followers` | Get followers list |
| GET | `/api/zalooa/{id}/messages` | Get message history |
| POST | `/api/zalooa/{id}/messages` | Send message |

## RabbitMQ Configuration

Default configuration in docker-compose:
- Host: `localhost` (or `rabbitmq` in Docker network)
- Port: `5672`
- Username: `guest`
- Password: `guest`
- Exchange: `zalo.events` (topic type)
