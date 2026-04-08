export type {
  AssetDto,
  CreateAssetRequest,
  ExtraProperty,
  UpdateAssetRequest,
} from './model/types'
export {
  getAssets,
  getAssetById,
  createAsset,
  updateAsset,
  deleteAsset,
} from './api/assetApi'
export { getAssetLabel } from './lib/getAssetLabel'
