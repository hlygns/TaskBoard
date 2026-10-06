import { SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { useState, type FormEvent } from 'react'
import { cardsApi, columnsApi, type CardSummary, type Column } from '../../api/boards'
import { ApiError } from '../../api/client'
import { CardItem } from './CardItem'

type ColumnViewProps = {
  column: Column
  onOpenCard: (cardId: string) => void
  onCardAdded: (columnId: string, card: CardSummary) => void
  onRenamed: (columnId: string, name: string) => void
  onDeleted: (columnId: string) => void
}

export function ColumnView({ column, onOpenCard, onCardAdded, onRenamed, onDeleted }: ColumnViewProps) {
  // Sütunun kendisi de sürüklenebilir; sadece başlığından tutulur (listeners başlıkta).
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: column.id,
    data: { type: 'column' },
  })

  const [renaming, setRenaming] = useState(false)
  const [name, setName] = useState(column.name)
  const [adding, setAdding] = useState(false)
  const [title, setTitle] = useState('')

  async function saveName() {
    setRenaming(false)
    const trimmed = name.trim()
    if (!trimmed || trimmed === column.name) {
      setName(column.name)
      return
    }
    await columnsApi.rename(column.id, trimmed)
    onRenamed(column.id, trimmed)
  }

  async function handleDelete() {
    const warning = column.cards.length > 0 ? ` İçindeki ${column.cards.length} kart da silinecek.` : ''
    if (!confirm(`"${column.name}" sütunu silinsin mi?${warning}`)) return
    await columnsApi.remove(column.id)
    onDeleted(column.id)
  }

  async function handleAddCard(e: FormEvent) {
    e.preventDefault()
    const trimmed = title.trim()
    if (!trimmed) return
    try {
      const card = await cardsApi.create(column.id, trimmed)
      onCardAdded(column.id, card)
      setTitle('')
    } catch (err) {
      alert(err instanceof ApiError ? err.message : 'Kart eklenemedi.')
    }
  }

  return (
    <section
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={`flex max-h-full w-72 shrink-0 flex-col rounded-lg bg-slate-100 ${isDragging ? 'opacity-40' : ''}`}
    >
      <header
        {...attributes}
        {...listeners}
        className="group flex cursor-grab items-center gap-2 px-3 pt-3 pb-2 active:cursor-grabbing"
      >
        {renaming ? (
          <input
            autoFocus
            value={name}
            maxLength={100}
            onChange={(e) => setName(e.target.value)}
            onBlur={saveName}
            onKeyDown={(e) => {
              if (e.key === 'Enter') e.currentTarget.blur()
              if (e.key === 'Escape') {
                setName(column.name)
                setRenaming(false)
              }
            }}
            // Input'a tıklamak sütun sürüklemeyi başlatmasın.
            onPointerDown={(e) => e.stopPropagation()}
            className="min-w-0 flex-1 rounded border border-indigo-400 bg-white px-1.5 py-0.5 text-sm font-semibold outline-none"
          />
        ) : (
          <h2
            onClick={() => setRenaming(true)}
            title="Yeniden adlandırmak için tıkla"
            className="min-w-0 flex-1 cursor-text truncate text-sm font-semibold text-slate-700"
          >
            {column.name} <span className="font-normal text-slate-400">{column.cards.length}</span>
          </h2>
        )}
        <button
          onClick={handleDelete}
          onPointerDown={(e) => e.stopPropagation()}
          title="Sütunu sil"
          className="rounded px-1 text-slate-400 opacity-0 group-hover:opacity-100 hover:bg-slate-200 hover:text-red-600"
        >
          ✕
        </button>
      </header>

      <SortableContext items={column.cards.map((c) => c.id)} strategy={verticalListSortingStrategy}>
        <div className="flex min-h-10 flex-col gap-2 overflow-y-auto px-3 pb-2">
          {column.cards.map((card) => (
            <CardItem key={card.id} card={card} columnId={column.id} onOpen={onOpenCard} />
          ))}
        </div>
      </SortableContext>

      <div className="px-3 pb-3">
        {adding ? (
          <form onSubmit={handleAddCard}>
            <textarea
              autoFocus
              rows={2}
              maxLength={200}
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault()
                  e.currentTarget.form?.requestSubmit()
                }
                if (e.key === 'Escape') setAdding(false)
              }}
              placeholder="Kart başlığı…"
              className="block w-full resize-none rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm outline-none focus:border-indigo-500"
            />
            <div className="mt-2 flex gap-2">
              <button type="submit" className="rounded-md bg-indigo-600 px-3 py-1 text-sm font-medium text-white hover:bg-indigo-700">
                Ekle
              </button>
              <button type="button" onClick={() => setAdding(false)} className="px-2 text-sm text-slate-500 hover:text-slate-700">
                Vazgeç
              </button>
            </div>
          </form>
        ) : (
          <button
            onClick={() => setAdding(true)}
            className="w-full rounded-md px-2 py-1.5 text-left text-sm text-slate-500 hover:bg-slate-200 hover:text-slate-700"
          >
            + Kart ekle
          </button>
        )}
      </div>
    </section>
  )
}
