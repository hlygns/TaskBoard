import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router'
import { boardsApi, type BoardDetail } from '../api/boards'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { Avatar } from '../components/Avatar'
import { ActivityPanel } from '../components/board/ActivityPanel'
import { ArchivePanel } from '../components/board/ArchivePanel'
import { BoardCanvas } from '../components/board/BoardCanvas'
import { CardModal } from '../components/board/CardModal'
import { buildPredicate, emptyFilter, isFilterActive } from '../components/board/boardFilter'
import { FilterBar } from '../components/board/FilterBar'
import { readLayout, saveLayout, type BoardLayout } from '../components/board/layout'
import { LayoutToggle } from '../components/board/LayoutToggle'
import { BoardForm } from '../components/BoardForm'
import { MembersPanel } from '../components/MembersPanel'
import { Modal } from '../components/Modal'
import { Spinner } from '../components/Spinner'
import { ToastStack } from '../components/Toasts'
import { useToasts } from '../components/useToasts'
import { boardInitial, boardTheme } from '../utils/boardColor'
import { isOverdue } from '../utils/format'
import { useBoardRealtime, type BoardEvent, type BoardEventType } from '../realtime/useBoardRealtime'

const eventMessages: Record<BoardEventType, string> = {
  BoardUpdated: 'pano bilgilerini güncelledi',
  BoardDeleted: 'panoyu sildi',
  MembersChanged: 'üye listesini güncelledi',
  ColumnCreated: 'yeni bir sütun ekledi',
  ColumnUpdated: 'bir sütunu yeniden adlandırdı',
  ColumnMoved: 'bir sütunu taşıdı',
  ColumnDeleted: 'bir sütunu sildi',
  CardCreated: 'yeni bir kart ekledi',
  CardUpdated: 'bir kartı güncelledi',
  CardMoved: 'bir kartı taşıdı',
  CardDeleted: 'bir kartı sildi',
  CommentAdded: 'yorum yazdı',
  CommentDeleted: 'bir yorumu sildi',
}

