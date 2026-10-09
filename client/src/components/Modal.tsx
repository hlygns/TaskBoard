import { useEffect, type ReactNode } from 'react'

type ModalProps = {
  title: string
  onClose: () => void
  size?: 'md' | 'lg'
  children: ReactNode
}

export function Modal({ title, onClose, size = 'md', children }: ModalProps) {
  // Esc ile kapat.
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 px-4 py-16"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`w-full rounded-xl bg-white p-6 shadow-xl ${size === 'lg' ? 'max-w-2xl' : 'max-w-md'}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4">
          <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
          {/* Dışarı tıklamak ve Esc de kapatır; düğme bunu bilmeyenler için görünür bir yol. */}
          <button
            type="button"
            onClick={onClose}
            aria-label="Kapat"
            title="Kapat (Esc)"
            className="-mt-1 -mr-2 grid h-8 w-8 shrink-0 place-items-center rounded-md text-xl leading-none text-slate-400 hover:bg-slate-100 hover:text-slate-700"
          >
            ×
          </button>
        </div>
        <div className="mt-4">{children}</div>
      </div>
    </div>
  )
}
