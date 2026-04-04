export const CANVAS_THEME_STORAGE_KEY = 'canvas-theme'

export type CanvasTheme = 'light' | 'dark'

export function getInitialCanvasTheme(): CanvasTheme {
  if (typeof window === 'undefined') return 'dark'
  const raw = window.localStorage.getItem(CANVAS_THEME_STORAGE_KEY)
  if (raw === 'light' || raw === 'dark') return raw
  if (typeof window.matchMedia === 'function') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light'
  }
  return 'light'
}
