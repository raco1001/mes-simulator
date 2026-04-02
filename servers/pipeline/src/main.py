"""FastAPI application entrypoint for pipeline analytics."""
import asyncio
import os
from contextlib import asynccontextmanager, suppress

from fastapi import FastAPI

from api.health import router as health_router
from api.metrics import router as metrics_router
from api.recommendations import router as recommendations_router
from workers.asset_worker import AssetWorker


@asynccontextmanager
async def lifespan(_: FastAPI):
    worker = AssetWorker()
    should_start_worker = os.getenv("PYTEST_CURRENT_TEST") is None
    task = asyncio.create_task(asyncio.to_thread(worker.run)) if should_start_worker else None
    try:
        yield
    finally:
        if task is not None:
            worker.running = False
            with suppress(Exception):
                await asyncio.wait_for(task, timeout=1.0)


app = FastAPI(title="Pipeline Analytics", version="0.1.0", lifespan=lifespan)
app.include_router(health_router)
app.include_router(metrics_router)
app.include_router(recommendations_router)
