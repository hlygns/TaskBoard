import { useEffect, useState, type FormEvent } from 'react'
import { boardsApi, type BoardTemplate } from '../api/boards'
import { ApiError } from '../api/client'
import { labelColors } from '../utils/labels'

type BoardFormProps = {
  initialName?: string
  initialDescription?: string | null
  submitLabel: string
  // Sadece yeni pano oluştururken: hazır sütun/etiket seti seçimi.
  showTemplates?: boolean
  onSubmit: (input: { name: string; description: string | null; template?: string }) => Promise<void>
  onCancel: () => void
}

// Pano oluşturma ve düzenleme için ortak form.
export function BoardForm({
  initialName = '',
  initialDescription,
  submitLabel,
  showTemplates = false,
  onSubmit,
  onCancel,
}: BoardFormProps) {
  const [name, setName] = useState(initialName)
  const [description, setDescription] = useState(initialDescription ?? '')
  const [templates, setTemplates] = useState<BoardTemplate[]>([])
  const [template, setTemplate] = useState('basic')

  useEffect(() => {
    if (showTemplates) boardsApi.templates().then(setTemplates)
  }, [showTemplates])
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await onSubmit({ name, description: description.trim() || null, ...(showTemplates ? { template } : {}) })
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

      {showTemplates && templates.length > 0 && (
        <fieldset>
          <legend className="text-sm font-medium text-slate-700">Şablon</legend>
          <div className="mt-1 grid gap-2 sm:grid-cols-2">
            {templates.map((t) => (
              <label
                key={t.id}
                className={`cursor-pointer rounded-lg border p-3 text-sm ${
                  template === t.id ? 'border-indigo-500 bg-indigo-50 ring-1 ring-indigo-500' : 'border-slate-200 hover:border-slate-300'
                }`}
              >
                <input
                  type="radio"
                  name="template"
                  value={t.id}
                  checked={template === t.id}
                  onChange={() => setTemplate(t.id)}
                  className="sr-only"
                />
                <span className="font-medium text-slate-900">{t.name}</span>
                <span className="mt-0.5 block text-xs text-slate-500">{t.columns.join(' → ')}</span>
                {t.labels.length > 0 && (
                  <span className="mt-1.5 flex flex-wrap gap-1">
                    {t.labels.map((l) => (
                      <span key={l.name} className={`rounded px-1.5 py-0.5 text-[11px] ${labelColors[l.color].chip}`}>
                        {l.name}
                      </span>
                    ))}
                  </span>
                )}
              </label>
            ))}
          </div>
        </fieldset>
      )}

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
