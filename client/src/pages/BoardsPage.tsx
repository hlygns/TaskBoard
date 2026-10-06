import { useAuth } from '../auth/useAuth'

// Panolar listesi 4. adımda (Pano API) gerçek veriye bağlanacak.
export function BoardsPage() {
  const { user } = useAuth()

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Panolarım</h1>
          <p className="mt-1 text-sm text-slate-500">Hoş geldin, {user?.fullName}.</p>
        </div>
        <button
          disabled
          title="4. adımda eklenecek"
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white opacity-50"
        >
          + Yeni pano
        </button>
      </div>

      <div className="mt-8 rounded-xl border-2 border-dashed border-slate-300 py-16 text-center">
        <p className="font-medium text-slate-700">Henüz bir panon yok</p>
        <p className="mt-1 text-sm text-slate-500">Pano oluşturma bir sonraki adımda gelecek.</p>
      </div>
    </div>
  )
}
