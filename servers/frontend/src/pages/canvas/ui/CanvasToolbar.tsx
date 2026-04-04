export function CanvasToolbar({
  relMode,
  objectTypePanelOpen,
  simPanelOpen,
  nodeCount,
  onAddAsset,
  onEnterRelMode,
  onExitRelMode,
  onToggleObjectTypePanel,
  onToggleSimPanel,
}: {
  relMode: boolean
  objectTypePanelOpen: boolean
  simPanelOpen: boolean
  nodeCount: number
  onAddAsset: () => void
  onEnterRelMode: () => void
  onExitRelMode: () => void
  onToggleObjectTypePanel: () => void
  onToggleSimPanel: () => void
}) {
  return (
    <div className="assets-canvas-page__toolbar">
      <button type="button" onClick={onAddAsset}>
        에셋 추가
      </button>
      {relMode ? (
        <button
          type="button"
          className="assets-canvas-page__toolbar-cancel"
          onClick={onExitRelMode}
        >
          관계 만들기 취소
        </button>
      ) : (
        <button
          type="button"
          onClick={onEnterRelMode}
          disabled={nodeCount < 2 || objectTypePanelOpen}
          title="캔버스에서 에셋을 클릭하여 관계를 만들 수 있습니다"
        >
          관계 만들기
        </button>
      )}
      <button
        type="button"
        className={objectTypePanelOpen ? 'assets-canvas-page__toolbar-active' : ''}
        onClick={onToggleObjectTypePanel}
        disabled={relMode}
      >
        {objectTypePanelOpen ? 'ObjectType 닫기' : 'ObjectType 관리'}
      </button>
      <button
        type="button"
        className={simPanelOpen ? 'assets-canvas-page__toolbar-active' : ''}
        onClick={onToggleSimPanel}
        disabled={relMode || objectTypePanelOpen}
      >
        {simPanelOpen ? '시뮬레이션 닫기' : '시뮬레이션'}
      </button>
      {relMode && (
        <span className="assets-canvas-page__rel-indicator">
          관계 편집 모드 — 캔버스에서 에셋을 클릭하세요
        </span>
      )}
    </div>
  )
}
