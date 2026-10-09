import { createContext, useContext } from 'react'
import type { Label } from '../../api/boards'
import type { BoardLayout } from './layout'

// Pano ekranının derinlerindeki bileşenlerin (kart yüzü gibi) ihtiyaç duyduğu ortak bilgiler.
// Her katmandan prop olarak geçirmek yerine context ile veriyoruz.
export type BoardView = {
  labels: Map<string, Label>
  // Kart yüzündeki yuvarlak işaretle tamamla / geri al.
  toggleComplete: (cardId: string, completed: boolean) => void
  // Filtre açıkken sürükle-bırak kapalı: gizli kartlar varken "şu sıraya bıraktım" yanlış hesaplanırdı.
  dragDisabled: boolean
  layout: BoardLayout
}

export const BoardViewContext = createContext<BoardView>({
  labels: new Map(),
  toggleComplete: () => {},
  dragDisabled: false,
  layout: 'grid',
})

export const useBoardView = () => useContext(BoardViewContext)
