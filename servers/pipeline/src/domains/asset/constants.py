"""Asset domain constants."""


class AssetConstants:
    """Asset domain constants."""

    class Status:
        """Asset status values."""

        NORMAL = "normal"
        WARNING = "warning"
        ERROR = "error"

    class EventType:
        """Event type constants."""

        ASSET_CREATED = "asset.created"
        ASSET_HEALTH_UPDATED = "asset.health.updated"
        ALERT_GENERATED = "alert.generated"
