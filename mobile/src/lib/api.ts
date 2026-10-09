import Constants from 'expo-constants'
import { storage } from './storage'
import type { AuthResponse } from './types'

const SERVER_KEY = 'taskboard.server'
const REFRESH_KEY = 'taskboard.refreshToken'

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

// --- Sunucu adresi ---------------------------------------------------------
// Telefon API'ye aynı Wi-Fi'daki bilgisayar üzerinden bağlanır. Expo Go, uygulamayı bilgisayardaki
// geliştirme sunucusundan (Metro) yükler; o bilgisayarın adresini buradan öğrenip API portunu (5009)
// varsayılan yapıyoruz. Kullanıcı giriş ekranından değiştirebilir.
export function defaultServerUrl() {
  if (process.env.EXPO_PUBLIC_API_URL) return process.env.EXPO_PUBLIC_API_URL
  const host = Constants.expoConfig?.hostUri?.split(':')[0]
  return `http://${host ?? 'localhost'}:5009`
}

let serverUrl = defaultServerUrl()

export async function loadServerUrl() {
  serverUrl = (await storage.get(SERVER_KEY)) ?? defaultServerUrl()
  return serverUrl
}

export async function saveServerUrl(url: string) {
  serverUrl = url.trim().replace(/\/+$/, '')
  await storage.set(SERVER_KEY, serverUrl)
}

export const getServerUrl = () => serverUrl

// --- Tokenlar ---------------------------------------------------------------
// Access token sadece bellekte; refresh token telefonun şifreli deposunda.
let accessToken: string | null = null

export async function startSession(auth: AuthResponse) {
  accessToken = auth.accessToken
  await storage.set(REFRESH_KEY, auth.refreshToken)
}

export async function endSession() {
  accessToken = null
  await storage.remove(REFRESH_KEY)
}

// Sunucuda refresh token'ı iptal et (çalınsa bile kullanılamasın), sonra telefondan sil.
export async function logoutSession() {
  const refreshToken = await storage.get(REFRESH_KEY)
  if (refreshToken) await api('/api/auth/logout', { method: 'POST', body: { refreshToken } }).catch(() => {})
  await endSession()
}

// Aynı anda birden fazla istek 401 alırsa tek bir yenileme isteği yapılır; yoksa aynı refresh token
// iki kez kullanılır ve sunucu bunu "çalınmış token" sayıp oturumu kapatır.
let refreshPromise: Promise<AuthResponse | null> | null = null

export function refreshSession(): Promise<AuthResponse | null> {
  refreshPromise ??= (async () => {
    const refreshToken = await storage.get(REFRESH_KEY)
    if (!refreshToken) return null
    try {
      const res = await fetch(`${serverUrl}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Client': 'mobile' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!res.ok) {
        if (res.status === 401) await endSession()
        return null
      }
      const auth = (await res.json()) as AuthResponse
      await startSession(auth)
      return auth
    } catch {
      // Sunucuya ulaşılamadı (bilgisayar kapalı, farklı Wi-Fi): oturumu silme, sonra tekrar denenir.
      return null
    }
  })().finally(() => {
    refreshPromise = null
  })
  return refreshPromise
}

// --- İstek ------------------------------------------------------------------
type Options = { method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'; body?: unknown }

export async function api<T>(path: string, options: Options = {}, retry = true): Promise<T> {
  const headers: Record<string, string> = { 'X-Client': 'mobile' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`

  let res: Response
  try {
    res = await fetch(`${serverUrl}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    })
  } catch {
    throw new ApiError(0, `Sunucuya ulaşılamadı (${serverUrl}). Bilgisayarın açık ve aynı Wi-Fi'da mı?`)
  }

  // Access token süresi dolduysa bir kez sessizce yenile ve isteği tekrarla.
  if (res.status === 401 && retry && !path.startsWith('/api/auth/')) {
    if (await refreshSession()) return api<T>(path, options, false)
  }

  if (!res.ok) throw new ApiError(res.status, await readError(res))
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

async function readError(res: Response) {
  try {
    const problem = await res.json()
    if (problem.detail) return problem.detail as string
    if (problem.errors) {
      const first = Object.values(problem.errors as Record<string, string[]>)[0]
      if (first?.[0]) return first[0]
    }
    return (problem.title as string) ?? 'Bir hata oluştu.'
  } catch {
    return 'Bir hata oluştu.'
  }
}

export const errorMessage = (err: unknown) => (err instanceof ApiError ? err.message : 'Bir hata oluştu.')
