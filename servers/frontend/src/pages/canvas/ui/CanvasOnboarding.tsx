export function CanvasOnboarding({ onAddFirstAsset }: { onAddFirstAsset: () => void }) {
  return (
    <div className="assets-canvas-page__onboarding">
      <h2>Factory MES에 오신 것을 환영합니다</h2>
      <p>공장 에셋을 추가하고 관계를 연결하여 디지털 트윈을 구성하세요.</p>
      <p>에셋을 추가한 뒤 &quot;관계 설정&quot; 버튼을 누르세요.</p>
      <button type="button" onClick={onAddFirstAsset}>
        첫 에셋 추가하기
      </button>
    </div>
  )
}
