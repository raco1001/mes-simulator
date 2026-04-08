import type { AssetDto } from '../model/types'
import { getAssetDisplayTitle } from '@/shared/lib/assetDisplay'

const SHORT_ID = 8

/** 관계 패널·목록용: "표시이름 (id…)" 또는 "타입 (id…)" */
export function getAssetLabel(asset: AssetDto): string {
  const title = getAssetDisplayTitle(asset)
  const id = asset.id
  const short =
    id.length > SHORT_ID ? `${id.slice(0, SHORT_ID)}…` : id
  return `${title} (${short})`
}