export function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [board, setBoard] = useState<BoardDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)
  // Sağdaki yan panel: üyeler, aktivite geçmişi ya da arşiv.
  const [panel, setPanel] = useState<'members' | 'activity' | 'archive' | null>(null)
  const [activityRefreshKey, setActivityRefreshKey] = useState(0)
  // "Görevlerim" ekranından gelinirse (?card=...) o kart açık başlar.
  const [searchParams] = useSearchParams()
  const [openCardId, setOpenCardId] = useState<string | null>(searchParams.get('card'))
  const [filter, setFilter] = useState(emptyFilter)
  const [layout, setLayout] = useState<BoardLayout>(readLayout)
  function changeLayout(next: BoardLayout) {
    setLayout(next)
    saveLayout(next)
  }
  const predicate = useMemo(() => (isFilterActive(filter) ? buildPredicate(filter) : undefined), [filter])
  const [cardRefreshKey, setCardRefreshKey] = useState(0)
  const { toasts, show: showToast } = useToasts()

  const load = useCallback(() => {
    boardsApi
      .get(boardId!)
      .then(setBoard)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Pano yüklenemedi.'))
  }, [boardId])

  useEffect(load, [load])

  // Art arda gelen olaylarda (ör. biri hızlıca üç kart taşıdı) panoyu tek seferde yükle.
  const reloadTimer = useRef<ReturnType<typeof setTimeout>>(undefined)
  const scheduleReload = useCallback(() => {
    clearTimeout(reloadTimer.current)
    reloadTimer.current = setTimeout(load, 150)
  }, [load])
  useEffect(() => () => clearTimeout(reloadTimer.current), [])

  function handleBoardEvent(event: BoardEvent, actor: { fullName: string }) {
    showToast(`${actor.fullName} ${eventMessages[event.type]}`)

    if (event.type === 'BoardDeleted') {
      navigate('/boards', { replace: true })
      return
    }

    // Açık olan kart başkası tarafından değiştirildiyse detay penceresini de güncelle.
    if (event.cardId && event.cardId === openCardId) {
      if (event.type === 'CardDeleted') setOpenCardId(null)
      else setCardRefreshKey((k) => k + 1)
    }

    // Olay sadece "ne değişti" der; güncel pano verisini kendi yetkimizle API'den çekiyoruz.
    scheduleReload()
    setActivityRefreshKey((k) => k + 1)
  }

  const { online } = useBoardRealtime(boardId, { onEvent: handleBoardEvent, onReconnected: load })
  const onlineIds = new Set(online.map((u) => u.userId))
  const othersOnline = online.filter((u) => u.userId !== user?.id)

  if (error)
    return (
      <div className="py-24 text-center">
        <p className="text-slate-600">{error}</p>
        <Link to="/boards" className="mt-4 inline-block text-sm font-medium text-indigo-600 hover:underline">
          Panolarıma dön
        </Link>
      </div>
    )
  if (!board || !user) return <Spinner />

  const isOwner = board.myRole === 'Owner'
  const togglePanel = (next: 'members' | 'activity' | 'archive') => setPanel((current) => (current === next ? null : next))
  const theme = boardTheme(board.id)
  const allCards = board.columns.flatMap((c) => c.cards)
  const stats = {
    total: allCards.length,
    completed: allCards.filter((c) => c.isCompleted).length,
    overdue: allCards.filter((c) => !c.isCompleted && c.dueDate !== null && isOverdue(c.dueDate)).length,
  }
  // Çevrimiçi üyeler önde görünsün.
  const sortedMembers = [...board.members].sort((a, b) => Number(onlineIds.has(b.userId)) - Number(onlineIds.has(a.userId)))

  async function handleUpdate(input: { name: string; description: string | null }) {
    await boardsApi.update(board!.id, input)
    setEditing(false)
    load()
  }

  async function handleDelete() {
    if (!confirm(`"${board!.name}" panosu ve içindeki tüm kartlar kalıcı olarak silinsin mi?`)) return
    await boardsApi.remove(board!.id)
    navigate('/boards')
  }

  async function handleLeave() {
    if (!confirm(`"${board!.name}" panosundan ayrılmak istiyor musun?`)) return
    await boardsApi.removeMember(board!.id, user!.id)
    navigate('/boards')
  }

  return (
    <div className="px-4 py-6 sm:px-6 lg:px-10">
      <Link to="/boards" className="text-sm text-slate-500 hover:text-slate-700">
        ← Panolarım
      </Link>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-4">
        <div className="flex min-w-0 items-center gap-4">
          <span
            className={`grid h-14 w-14 shrink-0 place-items-center rounded-2xl bg-gradient-to-br text-2xl font-bold text-white shadow-sm ${theme.gradient}`}
          >
            {boardInitial(board.name)}
          </span>
          <div className="min-w-0">
            <h1 className="truncate text-3xl font-bold tracking-tight text-slate-900">{board.name}</h1>
            <p className="mt-1 text-sm text-slate-500">
              {board.description && <span className="mr-2">{board.description} ·</span>}
              {stats.total} kart · {stats.completed} tamamlandı
              {stats.overdue > 0 && <span className="font-medium text-red-600"> · {stats.overdue} gecikmiş</span>}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {othersOnline.length > 0 && (
            <span className="hidden text-sm text-slate-500 md:inline">
              {othersOnline.length === 1
                ? `${othersOnline[0].fullName} şu an panoda`
                : `${othersOnline.length} kişi şu an panoda`}
            </span>
          )}
          <button
            onClick={() => togglePanel('members')}
            className="flex items-center -space-x-2 rounded-full p-1 hover:bg-slate-100"
            title="Üyeler"
          >
            {sortedMembers.slice(0, 4).map((m) => (
              <Avatar key={m.userId} name={m.fullName} size="md" online={onlineIds.has(m.userId)} />
            ))}
            {board.members.length > 4 && (
              <span className="grid h-9 w-9 place-items-center rounded-full bg-slate-200 text-xs ring-2 ring-white">
                +{board.members.length - 4}
              </span>
            )}
          </button>
          <button onClick={() => togglePanel('members')} className={panelButtonClass(panel === 'members')}>
            {isOwner ? 'Üyeler ve davet' : 'Üyeler'}
          </button>
          <button onClick={() => togglePanel('activity')} className={panelButtonClass(panel === 'activity')}>
            Aktivite
          </button>
          <button onClick={() => togglePanel('archive')} className={panelButtonClass(panel === 'archive')}>
            Arşiv
          </button>
          {isOwner ? (
            <>
              <button onClick={() => setEditing(true)} className="rounded-lg px-3 py-2 text-sm text-slate-600 hover:bg-slate-100">
                Düzenle
              </button>
              <button onClick={handleDelete} className="rounded-lg px-3 py-2 text-sm text-red-600 hover:bg-red-50">
                Sil
              </button>
            </>
          ) : (
            <button onClick={handleLeave} className="rounded-lg px-3 py-2 text-sm text-slate-600 hover:bg-slate-100">
              Panodan ayrıl
            </button>
          )}
        </div>
      </div>

      <div className="mt-5 flex flex-wrap items-start justify-between gap-3">
        <FilterBar filter={filter} labels={board.labels} onChange={setFilter} />
        <LayoutToggle layout={layout} onChange={changeLayout} />
      </div>

      <div className="mt-4 flex flex-col gap-6 lg:flex-row">
        <div className="min-w-0 flex-1">
          <BoardCanvas
            boardId={board.id}
            initialColumns={board.columns}
            labels={board.labels}
            filter={predicate}
            layout={layout}
            onOpenCard={setOpenCardId}
            onReload={load}
          />
        </div>

        {panel && (
          <div className="w-full shrink-0 lg:w-80">
            {panel === 'members' && (
              <MembersPanel board={board} currentUserId={user.id} onlineIds={onlineIds} onChanged={load} />
            )}
            {panel === 'activity' && <ActivityPanel boardId={board.id} refreshKey={activityRefreshKey} />}
            {panel === 'archive' && <ArchivePanel boardId={board.id} refreshKey={activityRefreshKey} onChanged={load} />}
          </div>
        )}
      </div>

      {openCardId && (
        <CardModal
          key={openCardId}
          cardId={openCardId}
          boardId={board.id}
          labels={board.labels}
          members={board.members}
          currentUserId={user.id}
          isOwner={isOwner}
          refreshKey={cardRefreshKey}
          onClose={() => setOpenCardId(null)}
          onChanged={load}
        />
      )}

      {editing && (
        <Modal title="Panoyu düzenle" onClose={() => setEditing(false)}>
          <BoardForm
            initialName={board.name}
            initialDescription={board.description}
            submitLabel="Kaydet"
            onSubmit={handleUpdate}
            onCancel={() => setEditing(false)}
          />
        </Modal>
      )}

      <ToastStack toasts={toasts} />
    </div>
  )
}

function panelButtonClass(active: boolean) {
  return `rounded-lg border px-3.5 py-2 text-sm font-medium ${
    active ? 'border-indigo-300 bg-indigo-50 text-indigo-700' : 'border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
  }`
}
