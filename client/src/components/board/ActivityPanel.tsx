import { useCallback, useEffect, useRef, useState } from 'react'
import { activitiesApi, type Activity } from '../../api/boards'
import { onApiMutation } from '../../api/client'
import { timeAgo } from '../../utils/format'
import { Avatar } from '../Avatar'

const PAGE_SIZE = 30

// Kayıttaki anlık adlardan Türkçe cümle kurar. Kart sonradan silinse bile cümle doğru kalır.
function describe(a: Activity): string {
  const d = a.data
  switch (a.type) {
    case 'BoardCreated':
      return 'panoyu oluşturdu'
    case 'BoardUpdated':
      return 'pano bilgilerini güncelledi'
    case 'MemberInvited':
      return `${d.email} adresini davet etti`
    case 'MemberJoined':
      return 'panoya katıldı'
    case 'MemberRemoved':
      return `${d.memberName} kişisini panodan çıkardı`
    case 'MemberLeft':
      return 'panodan ayrıldı'
    case 'ColumnCreated':
      return `"${d.columnName}" sütununu ekledi`
    case 'ColumnRenamed':
      return `"${d.oldName}" sütununun adını "${d.newName}" yaptı`
    case 'ColumnMoved':
      return `"${d.columnName}" sütununu taşıdı`
    case 'ColumnDeleted':
      return `"${d.columnName}" sütununu sildi${d.cardCount && d.cardCount !== '0' ? ` (${d.cardCount} kartla)` : ''}`
    case 'CardCreated':
      return `"${d.cardTitle}" kartını ekledi (${d.columnName})`
    case 'CardUpdated':
      return `"${d.cardTitle}" kartını güncelledi`
    case 'CardMoved':
      return `"${d.cardTitle}" kartını taşıdı: ${d.fromColumn} → ${d.toColumn}`
    case 'CardAssigned':
      return d.assigneeName
        ? `"${d.cardTitle}" kartını ${d.assigneeName} kişisine atadı`
        : `"${d.cardTitle}" kartının atamasını kaldırdı`
    case 'CardDeleted':
      return `"${d.cardTitle}" kartını sildi`
    case 'CommentAdded':
      return `"${d.cardTitle}" kartına yorum yazdı: “${d.excerpt}”`
  }
}

type ActivityPanelProps = {
  boardId: string
  // Başkasından canlı bir olay geldiğinde artar; liste baştan yüklenir.
  refreshKey: number
}

export function ActivityPanel({ boardId, refreshKey }: ActivityPanelProps) {
  const [items, setItems] = useState<Activity[] | null>(null)
  const [hasMore, setHasMore] = useState(false)
  const [loadingMore, setLoadingMore] = useState(false)

  const loadFirstPage = useCallback(() => {
    activitiesApi.list(boardId).then((page) => {
      setItems(page)
      setHasMore(page.length === PAGE_SIZE)
    })
  }, [boardId])

  useEffect(loadFirstPage, [loadFirstPage, refreshKey])

  // Kendi değişikliklerimiz: istek bittikten kısa süre sonra listeyi tazele (art arda gelenleri birleştir).
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined)
  useEffect(() => {
    const unsubscribe = onApiMutation(() => {
      clearTimeout(timer.current)
      timer.current = setTimeout(loadFirstPage, 300)
    })
    return () => {
      unsubscribe()
      clearTimeout(timer.current)
    }
  }, [loadFirstPage])

  async function loadMore() {
    if (!items?.length) return
    setLoadingMore(true)
    try {
      const page = await activitiesApi.list(boardId, items[items.length - 1].createdAt)
      setItems([...items, ...page])
      setHasMore(page.length === PAGE_SIZE)
    } finally {
      setLoadingMore(false)
    }
  }

  return (
    <aside className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="font-semibold text-slate-900">Aktivite</h2>

      {items === null && <p className="mt-4 text-sm text-slate-400">Yükleniyor…</p>}
      {items?.length === 0 && <p className="mt-4 text-sm text-slate-400">Henüz bir hareket yok.</p>}

      <ol className="mt-4 max-h-[60vh] space-y-4 overflow-y-auto pr-1">
        {items?.map((activity) => (
          <li key={activity.id} className="flex gap-3">
            <Avatar name={activity.actor.fullName} size="sm" />
            <div className="min-w-0 text-sm">
              <p className="break-words text-slate-700">
                <span className="font-medium text-slate-900">{activity.actor.fullName}</span> {describe(activity)}
              </p>
              <p className="mt-0.5 text-xs text-slate-400" title={new Date(activity.createdAt).toLocaleString('tr-TR')}>
                {timeAgo(activity.createdAt)}
              </p>
            </div>
          </li>
        ))}
      </ol>

      {hasMore && (
        <button
          onClick={loadMore}
          disabled={loadingMore}
          className="mt-4 w-full rounded-md py-1.5 text-sm text-slate-500 hover:bg-slate-100 disabled:opacity-60"
        >
          {loadingMore ? 'Yükleniyor…' : 'Daha eski hareketler'}
        </button>
      )}
    </aside>
  )
}
