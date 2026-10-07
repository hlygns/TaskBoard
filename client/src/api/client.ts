// Access token sadece bellekte (bu değişkende) tutulur, localStorage'a yazılmaz.
// Sayfa yenilenince kaybolur; o zaman httpOnly cookie'deki refresh token ile yenisi alınır.
let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function getAccessToken() {
  return accessToken
}

// Açık bir SignalR bağlantısı varsa kimliği her isteğe eklenir; sunucu bu isteğin yol açtığı
// canlı bildirimi bize geri göndermez (ekranımızı zaten kendimiz güncelledik).
let realtimeConnectionId: string | null = null

export function setRealtimeConnectionId(id: string | null) {
  realtimeConnectionId = id
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export type User = {
  id: string
  email: string
  fullName: string
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: User
}

// Aynı anda birden fazla istek 401 alırsa hepsi tek bir refresh isteğini bekler.
// Aksi halde aynı refresh token iki kez kullanılır ve sunucu bunu "çalınmış token" sayıp
// tüm oturumları kapatır (token rotation).
let refreshPromise: Promise<AuthResponse | null> | null = null

export function refreshSession(): Promise<AuthResponse | null> {
  refreshPromise ??= fetch('/api/auth/refresh', { method: 'POST' })
    .then(async (res) => {
      if (!res.ok) {
        setAccessToken(null)
        return null
      }
      const data = (await res.json()) as AuthResponse
      setAccessToken(data.accessToken)
      return data
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
}

export async function api<T>(path: string, options: RequestOptions = {}, retry = true): Promise<T> {
  const headers: Record<string, string> = {}
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`
  if (realtimeConnectionId) headers['X-Connection-Id'] = realtimeConnectionId

  const res = await fetch(path, {
    method: options.method ?? 'GET',
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  })

  // Access token'ın süresi dolmuşsa bir kez sessizce yenileyip isteği tekrarla.
  if (res.status === 401 && retry && !path.startsWith('/api/auth/')) {
    const refreshed = await refreshSession()
    if (refreshed) return api<T>(path, options, false)
  }

  if (!res.ok) throw new ApiError(res.status, await readErrorMessage(res))
  if (res.status === 204) return undefined as T

  return (await res.json()) as T
}

// Sunucu hataları ProblemDetails formatında gelir: { title, detail, errors? }
async function readErrorMessage(res: Response): Promise<string> {
  try {
    const problem = await res.json()
    if (problem.detail) return problem.detail
    if (problem.errors) {
      const first = Object.values(problem.errors as Record<string, string[]>)[0]
      if (first?.[0]) return first[0]
    }
    return problem.title ?? 'Bir hata oluştu.'
  } catch {
    return 'Sunucuya ulaşılamadı.'
  }
}
