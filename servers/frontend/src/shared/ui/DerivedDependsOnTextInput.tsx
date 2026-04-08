import {
  useEffect,
  useRef,
  useState,
  type InputHTMLAttributes,
} from 'react'

type Props = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  'value' | 'onChange' | 'defaultValue'
> & {
  /** Row / form identity — when it changes, draft resets from canonicalCsv. */
  syncKey: string
  /** Display string derived from saved constraints (array → CSV). */
  canonicalCsv: string
  onCsvChange: (csv: string) => void
}

/**
 * dependsOn CSV 입력: 매 키 입력마다 배열로 파싱해 저장하면 쉼표가 잘려 보이므로
 * 로컬 draft로 원문을 유지한다.
 */
export function DerivedDependsOnTextInput({
  syncKey,
  canonicalCsv,
  onCsvChange,
  ...rest
}: Props) {
  const [draft, setDraft] = useState(canonicalCsv)
  const prevSyncKey = useRef(syncKey)

  useEffect(() => {
    if (prevSyncKey.current !== syncKey) {
      prevSyncKey.current = syncKey
      setDraft(canonicalCsv)
    }
  }, [syncKey, canonicalCsv])

  return (
    <input
      {...rest}
      type="text"
      value={draft}
      onChange={(e) => {
        const v = e.target.value
        setDraft(v)
        onCsvChange(v)
      }}
    />
  )
}
