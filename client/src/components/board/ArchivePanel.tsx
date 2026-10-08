import { useCallback, useEffect, useState } from 'react'
import { boardsApi, cardsApi, type ArchivedCard } from '../../api/boards'
import { timeAgo } from '../../utils/format'

type ArchivePanelProps = {
  boardId: string
  // Pano değiştiğinde (biri kart arşivledi vb.) liste yeniden yüklenir.
  refreshKey: number
  onChanged: () => void
}

// Arşivlenen kartlar: geri alınabilir ya da kalıcı olarak silinebilir.
export function ArchivePanel({ boardId, refreshKey, onChanged }: ArchivePanelProps) {
  const [cards, setCards] = useState<ArchivedCard[] | null>(null)

  const load = useCallback(() => {
    boardsApi.archivedCards(boardId).then(setCards)
  }, [boardId])

  useEffect(load, [load, refreshKey])

  async function restore(cardId: string) {
    await cardsApi.setArchived(cardId, false)
    setCards((list) => list?.filter((c) => c.id !== cardId) ?? null)
    onChanged()
  }

  async function remove(card: ArchivedCard) {
    if (!confirm(`"${card.title}" kartı kalıcı olarak silinsin mi?`)) return
    await cardsApi.remove(card.id)
    setCards((list) => list?.filter((c) => c.id !== card.id) ?? null)
  }

  return (
    <aside className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="font-semibold text-slate-900">Arşiv</h2>
      <p className="mt-1 text-xs text-slate-500">Arşivlenen kartlar panoda görünmez, buradan geri alınabilir.</p>

      {cards === null && <p className="mt-4 text-sm text-slate-400">Yükleniyor…</p>}
      {cards?.length === 0 && <p className="mt-4 text-sm text-slate-400">Arşivde kart yok.</p>}

      <ul className="mt-4 max-h-[60vh] space-y-3 overflow-y-auto pr-1">
        {cards?.map((card) => (
          <li key={card.id} className="rounded-md border border-slate-200 p-3">
            <p className="break-words text-sm text-slate-800">{card.title}</p>
            <p className="mt-0.5 text-xs text-slate-400">
              {card.columnName} · {timeAgo(card.archivedAt)} arşivlendi
            </p>
            <div className="mt-2 flex gap-3 text-xs">
              <button onClick={() => restore(card.id)} className="font-medium text-indigo-600 hover:underline">
                Geri al
              </button>
              <button onClick={() => remove(card)} className="text-slate-400 hover:text-red-600">
                Kalıcı sil
              </button>
            </div>
          </li>
        ))}
      </ul>
    </aside>
  )
}
