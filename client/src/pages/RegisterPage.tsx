import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { AuthForm, Field } from '../components/AuthForm'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit() {
    setError(null)
    setSubmitting(true)
    try {
      await register(fullName, email, password)
      // Davet linkinden geldiyse davete geri dön.
      navigate(location.state?.from ?? '/boards', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Bir hata oluştu.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthForm
      title="Hesap oluştur"
      subtitle="Birkaç saniyede ilk panonu oluşturmaya hazır ol."
      submitLabel="Kayıt ol"
      error={error}
      submitting={submitting}
      onSubmit={handleSubmit}
      footer={
        <>
          Zaten hesabın var mı?{' '}
          <Link to="/login" className="font-medium text-indigo-600 hover:underline">
            Giriş yap
          </Link>
        </>
      }
    >
      <Field label="Ad soyad" value={fullName} onChange={setFullName} autoComplete="name" />
      <Field label="E-posta" type="email" value={email} onChange={setEmail} autoComplete="email" />
      <Field
        label="Şifre (en az 8 karakter)"
        type="password"
        value={password}
        onChange={setPassword}
        autoComplete="new-password"
        minLength={8}
      />
    </AuthForm>
  )
}
