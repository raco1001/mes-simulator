변경 전 변경 후
───────────────────────────── ─────────────────────────────────────
AddAssetModal.tsx AddAssetModal.tsx
├ type state └ useAssetMetadataForm (훅)
├ metadata state └ ExtraPropertiesSection (컴포넌트)
├ extraKeys (flat only)
└ handleTypeChange 복사본

EditAssetOnPanel.tsx EditAssetOnPanel.tsx
├ type state └ useAssetMetadataForm (훅) ← 공유
├ metadata state └ ExtraPropertiesSection (컴포넌트) ← 공유
├ extraProperties state
├ handleTypeChange 복사본
└ ExtraProperty 인터페이스 (로컬)

                                 추가된 파일
                                 ├ shared/lib/useAssetMetadataForm.ts
                                 ├ shared/ui/ExtraPropertiesSection.tsx
                                 └ entities/asset/model/types.ts (ExtraProperty 추가)
