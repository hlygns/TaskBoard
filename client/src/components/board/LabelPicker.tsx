import { useState, type FormEvent } from 'react'
import { cardsApi, labelsApi, type Label, type LabelColor } from '../../api/boards'
import { ApiError } from '../../api/client'
import { labelColorList, labelColors } from '../../utils/labels'

type LabelPickerProps = {
  boardId: string
  cardId: string
  boardLabels: Label[]
  selectedIds: string[]
  onChange: (selectedIds: string[]) => void
  // Panonun etiket listesi değişti (yeni etiket / silindi): pano yeniden yüklensin.
  onLabelsChanged: () => void
}

// Karttaki etiketler: tıklayınca takılır/çıkar. Panoya yeni etiket de buradan eklenir.
export function LabelPicker({ boardId, cardId, boardLabels, selectedIds, onChange, onLabelsChanged }: LabelPickerProps) {
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [color, setColor] = useState<LabelColor>('sky')
  const [error, setError] = useState<string | null>(null)

  async function save(ids: string[]) {
    onChange(ids)
    await cardsApi.setLabels(cardId, ids)
  }

  function toggle(labelId: string) {
    save(selectedIds.includes(labelId) ? selectedIds.filter((id) => id !== labelId) : [...selectedIds, labelId])
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const label = await labelsApi.create(boardId, name, color)
      await save([...selectedIds, label.id])
      onLabelsChanged()
      setName('')
      setCreating(false)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Etiket eklenemedi.')
    }
  }

  async function handleDelete(label: Label) {
    if (!confirm(`"${label.name}" etiketi panodan silinsin mi? Tüm kartlardan kaldırılır.`)) return
    await labelsApi.remove(label.id)
    onChange(selectedIds.filter((id) => id !== label.id))
    onLabelsChanged()
  }

  return (
    <div>
      <span className="text-sm font-medium text-slate-700">Etiketler</span>
      <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
        {boardLabels.map((label) => {
          const selected = selectedIds.includes(label.id)
          return (
            <span key={label.id} className="group/label relative">
              <button
                type="button"
                onClick={() => toggle(label.id)}
                aria-pressed={selected}
                className={`rounded-full px-2.5 py-1 text-xs font-medium ring-2 transition ${labelColors[label.color].chip} ${
                  selected ? 'ring-slate-700' : 'opacity-50 ring-transparent hover:opacity-100'
                }`}
              >
                {selected && '✓ '}
                {label.name}
              </button>
              <button
                type="button"
                onClick={() => handleDelete(label)}
                title="Etiketi panodan sil"
                className="absolute -top-1.5 -right-1.5 hidden h-4 w-4 place-items-center rounded-full bg-slate-700 text-[9px] text-white group-hover/label:grid"
              >
                ✕
              </button>
            </span>
          )
        })}
        {!creating && (
          <button
            type="button"
            onClick={() => setCreating(true)}
            className="rounded-full border border-dashed border-slate-300 px-2.5 py-1 text-xs text-slate-500 hover:border-slate-400 hover:text-slate-700"
          >
            + Yeni etiket
          </button>
        )}
      </div>

      {creating && (
        <form onSubmit={handleCreate} className="mt-2 flex flex-wrap items-center gap-2 rounded-md bg-slate-50 p-2">
          <input
            autoFocus
            required
            maxLength={40}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Etiket adı"
            className="w-36 rounded-md border border-slate-300 bg-white px-2 py-1 text-sm outline-none focus:border-indigo-500"
          />
          <div className="flex gap-1" role="radiogroup" aria-label="Renk">
            {labelColorList.map((c) => (
              <button
                key={c}
                type="button"
                role="radio"
                aria-checked={color === c}
                title={labelColors[c].name}
                onClick={() => setColor(c)}
                className={`h-5 w-5 rounded-full ${labelColors[c].dot} ${color === c ? 'ring-2 ring-slate-700 ring-offset-1' : ''}`}
              />
            ))}
          </div>
          <button type="submit" className="rounded-md bg-indigo-600 px-2.5 py-1 text-xs font-medium text-white hover:bg-indigo-700">
            Ekle
          </button>
          <button type="button" onClick={() => setCreating(false)} className="text-xs text-slate-500 hover:text-slate-700">
            Vazgeç
          </button>
          {error && <p className="w-full text-xs text-red-700">{error}</p>}
        </form>
      )}
    </div>
  )
}
