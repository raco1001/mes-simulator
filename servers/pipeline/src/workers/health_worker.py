"""Health check worker: CLI entry point for get_health."""
import json
import sys

from pipelines.health_pipeline import get_health


def main() -> None:
    """Print health status as JSON to stdout and exit 0 if healthy."""
    dto = get_health()
    payload = dto.model_dump(mode="json")
    print(json.dumps(payload, indent=2, default=str))
    if dto.status != "healthy":
        sys.exit(1)
    sys.exit(0)


if __name__ == "__main__":
    main()
