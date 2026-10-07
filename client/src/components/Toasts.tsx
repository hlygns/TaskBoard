import type { Toast } from './useToasts'

export function ToastStack({ toasts }: { toasts: Toast[] }) {
  return (
    <div className="pointer-events-none fixed right-4 bottom-4 z-40 flex flex-col gap-2" aria-live="polite">
      {toasts.map((toast) => (
        <div key={toast.id} className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm text-white shadow-lg">
          {toast.text}
        </div>
      ))}
    </div>
  )
}
