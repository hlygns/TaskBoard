import { createContext, useContext } from 'react'
import type { User } from '../api/client'

export type AuthContextValue = {
  user: User | null
  // Uygulama ilk açıldığında oturum kontrol edilirken true.
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (fullName: string, email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth, AuthProvider içinde kullanılmalı.')
  return context
}
