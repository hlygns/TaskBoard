import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { boardsApi, type BoardSummary } from '../api/boards'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { BoardForm } from '../components/BoardForm'
import { Modal } from '../components/Modal'
import { Spinner } from '../components/Spinner'

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

  async function handleCreate(input: { name: string; description: string | null }) {
    const board = await boardsApi.create(input)
    navigate(`/boards/${board.id}`)
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Panolarım</h1>
          <p className="mt-1 text-sm text-slate-500">Hoş geldin, {user?.fullName}.</p>
        </div>
        <button
          onClick={() => setCreating(true)}
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
        >
          + Yeni pano
        </button>
      </div>

      {error && <p className="mt-8 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {!boards && !error && <Spinner />}

      {boards?.length === 0 && (
        <div className="mt-8 rounded-xl border-2 border-dashed border-slate-300 py-16 text-center">
          <p className="font-medium text-slate-700">Henüz bir panon yok</p>
          <p className="mt-1 text-sm text-slate-500">İlk panonu oluştur ya da bir davet linkiyle mevcut bir panoya katıl.</p>
          <button
            onClick={() => setCreating(true)}
            className="mt-4 text-sm font-medium text-indigo-600 hover:underline"
          >
            İlk panonu oluştur
          </button>
        </div>
      )}

      {boards && boards.length > 0 && (
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {boards.map((board) => (
            <Link
              key={board.id}
              to={`/boards/${board.id}`}
              className="group rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-indigo-300 hover:shadow-md"
            >
              <div className="flex items-start justify-between gap-2">
                <h2 className="font-semibold text-slate-900 group-hover:text-indigo-700">{board.name}</h2>
                {board.myRole === 'Owner' && (
                  <span className="rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">Sahip</span>
                )}
              </div>
              {board.description && <p className="mt-1 line-clamp-2 text-sm text-slate-500">{board.description}</p>}
              <p className="mt-4 text-xs text-slate-400">{board.memberCount} üye</p>
            </Link>
          ))}
        </div>
      )}

      {creating && (
        <Modal title="Yeni pano" onClose={() => setCreating(false)}>
          <BoardForm submitLabel="Oluştur" onSubmit={handleCreate} onCancel={() => setCreating(false)} />
        </Modal>
      )}
    </div>
  )
}
