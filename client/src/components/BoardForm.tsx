import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'

type BoardFormProps = {
  initialName?: string
  initialDescription?: string | null
  submitLabel: string
  onSubmit: (input: { name: string; description: string | null }) => Promise<void>
  onCancel: () => void
}

// Pano oluşturma ve düzenleme için ortak form.
export function BoardForm({ initialName = '', initialDescription, submitLabel, onSubmit, onCancel }: BoardFormProps) {
  const [name, setName] = useState(initialName)
  const [description, setDescription] = useState(initialDescription ?? '')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await onSubmit({ name, description: description.trim() || null })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Bir hata oluştu.')
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <label className="block">
        <span className="text-sm font-medium text-slate-700">Pano adı</span>
        <input
          autoFocus
          required
          maxLength={100}
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Örn. Web sitesi yenileme"
          className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        />
      </label>
      <label className="block">
        <span className="text-sm font-medium text-slate-700">Açıklama (isteğe bağlı)</span>
        <textarea
          rows={3}
          maxLength={1000}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        />
      </label>

      {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="flex justify-end gap-2">
        <button type="button" onClick={onCancel} className="rounded-md px-4 py-2 text-sm text-slate-600 hover:bg-slate-100">
          Vazgeç
        </button>
        <button
          type="submit"
          disabled={submitting}
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
        >
          {submitting ? 'Kaydediliyor…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
