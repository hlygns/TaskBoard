import { Link, NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { Avatar } from './Avatar'

// Açık olan sayfanın menü bağlantısı vurgulanır.
const navClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3.5 py-2 text-[0.95rem] font-medium transition ${
    isActive ? 'bg-indigo-50 text-indigo-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
  }`

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6 lg:px-10">
          <Link to={user ? '/boards' : '/'} className="flex items-center gap-2.5">
            <span className="grid h-9 w-9 place-items-center rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 text-lg font-bold text-white shadow-sm">
              T
            </span>
            <span className="text-xl font-bold tracking-tight text-slate-900">TaskBoard</span>
          </Link>

          <nav className="flex items-center gap-1 sm:gap-2">
            {user ? (
              <>
                <NavLink to="/tasks" className={navClass}>
                  Görevlerim
                </NavLink>
                <NavLink to="/boards" className={navClass}>
                  Panolarım
                </NavLink>
                <span className="mx-1 hidden h-6 w-px bg-slate-200 sm:block" />
                <span className="hidden items-center gap-2 px-1 sm:flex">
                  <Avatar name={user.fullName} size="sm" />
                  <span className="text-[0.95rem] font-medium text-slate-700">{user.fullName}</span>
                </span>
                <button
                  onClick={handleLogout}
                  className="rounded-lg px-3.5 py-2 text-[0.95rem] text-slate-500 hover:bg-slate-100 hover:text-slate-900"
                >
                  Çıkış
                </button>
              </>
            ) : (
              <>
                <NavLink to="/login" className={navClass}>
                  Giriş yap
                </NavLink>
                <Link
                  to="/register"
                  className="rounded-lg bg-indigo-600 px-4 py-2 text-[0.95rem] font-medium text-white shadow-sm hover:bg-indigo-700"
                >
                  Kayıt ol
                </Link>
              </>
            )}
          </nav>
        </div>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
