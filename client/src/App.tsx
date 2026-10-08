import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { AuthProvider } from './auth/AuthContext'
import { useAuth } from './auth/useAuth'
import { GuestOnly, RequireAuth } from './auth/RequireAuth'
import { Layout } from './components/Layout'
import { BoardPage } from './pages/BoardPage'
import { BoardsPage } from './pages/BoardsPage'
import { HomePage } from './pages/HomePage'
import { InvitationPage } from './pages/InvitationPage'
import { LoginPage } from './pages/LoginPage'
import { MyTasksPage } from './pages/MyTasksPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RegisterPage } from './pages/RegisterPage'

// Giriş yapmış kullanıcı ana sayfa yerine doğrudan panolarını görür.
function Home() {
  const { user, loading } = useAuth()
  if (loading) return null
  return user ? <Navigate to="/boards" replace /> : <HomePage />
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<Home />} />

            <Route element={<GuestOnly />}>
              <Route path="login" element={<LoginPage />} />
              <Route path="register" element={<RegisterPage />} />
            </Route>

            <Route element={<RequireAuth />}>
              <Route path="boards" element={<BoardsPage />} />
              <Route path="boards/:boardId" element={<BoardPage />} />
              <Route path="tasks" element={<MyTasksPage />} />
            </Route>

            {/* Davet linki: giriş yapmış ya da yapmamış herkes açabilir. */}
            <Route path="invitations/:token" element={<InvitationPage />} />

            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
