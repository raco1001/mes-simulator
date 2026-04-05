import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import type { ExtraProperty } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import {
  buildMetadataFromTypeSelection,
  mergeAssetMetadataWithSchema,
  EXTRA_PROPERTIES_KEY,
  getReservedExtraPropertiesRaw,
  isReservedExtraPropertiesKey,
  stripReservedExtraPropertiesKeys,
} from '@/shared/lib/canvasMetadata'

export function emptyExtraProperty(): ExtraProperty {
  return { key: '', dataType: 'String', value: '' }
}

function parseExtraProperties(
  meta: Record<string, unknown> | undefined,
): ExtraProperty[] {
  const raw = meta ? getReservedExtraPropertiesRaw(meta) : undefined
  return Array.isArray(raw) ? (raw as ExtraProperty[]) : []
}

function deriveFormState(
  mergeOnInit: boolean,
  initialType: string,
  initialMetadata: Record<string, unknown>,
  objectTypeSchemas: ObjectTypeSchemaDto[],
): {
  type: string
  metadata: Record<string, unknown>
  extraProperties: ExtraProperty[]
} {
  const metaIn = { ...(initialMetadata ?? {}) } as Record<string, unknown>
  const extraProperties = parseExtraProperties(metaIn)
  const withoutEp = stripReservedExtraPropertiesKeys(metaIn)

  if (mergeOnInit && initialType) {
    const merged = mergeAssetMetadataWithSchema(
      initialType,
      objectTypeSchemas,
      withoutEp,
    )
    return {
      type: initialType,
      metadata: stripReservedExtraPropertiesKeys(merged),
      extraProperties,
    }
  }

  return {
    type: initialType,
    metadata: withoutEp,
    extraProperties,
  }
}

export interface UseAssetMetadataFormOptions {
  objectTypeSchemas: ObjectTypeSchemaDto[]
  resetKey: string
  initialType: string
  initialMetadata: Record<string, unknown>
  /** 편집: 스키마 기본값 병합. 생성: false */
  mergeOnInit: boolean
}

export function useAssetMetadataForm({
  objectTypeSchemas,
  resetKey,
  initialType,
  initialMetadata,
  mergeOnInit,
}: UseAssetMetadataFormOptions) {
  const [type, setType] = useState(initialType)
  const [metadata, setMetadata] = useState<Record<string, unknown>>(() =>
    deriveFormState(
      mergeOnInit,
      initialType,
      initialMetadata,
      objectTypeSchemas,
    ).metadata,
  )
  const [extraProperties, setExtraProperties] = useState<ExtraProperty[]>(
    () =>
      deriveFormState(
        mergeOnInit,
        initialType,
        initialMetadata,
        objectTypeSchemas,
      ).extraProperties,
  )

  const initialMetadataRef = useRef(initialMetadata)
  initialMetadataRef.current = initialMetadata

  useEffect(() => {
    const s = deriveFormState(
      mergeOnInit,
      initialType,
      initialMetadataRef.current,
      objectTypeSchemas,
    )
    setType(s.type)
    setMetadata(s.metadata)
    setExtraProperties(s.extraProperties)
  }, [resetKey, mergeOnInit, initialType, objectTypeSchemas])

  const schema = objectTypeSchemas.find((s) => s.objectType === type) ?? null
  const schemaProps = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const schemaKeySet = useMemo(
    () => new Set(schemaProps.map((p) => p.key)),
    [schemaProps],
  )

  const extraKeys = useMemo(
    () =>
      Object.keys(metadata).filter(
        (k) => !schemaKeySet.has(k) && !isReservedExtraPropertiesKey(k),
      ),
    [metadata, schemaKeySet],
  )

  const handleTypeChange = useCallback(
    (newType: string) => {
      setType(newType)
      setMetadata((prev) => {
        const built = buildMetadataFromTypeSelection(
          newType,
          objectTypeSchemas,
          prev,
        )
        return stripReservedExtraPropertiesKeys(built)
      })
    },
    [objectTypeSchemas],
  )

  const setMetaValue = useCallback((key: string, raw: string) => {
    setMetadata((m) => ({ ...m, [key]: raw }))
  }, [])

  const removeExtraKey = useCallback((key: string) => {
    setMetadata((m) => {
      const next = { ...m }
      delete next[key]
      return next
    })
  }, [])

  const addExtraRow = useCallback(() => {
    const k = `extra_${Date.now()}`
    setMetadata((m) => ({ ...m, [k]: '' }))
  }, [])

  const addExtraProperty = useCallback(() => {
    setExtraProperties((prev) => [...prev, emptyExtraProperty()])
  }, [])

  const updateExtraProperty = useCallback(
    (index: number, patch: Partial<ExtraProperty>) => {
      setExtraProperties((prev) =>
        prev.map((p, i) => (i === index ? { ...p, ...patch } : p)),
      )
    },
    [],
  )

  const removeExtraProperty = useCallback((index: number) => {
    setExtraProperties((prev) => prev.filter((_, i) => i !== index))
  }, [])

  const buildMetadataForSave = useCallback((): Record<string, unknown> => {
    const out = stripReservedExtraPropertiesKeys({ ...metadata })
    if (extraProperties.length > 0) {
      out[EXTRA_PROPERTIES_KEY] = extraProperties
    }
    return out
  }, [metadata, extraProperties])

  return {
    type,
    metadata,
    extraProperties,
    schemaProps,
    schemaKeySet,
    extraKeys,
    handleTypeChange,
    setMetaValue,
    removeExtraKey,
    addExtraRow,
    addExtraProperty,
    updateExtraProperty,
    removeExtraProperty,
    buildMetadataForSave,
  }
}
