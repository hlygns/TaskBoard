import type { FormEvent, ReactNode } from 'react'

type AuthFormProps = {
  title: string
  subtitle: string
  submitLabel: string
  error: string | null
  submitting: boolean
  onSubmit: () => void
  footer: ReactNode
  children: ReactNode
}

// Giriş ve kayıt sayfalarının ortak kalıbı.
export function AuthForm({ title, subtitle, submitLabel, error, submitting, onSubmit, footer, children }: AuthFormProps) {
  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    onSubmit()
  }

  return (
    <div className="mx-auto max-w-sm px-4 py-16">
      <h1 className="text-2xl font-semibold text-slate-900">{title}</h1>
      <p className="mt-1 text-sm text-slate-500">{subtitle}</p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-4">
        {children}

        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="w-full rounded-md bg-indigo-600 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
        >
          {submitting ? 'Lütfen bekleyin…' : submitLabel}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-slate-500">{footer}</p>
    </div>
  )
}

type FieldProps = {
  label: string
  type?: string
  value: string
  onChange: (value: string) => void
  autoComplete?: string
  minLength?: number
}

export function Field({ label, type = 'text', value, onChange, autoComplete, minLength }: FieldProps) {
  return (
    <label className="block">
      <span className="text-sm font-medium text-slate-700">{label}</span>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        autoComplete={autoComplete}
        minLength={minLength}
        required
        className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
      />
    </label>
  )
}
