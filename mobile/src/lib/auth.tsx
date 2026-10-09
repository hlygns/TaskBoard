import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, loadServerUrl, logoutSession, refreshSession, startSession } from './api'
import type { AuthResponse, User } from './types'

type AuthState = {
  user: User | null
  // Uygulama açılırken kayıtlı oturum kontrol ediliyor.
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (fullName: string, email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  // Açılışta: kayıtlı sunucu adresini yükle, şifreli depodaki refresh token ile oturumu geri getir.
  useEffect(() => {
    loadServerUrl()
      .then(() => refreshSession())
      .then((session) => setUser(session?.user ?? null))
      .finally(() => setLoading(false))
  }, [])

  async function begin(auth: AuthResponse) {
    await startSession(auth)
    setUser(auth.user)
  }

  const value: AuthState = {
    user,
    loading,
    login: async (email, password) =>
      begin(await api<AuthResponse>('/api/auth/login', { method: 'POST', body: { email, password } })),
    register: async (fullName, email, password) =>
      begin(await api<AuthResponse>('/api/auth/register', { method: 'POST', body: { fullName, email, password } })),
    logout: async () => {
      await logoutSession()
      setUser(null)
    },
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth, AuthProvider içinde kullanılmalı.')
  return context
}
