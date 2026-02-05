"""
Zalo OA API Client - Python Example

This module provides a Python client to interact with ZaloOA API service.

Usage:
    pip install requests

    from zalo_api_client import ZaloOAClient

    client = ZaloOAClient("https://your-api-url.com/api/zalooa")

    # Get connected OA accounts
    accounts = client.get_accounts()

    # Send message
    client.send_message(
        oa_account_id="guid-here",
        zalo_user_id="user-zalo-id",
        text="Hello from Python!"
    )
"""

import requests
from typing import List, Dict, Optional, Any
from dataclasses import dataclass
from enum import IntEnum


class MessageType(IntEnum):
    TEXT = 0
    IMAGE = 1
    FILE = 2
    STICKER = 3


@dataclass
class OAAccount:
    id: str
    oa_id: str
    name: str
    avatar_url: Optional[str]
    status: int
    auth_type: int
    created_at: str
    updated_at: str


@dataclass
class Follower:
    id: str
    zalo_user_id: str
    display_name: Optional[str]
    avatar_url: Optional[str]
    is_follower: bool
    last_message_preview: Optional[str]
    last_message_at: Optional[str]
    unread_count: int


@dataclass
class Message:
    id: str
    zalo_message_id: Optional[str]
    direction: int  # 0 = Incoming, 1 = Outgoing
    type: int
    content: str
    attachment_url: Optional[str]
    sent_at: str
    status: int


class ZaloOAClient:
    """Python client for ZaloOA API"""

    def __init__(self, base_url: str, headers: Optional[Dict[str, str]] = None):
        """
        Initialize the client.

        Args:
            base_url: Base URL of ZaloOA API (e.g., "https://your-domain.com/api/zalooa")
            headers: Optional additional headers
        """
        self.base_url = base_url.rstrip('/')
        self.headers = {
            "Content-Type": "application/json",
            "ngrok-skip-browser-warning": "true",  # Skip ngrok warning page
            **(headers or {})
        }

    def _request(
        self,
        method: str,
        endpoint: str,
        json: Optional[Dict] = None,
        params: Optional[Dict] = None
    ) -> Dict[str, Any]:
        """Make HTTP request to API"""
        url = f"{self.base_url}{endpoint}"

        response = requests.request(
            method=method,
            url=url,
            json=json,
            params=params,
            headers=self.headers
        )

        if not response.ok:
            try:
                error = response.json().get('error', response.text)
            except:
                error = response.text
            raise Exception(f"API Error ({response.status_code}): {error}")

        return response.json() if response.text else {}

    # ==================== OA Account APIs ====================

    def get_accounts(self) -> List[OAAccount]:
        """Get all connected OA accounts"""
        data = self._request("GET", "")
        items = data.get('items', [])
        return [OAAccount(**item) for item in items]

    def get_account(self, account_id: str) -> OAAccount:
        """Get specific OA account by ID"""
        data = self._request("GET", f"/{account_id}")
        return OAAccount(**data)

    def disconnect_account(self, account_id: str) -> None:
        """Disconnect OA account"""
        self._request("DELETE", f"/{account_id}")

    def refresh_token(self, account_id: str) -> Dict[str, Any]:
        """Refresh OA access token"""
        return self._request("POST", f"/{account_id}/refresh-token")

    # ==================== Messaging APIs ====================

    def get_followers(
        self,
        oa_account_id: str,
        offset: int = 0,
        limit: int = 20
    ) -> List[Follower]:
        """Get list of followers for an OA account"""
        data = self._request(
            "GET",
            f"/{oa_account_id}/followers",
            params={"offset": offset, "limit": limit}
        )
        followers = data.get('followers', [])
        return [Follower(**f) for f in followers]

    def get_messages(
        self,
        oa_account_id: str,
        zalo_user_id: str,
        offset: int = 0,
        limit: int = 50
    ) -> List[Message]:
        """Get message history with a Zalo user"""
        data = self._request(
            "GET",
            f"/{oa_account_id}/messages",
            params={
                "zaloUserId": zalo_user_id,
                "offset": offset,
                "limit": limit
            }
        )
        messages = data.get('messages', [])
        return [Message(**m) for m in messages]

    def send_message(
        self,
        oa_account_id: str,
        zalo_user_id: str,
        text: Optional[str] = None,
        message_type: MessageType = MessageType.TEXT,
        attachment_url: Optional[str] = None,
        attachment_id: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Send a message to a Zalo user.

        Args:
            oa_account_id: GUID of the OA account
            zalo_user_id: Zalo user ID to send message to
            text: Text content (required for TEXT type)
            message_type: Type of message (TEXT, IMAGE, FILE, STICKER)
            attachment_url: URL of attachment (for IMAGE type)
            attachment_id: ID of uploaded attachment (for FILE type)

        Returns:
            Response with messageId and sentAt
        """
        payload = {
            "zaloUserId": zalo_user_id,
            "type": int(message_type)
        }

        if text:
            payload["text"] = text

        if attachment_url:
            payload["attachmentUrl"] = attachment_url

        if attachment_id:
            payload["attachmentId"] = attachment_id

        return self._request("POST", f"/{oa_account_id}/messages", json=payload)

    def send_text(
        self,
        oa_account_id: str,
        zalo_user_id: str,
        text: str
    ) -> Dict[str, Any]:
        """Shorthand to send text message"""
        return self.send_message(
            oa_account_id=oa_account_id,
            zalo_user_id=zalo_user_id,
            text=text,
            message_type=MessageType.TEXT
        )

    def send_image(
        self,
        oa_account_id: str,
        zalo_user_id: str,
        image_url: str,
        caption: Optional[str] = None
    ) -> Dict[str, Any]:
        """Shorthand to send image message"""
        return self.send_message(
            oa_account_id=oa_account_id,
            zalo_user_id=zalo_user_id,
            text=caption,
            message_type=MessageType.IMAGE,
            attachment_url=image_url
        )


# ==================== Example Usage ====================

if __name__ == "__main__":
    # Example usage
    API_BASE_URL = "https://tomorrow-chameleonic-heath.ngrok-free.dev/api/zalooa"

    client = ZaloOAClient(API_BASE_URL)

    print("=" * 60)
    print("  Zalo OA API Client - Python Example")
    print("=" * 60)

    try:
        # Get connected accounts
        print("\n[1] Getting connected OA accounts...")
        accounts = client.get_accounts()

        if not accounts:
            print("    No accounts connected.")
        else:
            for acc in accounts:
                print(f"    - {acc.name} ({acc.id})")

            # Get followers of first account
            if accounts:
                account = accounts[0]
                print(f"\n[2] Getting followers for '{account.name}'...")
                followers = client.get_followers(account.id)

                if not followers:
                    print("    No followers found.")
                else:
                    for f in followers:
                        print(f"    - {f.display_name or 'Unknown'} ({f.zalo_user_id})")

                    # Send message to first follower
                    # Uncomment to test sending message
                    # follower = followers[0]
                    # print(f"\n[3] Sending message to {follower.display_name}...")
                    # result = client.send_text(
                    #     oa_account_id=account.id,
                    #     zalo_user_id=follower.zalo_user_id,
                    #     text="Hello from Python! This is a test message."
                    # )
                    # print(f"    Message sent! ID: {result.get('messageId')}")

    except Exception as e:
        print(f"\n[ERROR] {e}")

    print("\n" + "=" * 60)
