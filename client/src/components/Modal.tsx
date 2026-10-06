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
        <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
        <div className="mt-4">{children}</div>
      </div>
    </div>
  )
}
