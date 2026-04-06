import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import type { ExtraProperty } from '@/entities/asset'
import type {
  Mutability,
  ObjectTypeSchemaDto,
  SimulationBehavior,
} from '@/entities/object-type-schema'
import {
  buildMetadataFromTypeSelection,
  mergeAssetMetadataWithSchema,
  EXTRA_PROPERTIES_KEY,
  applyAssetNameToMetadata,
  isHiddenFromFlatMetadataKeys,
  stripReservedExtraPropertiesKeys,
  parseExtraPropertiesFromMetadata,
} from '@/shared/lib/canvasMetadata'
import {
  normalizeExtraProperty,
  sanitizeExtraPropertyForSave,
} from '@/shared/lib/extraPropertiesCore'

const DEFAULT_SIM_BEHAVIOR: SimulationBehavior = 'Settable'
const DEFAULT_MUTABILITY: Mutability = 'Mutable'

export { normalizeExtraProperty }

export function emptyExtraProperty(): ExtraProperty {
  return {
    key: '',
    dataType: 'String',
    value: '',
    simulationBehavior: DEFAULT_SIM_BEHAVIOR,
    mutability: DEFAULT_MUTABILITY,
  }
}

function parseExtraProperties(
  meta: Record<string, unknown> | undefined,
): ExtraProperty[] {
  return parseExtraPropertiesFromMetadata(meta)
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
  const withoutEp = applyAssetNameToMetadata(
    stripReservedExtraPropertiesKeys(metaIn),
  )

  if (mergeOnInit && initialType) {
    const merged = mergeAssetMetadataWithSchema(
      initialType,
      objectTypeSchemas,
      withoutEp,
    )
    return {
      type: initialType,
      metadata: merged,
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
        (k) => !schemaKeySet.has(k) && !isHiddenFromFlatMetadataKeys(k),
      ),
    [metadata, schemaKeySet],
  )

  const handleTypeChange = useCallback(
    (newType: string) => {
      setType(newType)
      setMetadata((prev) =>
        buildMetadataFromTypeSelection(newType, objectTypeSchemas, prev),
      )
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
    const out = applyAssetNameToMetadata(
      stripReservedExtraPropertiesKeys({ ...metadata }),
    )
    if (extraProperties.length > 0) {
      out[EXTRA_PROPERTIES_KEY] = extraProperties.map(sanitizeExtraPropertyForSave)
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
