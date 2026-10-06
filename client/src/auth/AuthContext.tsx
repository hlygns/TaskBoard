import { useEffect, useState, type ReactNode } from 'react'
import { api, refreshSession, setAccessToken, type AuthResponse, type User } from '../api/client'
import { AuthContext } from './useAuth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  // Sayfa yenilendiğinde: cookie'de geçerli refresh token varsa oturumu geri yükle.
  useEffect(() => {
    refreshSession()
      .then((session) => setUser(session?.user ?? null))
      .finally(() => setLoading(false))
  }, [])

  function startSession(response: AuthResponse) {
    setAccessToken(response.accessToken)
    setUser(response.user)
  }

  async function login(email: string, password: string) {
    startSession(await api<AuthResponse>('/api/auth/login', { method: 'POST', body: { email, password } }))
  }

  async function register(fullName: string, email: string, password: string) {
    startSession(
      await api<AuthResponse>('/api/auth/register', { method: 'POST', body: { fullName, email, password } }),
    )
  }

  async function logout() {
    await api('/api/auth/logout', { method: 'POST' })
    setAccessToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout }}>{children}</AuthContext.Provider>
  )
}
