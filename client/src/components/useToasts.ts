import { useCallback, useRef, useState } from 'react'

export type Toast = { id: number; text: string }

// Ekranın sağ altında birkaç saniye görünüp kaybolan kısa bildirimler.
export function useToasts(durationMs = 4000) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextId = useRef(0)

  const show = useCallback(
    (text: string) => {
      const id = nextId.current++
      // Aynı anda en fazla 3 bildirim.
      setToasts((list) => [...list.slice(-2), { id, text }])
      setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), durationMs)
    },
    [durationMs],
  )

  return { toasts, show }
}
