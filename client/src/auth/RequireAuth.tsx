import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './useAuth'
import { Spinner } from '../components/Spinner'

// Giriş gerektiren sayfaları sarar. Giriş yapılmamışsa login'e yönlendirir ve
// girişten sonra kullanıcıyı geldiği sayfaya geri götürmek için adresi hatırlar.
export function RequireAuth() {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return <Spinner />
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />

  return <Outlet />
}

// Giriş yapmış kullanıcı login/kayıt sayfalarını görmesin.
export function GuestOnly() {
  const { user, loading } = useAuth()

  if (loading) return <Spinner />
  if (user) return <Navigate to="/boards" replace />

  return <Outlet />
}
