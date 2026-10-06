import { Link, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/useAuth'

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between px-4">
          <Link to={user ? '/boards' : '/'} className="flex items-center gap-2 font-semibold text-slate-900">
            <span className="grid h-7 w-7 place-items-center rounded-md bg-indigo-600 text-sm text-white">T</span>
            TaskBoard
          </Link>

          <nav className="flex items-center gap-2 text-sm">
            {user ? (
              <>
                <Link to="/boards" className="rounded-md px-3 py-1.5 text-slate-600 hover:bg-slate-100">
                  Panolarım
                </Link>
                <span className="hidden px-2 text-slate-500 sm:inline">{user.fullName}</span>
                <button
                  onClick={handleLogout}
                  className="rounded-md px-3 py-1.5 text-slate-600 hover:bg-slate-100"
                >
                  Çıkış
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="rounded-md px-3 py-1.5 text-slate-600 hover:bg-slate-100">
                  Giriş yap
                </Link>
                <Link
                  to="/register"
                  className="rounded-md bg-indigo-600 px-3 py-1.5 font-medium text-white hover:bg-indigo-700"
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
