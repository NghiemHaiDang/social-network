"""
Zalo OA Event Consumer - Python Example

This service subscribes to RabbitMQ and receives events from ZaloOA service:
- user_message_received: When a user sends a message to OA
- user_follow: When a user follows OA
- user_unfollow: When a user unfollows OA

Usage:
    pip install pika requests
    python zalo_consumer.py
"""

import json
import pika
import requests
from datetime import datetime

# Configuration
RABBITMQ_HOST = "localhost"
RABBITMQ_PORT = 5672
RABBITMQ_USER = "guest"
RABBITMQ_PASSWORD = "guest"

ZALO_API_BASE_URL = "https://tomorrow-chameleonic-heath.ngrok-free.dev/api/zalooa"

# Exchange and queue names
EXCHANGE_NAME = "zalo.events"
QUEUE_NAME = "python.zalo.consumer"

# Routing keys to subscribe
ROUTING_KEYS = [
    "zalo.message.received",
    "zalo.user.follow",
    "zalo.user.unfollow",
]


def handle_message_received(event: dict):
    """Handle user_message_received event"""
    print(f"\n{'='*50}")
    print(f"[MESSAGE RECEIVED] {datetime.now()}")
    print(f"  OA: {event.get('oa_name', 'Unknown')} ({event.get('oa_id')})")
    print(f"  From: {event.get('sender_name', 'Unknown')} ({event.get('sender_id')})")
    print(f"  Type: {event.get('message_type')}")
    print(f"  Content: {event.get('content')}")
    print(f"  Conversation ID: {event.get('conversation_id')}")
    print(f"{'='*50}\n")

    # Example: Auto-reply with AI bot
    # You can integrate with OpenAI, Claude, or any AI service here
    content = event.get('content', '').lower()

    if 'hello' in content or 'xin chào' in content:
        reply = "Xin chào! Tôi là AI Bot. Tôi có thể giúp gì cho bạn?"
        send_reply(event, reply)
    elif 'help' in content or 'giúp' in content:
        reply = "Tôi có thể hỗ trợ bạn với các vấn đề sau:\n1. Thông tin sản phẩm\n2. Hỗ trợ kỹ thuật\n3. Đặt hàng"
        send_reply(event, reply)


def handle_user_follow(event: dict):
    """Handle user_follow event"""
    print(f"\n{'='*50}")
    print(f"[USER FOLLOW] {datetime.now()}")
    print(f"  OA: {event.get('oa_name', 'Unknown')}")
    print(f"  User: {event.get('user_name', 'Unknown')} ({event.get('user_id')})")
    print(f"{'='*50}\n")

    # Example: Send welcome message to new follower
    # send_welcome_message(event)


def handle_user_unfollow(event: dict):
    """Handle user_unfollow event"""
    print(f"\n{'='*50}")
    print(f"[USER UNFOLLOW] {datetime.now()}")
    print(f"  OA: {event.get('oa_name', 'Unknown')}")
    print(f"  User: {event.get('user_name', 'Unknown')} ({event.get('user_id')})")
    print(f"{'='*50}\n")


def send_reply(event: dict, message: str):
    """Send reply message via ZaloOA API"""
    try:
        # You need to get oaAccountId from your database based on oa_id
        # For now, this is a placeholder
        oa_account_id = "YOUR_OA_ACCOUNT_GUID"  # Replace with actual GUID

        payload = {
            "zaloUserId": event.get('sender_id'),
            "type": 0,  # Text message
            "text": message
        }

        headers = {
            "Content-Type": "application/json",
            "ngrok-skip-browser-warning": "true"
        }

        response = requests.post(
            f"{ZALO_API_BASE_URL}/{oa_account_id}/messages",
            json=payload,
            headers=headers
        )

        if response.status_code == 200:
            print(f"[REPLY SENT] {message[:50]}...")
        else:
            print(f"[REPLY FAILED] {response.text}")

    except Exception as e:
        print(f"[REPLY ERROR] {e}")


def on_message(channel, method, properties, body):
    """Callback when message is received from RabbitMQ"""
    try:
        event = json.loads(body.decode('utf-8'))
        event_type = event.get('event_type', '')

        if event_type == 'user_message_received':
            handle_message_received(event)
        elif event_type == 'user_follow':
            handle_user_follow(event)
        elif event_type == 'user_unfollow':
            handle_user_unfollow(event)
        else:
            print(f"[UNKNOWN EVENT] {event_type}: {event}")

        # Acknowledge message
        channel.basic_ack(delivery_tag=method.delivery_tag)

    except Exception as e:
        print(f"[ERROR] Failed to process message: {e}")
        # Reject and requeue on error
        channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)


def main():
    """Main function to start the consumer"""
    print("=" * 60)
    print("  Zalo OA Event Consumer (Python)")
    print("=" * 60)
    print(f"Connecting to RabbitMQ at {RABBITMQ_HOST}:{RABBITMQ_PORT}...")

    # Connect to RabbitMQ
    credentials = pika.PlainCredentials(RABBITMQ_USER, RABBITMQ_PASSWORD)
    parameters = pika.ConnectionParameters(
        host=RABBITMQ_HOST,
        port=RABBITMQ_PORT,
        credentials=credentials
    )

    connection = pika.BlockingConnection(parameters)
    channel = connection.channel()

    # Declare exchange
    channel.exchange_declare(
        exchange=EXCHANGE_NAME,
        exchange_type='topic',
        durable=True
    )

    # Declare queue
    result = channel.queue_declare(queue=QUEUE_NAME, durable=True)
    queue_name = result.method.queue

    # Bind queue to exchange with routing keys
    for routing_key in ROUTING_KEYS:
        channel.queue_bind(
            exchange=EXCHANGE_NAME,
            queue=queue_name,
            routing_key=routing_key
        )
        print(f"  Subscribed to: {routing_key}")

    # Set prefetch count
    channel.basic_qos(prefetch_count=1)

    # Start consuming
    channel.basic_consume(queue=queue_name, on_message_callback=on_message)

    print("\n[*] Waiting for events. Press CTRL+C to exit.\n")

    try:
        channel.start_consuming()
    except KeyboardInterrupt:
        print("\n[*] Stopping consumer...")
        channel.stop_consuming()

    connection.close()
    print("[*] Consumer stopped.")


if __name__ == "__main__":
    main()
