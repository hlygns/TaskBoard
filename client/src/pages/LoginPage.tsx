import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { AuthForm, Field } from '../components/AuthForm'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit() {
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      // Korumalı bir sayfadan yönlendirildiyse oraya geri dön.
      navigate(location.state?.from ?? '/boards', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Bir hata oluştu.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthForm
      title="Giriş yap"
      subtitle="Panolarına kaldığın yerden devam et."
      submitLabel="Giriş yap"
      error={error}
      submitting={submitting}
      onSubmit={handleSubmit}
      footer={
        <>
          Hesabın yok mu?{' '}
          <Link to="/register" className="font-medium text-indigo-600 hover:underline">
            Kayıt ol
          </Link>
        </>
      }
    >
      <Field label="E-posta" type="email" value={email} onChange={setEmail} autoComplete="email" />
      <Field label="Şifre" type="password" value={password} onChange={setPassword} autoComplete="current-password" />
    </AuthForm>
  )
}
