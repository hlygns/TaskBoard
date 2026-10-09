import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { boardsApi, type BoardSummary } from '../api/boards'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { BoardForm } from '../components/BoardForm'
import { Modal } from '../components/Modal'
import { Spinner } from '../components/Spinner'
import { boardInitial, boardTheme } from '../utils/boardColor'

export function BoardsPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [boards, setBoards] = useState<BoardSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)

  useEffect(() => {
    boardsApi
      .list()
      .then(setBoards)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Panolar yüklenemedi.'))
  }, [])

  async function handleCreate(input: { name: string; description: string | null; template?: string }) {
    const board = await boardsApi.create(input)
    navigate(`/boards/${board.id}`)
  }

  const firstName = user?.fullName.split(' ')[0]

  return (
    <div className="px-4 py-8 sm:px-6 lg:px-10 lg:py-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-indigo-600">Hoş geldin, {firstName} 👋</p>
          <h1 className="mt-1 text-3xl font-bold tracking-tight text-slate-900">Panolarım</h1>
        </div>
        <button
          onClick={() => setCreating(true)}
          className="rounded-lg bg-indigo-600 px-5 py-2.5 font-medium text-white shadow-sm hover:bg-indigo-700"
        >
          + Yeni pano
        </button>
      </div>

      {error && <p className="mt-8 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {!boards && !error && <Spinner />}

      {boards?.length === 0 && (
        <div className="mt-10 rounded-2xl border-2 border-dashed border-slate-300 py-20 text-center">
          <p className="text-lg font-semibold text-slate-700">Henüz bir panon yok</p>
          <p className="mt-1 text-slate-500">İlk panonu oluştur ya da bir davet linkiyle mevcut bir panoya katıl.</p>
          <button
            onClick={() => setCreating(true)}
            className="mt-5 rounded-lg bg-indigo-600 px-5 py-2.5 font-medium text-white hover:bg-indigo-700"
          >
            İlk panonu oluştur
          </button>
        </div>
      )}

      {boards && boards.length > 0 && (
        <div className="mt-8 grid gap-5 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
          {boards.map((board) => (
            <BoardCard key={board.id} board={board} />
          ))}
          <button
            onClick={() => setCreating(true)}
            className="flex min-h-48 flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-slate-300 text-slate-500 transition hover:border-indigo-400 hover:bg-indigo-50/40 hover:text-indigo-700"
          >
            <span className="text-3xl leading-none">+</span>
            <span className="font-medium">Yeni pano</span>
          </button>
        </div>
      )}

      {creating && (
        <Modal title="Yeni pano" size="lg" onClose={() => setCreating(false)}>
          <BoardForm submitLabel="Oluştur" showTemplates onSubmit={handleCreate} onCancel={() => setCreating(false)} />
        </Modal>
      )}
    </div>
  )
}

function BoardCard({ board }: { board: BoardSummary }) {
  const theme = boardTheme(board.id)
  const percent = board.cardCount === 0 ? 0 : Math.round((board.completedCount / board.cardCount) * 100)

  return (
    <Link
      to={`/boards/${board.id}`}
      className="group flex min-h-48 flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm transition hover:-translate-y-0.5 hover:shadow-lg"
    >
      {/* Panonun renk şeridi ve baş harfi */}
      <div className={`relative h-20 bg-gradient-to-br ${theme.gradient}`}>
        <span className="absolute -bottom-6 left-5 grid h-12 w-12 place-items-center rounded-xl bg-white text-xl font-bold text-slate-800 shadow-md ring-4 ring-white">
          {boardInitial(board.name)}
        </span>
        <span className="absolute top-3 right-3 rounded-full bg-white/90 px-2.5 py-0.5 text-xs font-medium text-slate-700">
          {board.myRole === 'Owner' ? 'Sahip' : 'Üye'}
        </span>
      </div>

      <div className="flex flex-1 flex-col px-5 pt-9 pb-5">
        <h2 className="text-lg font-semibold text-slate-900 group-hover:text-indigo-700">{board.name}</h2>
        {board.description && <p className="mt-1 line-clamp-2 text-sm text-slate-500">{board.description}</p>}

        <div className="mt-auto pt-5">
          <div className="flex items-baseline justify-between text-sm">
            <span className="text-slate-600">
              {board.cardCount === 0 ? 'Henüz kart yok' : `${board.completedCount}/${board.cardCount} tamamlandı`}
            </span>
            {board.cardCount > 0 && <span className="font-medium text-slate-700">%{percent}</span>}
          </div>
          <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-100">
            <div className={`h-full rounded-full ${theme.bar}`} style={{ width: `${percent}%` }} />
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-2 text-xs">
            <span className="rounded-full bg-slate-100 px-2.5 py-1 text-slate-600">👥 {board.memberCount} üye</span>
            {board.overdueCount > 0 && (
              <span className="rounded-full bg-red-50 px-2.5 py-1 font-medium text-red-700">⏰ {board.overdueCount} gecikmiş</span>
            )}
          </div>
        </div>
      </div>
    </Link>
  )
}
