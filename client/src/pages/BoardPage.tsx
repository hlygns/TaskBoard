import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { boardsApi, type BoardDetail } from '../api/boards'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { Avatar } from '../components/Avatar'
import { BoardCanvas } from '../components/board/BoardCanvas'
import { CardModal } from '../components/board/CardModal'
import { BoardForm } from '../components/BoardForm'
import { MembersPanel } from '../components/MembersPanel'
import { Modal } from '../components/Modal'
import { Spinner } from '../components/Spinner'

export function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [board, setBoard] = useState<BoardDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)
  const [showMembers, setShowMembers] = useState(false)
  const [openCardId, setOpenCardId] = useState<string | null>(null)

  const load = useCallback(() => {
    boardsApi
      .get(boardId!)
      .then(setBoard)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Pano yüklenemedi.'))
  }, [boardId])

  useEffect(load, [load])

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
    <div className="px-4 py-6 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          <Link to="/boards" className="text-sm text-slate-500 hover:text-slate-700">
            ← Panolarım
          </Link>
          <h1 className="mt-1 text-2xl font-semibold text-slate-900">{board.name}</h1>
          {board.description && <p className="mt-1 text-sm text-slate-500">{board.description}</p>}
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => setShowMembers((v) => !v)}
            className="flex items-center -space-x-2 rounded-full p-1 hover:bg-slate-100"
            title="Üyeler"
          >
            {board.members.slice(0, 4).map((m) => (
              <Avatar key={m.userId} name={m.fullName} size="sm" />
            ))}
            {board.members.length > 4 && (
              <span className="grid h-7 w-7 place-items-center rounded-full bg-slate-200 text-xs ring-2 ring-white">
                +{board.members.length - 4}
              </span>
            )}
          </button>
          <button
            onClick={() => setShowMembers((v) => !v)}
            className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
          >
            {isOwner ? 'Üyeler ve davet' : 'Üyeler'}
          </button>
          {isOwner ? (
            <>
              <button onClick={() => setEditing(true)} className="rounded-md px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100">
                Düzenle
              </button>
              <button onClick={handleDelete} className="rounded-md px-3 py-1.5 text-sm text-red-600 hover:bg-red-50">
                Sil
              </button>
            </>
          ) : (
            <button onClick={handleLeave} className="rounded-md px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100">
              Panodan ayrıl
            </button>
          )}
        </div>
      </div>

      <div className="mt-6 flex flex-col gap-6 lg:flex-row">
        <div className="min-w-0 flex-1">
          <BoardCanvas boardId={board.id} initialColumns={board.columns} onOpenCard={setOpenCardId} onReload={load} />
        </div>

        {showMembers && (
          <div className="w-full shrink-0 lg:w-80">
            <MembersPanel board={board} currentUserId={user.id} onChanged={load} />
          </div>
        )}
      </div>

      {openCardId && (
        <CardModal
          key={openCardId}
          cardId={openCardId}
          members={board.members}
          currentUserId={user.id}
          isOwner={isOwner}
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
    </div>
  )
}
