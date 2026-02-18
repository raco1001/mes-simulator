import type { AssetDto, StateDto } from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

/**
 * Backend API 클라이언트
 */
class ApiClient {
  private baseUrl: string

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl
  }

  private async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    })

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('Resource not found')
      }
      throw new Error(`API request failed: ${response.statusText}`)
    }

    return response.json()
  }

  /**
   * 모든 asset 목록 조회
   */
  async getAssets(): Promise<AssetDto[]> {
    return this.request<AssetDto[]>('/api/assets')
  }

  /**
   * 특정 asset 정보 조회
   */
  async getAssetById(id: string): Promise<AssetDto> {
    return this.request<AssetDto>(`/api/assets/${id}`)
  }

  /**
   * 모든 asset의 현재 상태 조회
   */
  async getStates(): Promise<StateDto[]> {
    return this.request<StateDto[]>('/api/states')
  }

  /**
   * 특정 asset의 현재 상태 조회
   */
  async getStateByAssetId(assetId: string): Promise<StateDto> {
    return this.request<StateDto>(`/api/states/${assetId}`)
  }
}

export const apiClient = new ApiClient()
