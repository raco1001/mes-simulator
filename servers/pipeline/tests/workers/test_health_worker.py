"""Tests for health worker CLI entry point."""
import json
import sys
from io import StringIO

import pytest

from workers.health_worker import main


class TestHealthWorkerMain:
    """main() prints JSON and exits 0 when healthy."""

    def test_stdout_is_valid_json_with_health_fields(self, capsys: pytest.CaptureFixture[str]) -> None:
        with pytest.raises(SystemExit):
            main()
        out, _ = capsys.readouterr()
        data = json.loads(out)
        assert "status" in data
        assert data["status"] == "healthy"
        assert "description" in data
        assert "application_name" in data
        assert "reported_at" in data

    def test_exit_code_zero_when_healthy(self) -> None:
        with pytest.raises(SystemExit) as exc_info:
            main()
        assert exc_info.value.code == 0
