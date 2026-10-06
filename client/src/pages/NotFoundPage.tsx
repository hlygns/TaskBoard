import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="py-24 text-center">
      <p className="text-5xl font-bold text-slate-300">404</p>
      <p className="mt-2 text-slate-600">Aradığın sayfa bulunamadı.</p>
      <Link to="/" className="mt-6 inline-block text-sm font-medium text-indigo-600 hover:underline">
        Ana sayfaya dön
      </Link>
    </div>
  )
}
