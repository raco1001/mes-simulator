import { useCallback, useEffect, useRef, useState } from 'react'
import type { Node } from '@xyflow/react'
import {
  getRunningSimulationRuns,
  subscribeSimulationEvents,
} from '@/entities/simulation'
import type { AssetNodeData } from '../ui/AssetNode'

export type SimCanvasPhase = 'idle' | 'running' | 'stoppedCached'

export function useCanvasSimulationSync(
  setNodes: React.Dispatch<
    React.SetStateAction<Node<AssetNodeData>[]>
  >,
  canvasReady: boolean,
) {
  const [simCanvasPhase, setSimCanvasPhase] =
    useState<SimCanvasPhase>('idle')
  const [running, setRunning] = useState(false)
  const [activeRunId, setActiveRunId] = useState<string | null>(null)
  const [tickCount, setTickCount] = useState(0)
  const sseCleanupRef = useRef<(() => void) | null>(null)

  const stopSseSubscription = useCallback(() => {
    sseCleanupRef.current?.()
    sseCleanupRef.current = null
  }, [])

  const startSseSubscription = useCallback(() => {
    stopSseSubscription()
    sseCleanupRef.current = subscribeSimulationEvents((tick) => {
      setNodes((nds) =>
        nds.map((n) =>
          n.id === tick.assetId
            ? {
                ...n,
                data: {
                  ...n.data,
                  liveStatus: tick.status,
                  liveProperties: tick.properties,
                },
              }
            : n,
        ),
      )
      setTickCount((c) => c + 1)
    })
  }, [setNodes, stopSseSubscription])

  const syncRunningFromServer = useCallback(async (): Promise<boolean> => {
    try {
      const runs = await getRunningSimulationRuns()
      const active = runs.filter(
        (r) =>
          r.id &&
          (r.status === 'Running' ||
            String(r.status).toLowerCase() === 'running'),
      )
      if (active.length === 0) return false
      const first = active[0]
      setActiveRunId(first.id!)
      setRunning(true)
      setSimCanvasPhase('running')
      startSseSubscription()
      return true
    } catch {
      return false
    }
  }, [startSseSubscription])

  useEffect(() => {
    if (!canvasReady) return
    let cancelled = false
    void (async () => {
      try {
        const runs = await getRunningSimulationRuns()
        if (cancelled) return
        const active = runs.filter(
          (r) =>
            r.id &&
            (r.status === 'Running' ||
              String(r.status).toLowerCase() === 'running'),
        )
        if (active.length === 0) {
          setSimCanvasPhase('idle')
          return
        }
        const first = active[0]
        setActiveRunId(first.id!)
        setRunning(true)
        setSimCanvasPhase('running')
        startSseSubscription()
      } catch {
        if (!cancelled) setSimCanvasPhase('idle')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [canvasReady, startSseSubscription])

  useEffect(() => {
    return () => {
      stopSseSubscription()
    }
  }, [stopSseSubscription])

  const clearLiveOnAllNodes = useCallback(() => {
    setNodes((nds) =>
      nds.map((n) => ({
        ...n,
        data: {
          ...n.data,
          liveStatus: undefined,
          liveProperties: undefined,
        },
      })),
    )
  }, [setNodes])


  const onContinuousStarted = useCallback(() => {
    setRunning(true)
    setSimCanvasPhase('running')
  }, [])

  const onContinuousStopped = useCallback(() => {
    stopSseSubscription()
    setRunning(false)
    setActiveRunId(null)
    setSimCanvasPhase('stoppedCached')
  }, [stopSseSubscription])

  const onSingleRunStarted = useCallback(() => {
    setRunning(true)
    setSimCanvasPhase('running')
  }, [])

  const onSingleRunEnded = useCallback(() => {
    setRunning(false)
    setSimCanvasPhase('idle')
    clearLiveOnAllNodes()
  }, [clearLiveOnAllNodes])

  return {
    simCanvasPhase,
    setSimCanvasPhase,
    running,
    setRunning,
    activeRunId,
    setActiveRunId,
    tickCount,
    setTickCount,
    startSseSubscription,
    stopSseSubscription,
    syncRunningFromServer,
    onContinuousStarted,
    onContinuousStopped,
    onSingleRunStarted,
    onSingleRunEnded,
    clearLiveOnAllNodes,
  }
}
