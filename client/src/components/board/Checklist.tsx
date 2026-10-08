import { useState, type FormEvent } from 'react'
import { cardsApi, type ChecklistItem } from '../../api/boards'

type ChecklistProps = {
  cardId: string
  items: ChecklistItem[]
  onChange: (items: ChecklistItem[]) => void
}

// Kart içindeki alt görevler: işaretle, ekle, sil. Kart yüzünde "2/5" olarak görünür.
export function Checklist({ cardId, items, onChange }: ChecklistProps) {
  const [text, setText] = useState('')
  const done = items.filter((i) => i.isDone).length
  const percent = items.length === 0 ? 0 : Math.round((done / items.length) * 100)

  async function handleAdd(e: FormEvent) {
    e.preventDefault()
    if (!text.trim()) return
    const item = await cardsApi.addChecklistItem(cardId, text)
    onChange([...items, item])
    setText('')
  }

  async function toggle(item: ChecklistItem) {
    // İyimser: önce ekranda işaretle, sonra sunucuya yaz.
    onChange(items.map((i) => (i.id === item.id ? { ...i, isDone: !i.isDone } : i)))
    await cardsApi.updateChecklistItem(item.id, { isDone: !item.isDone })
  }

  async function remove(itemId: string) {
    onChange(items.filter((i) => i.id !== itemId))
    await cardsApi.removeChecklistItem(itemId)
  }

  return (
    <section className="mt-8 border-t border-slate-100 pt-6">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-900">Alt görevler</h3>
        {items.length > 0 && (
          <span className="text-xs text-slate-500">
            {done}/{items.length} · %{percent}
          </span>
        )}
      </div>

      {items.length > 0 && (
        <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-slate-100">
          <div
            className={`h-full rounded-full transition-all ${percent === 100 ? 'bg-emerald-500' : 'bg-indigo-500'}`}
            style={{ width: `${percent}%` }}
          />
        </div>
      )}

      <ul className="mt-3 space-y-1">
        {items.map((item) => (
          <li key={item.id} className="group/item flex items-center gap-2 rounded px-1 py-1 hover:bg-slate-50">
            <input
              type="checkbox"
              checked={item.isDone}
              onChange={() => toggle(item)}
              aria-label={item.text}
              className="h-4 w-4 accent-indigo-600"
            />
            <span className={`flex-1 break-words text-sm ${item.isDone ? 'text-slate-400 line-through' : 'text-slate-700'}`}>
              {item.text}
            </span>
            <button
              type="button"
              onClick={() => remove(item.id)}
              title="Sil"
              className="text-xs text-slate-300 opacity-0 group-hover/item:opacity-100 hover:text-red-600"
            >
              ✕
            </button>
          </li>
        ))}
      </ul>

      <form onSubmit={handleAdd} className="mt-2 flex gap-2">
        <input
          value={text}
          maxLength={300}
          onChange={(e) => setText(e.target.value)}
          placeholder="Alt görev ekle…"
          className="min-w-0 flex-1 rounded-md border border-slate-300 px-3 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        />
        <button
          type="submit"
          disabled={!text.trim()}
          className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-900 disabled:opacity-50"
        >
          Ekle
        </button>
      </form>
    </section>
  )
}
