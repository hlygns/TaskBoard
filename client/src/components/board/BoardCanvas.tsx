import {
  closestCorners,
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragOverEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { arrayMove, horizontalListSortingStrategy, SortableContext, sortableKeyboardCoordinates } from '@dnd-kit/sortable'
import { useRef, useState, type FormEvent } from 'react'
import { cardsApi, columnsApi, type CardSummary, type Column } from '../../api/boards'
import { CardFace } from './CardItem'
import { ColumnView } from './ColumnView'

type BoardCanvasProps = {
  boardId: string
  initialColumns: Column[]
  onOpenCard: (cardId: string) => void
  // Sunucuyla aramız bozulursa (ör. taşıma isteği hata verirse) panoyu baştan yükle.
  onReload: () => void
}

// Sürükle-bırak akışı:
//  1. onDragStart: hangi kart/sütun tutuldu, nereden alındı → kaydet
//  2. onDragOver : kart başka bir sütunun üstüne gelince ekranda hemen o sütuna geçir (canlı önizleme)
//  3. onDragEnd  : son konumu hesapla, ekranı güncelle (iyimser/optimistic), sonra sunucuya bildir
export function BoardCanvas({ boardId, initialColumns, onOpenCard, onReload }: BoardCanvasProps) {
  const [columns, setColumns] = useState(initialColumns)
  const [activeCard, setActiveCard] = useState<CardSummary | null>(null)
  const [activeColumn, setActiveColumn] = useState<Column | null>(null)
  const dragOrigin = useRef<{ columnId: string; index: number } | null>(null)
  // Esc ile iptal edilirse panoyu sürükleme başlamadan önceki haline döndürmek için.
  const snapshot = useRef<Column[]>(initialColumns)

  // Pano yeniden yüklendiğinde (kart detayı kaydedildi vb.) yerel durumu sunucudan gelenle değiştir.
  // Effect yerine render sırasında karşılaştırıyoruz; böylece ekstra bir render turu olmaz.
  //
  // Sürükleme sırasında başka birinin değişikliği gelirse uygulamıyoruz: elimizdeki kart kayardı.
  // Bırakınca, kendi taşımamız sunucuya yazıldıktan sonra pano baştan yüklenir (needsReload).
  const [syncedFrom, setSyncedFrom] = useState(initialColumns)
  const [needsReload, setNeedsReload] = useState(false)
  const dragging = activeCard !== null || activeColumn !== null
  if (initialColumns !== syncedFrom) {
    setSyncedFrom(initialColumns)
    if (dragging) setNeedsReload(true)
    else setColumns(initialColumns)
  }

  // Taşıma isteği bittikten sonra, sürükleme sırasında kaçırılan değişiklikler varsa panoyu tazele.
  function afterDrop(request?: Promise<unknown>) {
    const reload = needsReload
    setNeedsReload(false)
    Promise.resolve(request)
      .catch(() => onReload())
      .then(() => reload && onReload())
  }

  const sensors = useSensors(
    // 5px hareket etmeden sürükleme başlamaz; böylece karta tıklamak detay penceresini açar.
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    // Klavyeyle de taşınabilir: Tab ile karta gel, Space ile tut, oklarla taşı, Space ile bırak.
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  function findColumnOfCard(cardId: string, list = columns) {
    return list.find((c) => c.cards.some((card) => card.id === cardId))
  }

  // "over" bir kart da olabilir, sütunun kendisi de (boş sütuna bırakırken).
  function resolveColumnId(overId: string, overType: unknown) {
    return overType === 'column' ? overId : findColumnOfCard(overId)?.id
  }

  function handleDragStart({ active }: DragStartEvent) {
    const id = String(active.id)
    snapshot.current = columns
    if (active.data.current?.type === 'column') {
      setActiveColumn(columns.find((c) => c.id === id) ?? null)
      return
    }
    const column = findColumnOfCard(id)!
    dragOrigin.current = { columnId: column.id, index: column.cards.findIndex((c) => c.id === id) }
    setActiveCard(column.cards.find((c) => c.id === id) ?? null)
  }

  function handleDragOver({ active, over }: DragOverEvent) {
    if (!over || active.data.current?.type !== 'card') return

    const activeId = String(active.id)
    const overId = String(over.id)
    const overType = over.data.current?.type

    // Kartı yeni sütuna, üzerine gelinen kartın yerine (yoksa en sona) yerleştir.
    // Bu olay art arda çok hızlı gelebilir; o yüzden her şeyi en güncel durumdan (prev) hesaplıyoruz.
    setColumns((prev) => {
      const fromColumn = findColumnOfCard(activeId, prev)
      const toColumnId = overType === 'column' ? overId : findColumnOfCard(overId, prev)?.id
      if (!fromColumn || !toColumnId || fromColumn.id === toColumnId) return prev

      const card = fromColumn.cards.find((c) => c.id === activeId)!
      return prev.map((column) => {
        if (column.id === fromColumn.id) return { ...column, cards: column.cards.filter((c) => c.id !== activeId) }
        if (column.id !== toColumnId) return column

        const overIndex = column.cards.findIndex((c) => c.id === overId)
        const index = overIndex >= 0 ? overIndex : column.cards.length
        return { ...column, cards: [...column.cards.slice(0, index), card, ...column.cards.slice(index)] }
      })
    })
  }

  function handleDragEnd({ active, over }: DragEndEvent) {
    const activeId = String(active.id)
    const isColumn = active.data.current?.type === 'column'
    setActiveCard(null)
    setActiveColumn(null)

    if (isColumn) {
      const overColumnId = over ? resolveColumnId(String(over.id), over.data.current?.type) : undefined
      const from = columns.findIndex((c) => c.id === activeId)
      const to = columns.findIndex((c) => c.id === overColumnId)
      if (from < 0 || to < 0 || from === to) return afterDrop()

      setColumns(arrayMove(columns, from, to))
      return afterDrop(columnsApi.move(activeId, to))
    }

    const origin = dragOrigin.current
    dragOrigin.current = null
    const column = findColumnOfCard(activeId)
    if (!origin || !column) return afterDrop()

    // Aynı sütun içinde yer değiştirme (sütun değişimi zaten onDragOver'da yapıldı).
    let cards = column.cards
    const from = cards.findIndex((c) => c.id === activeId)
    const overIndex = over ? cards.findIndex((c) => c.id === over.id) : -1
    if (overIndex >= 0 && overIndex !== from) {
      cards = arrayMove(cards, from, overIndex)
      setColumns((prev) => prev.map((c) => (c.id === column.id ? { ...c, cards } : c)))
    }

    const index = cards.findIndex((c) => c.id === activeId)
    if (column.id === origin.columnId && index === origin.index) return afterDrop()

    // Sunucuya "şu sütunun şu sırasına" diyoruz; sıra numarasını sunucu hesaplar.
    afterDrop(cardsApi.move(activeId, column.id, index))
  }

  function handleDragCancel() {
    setActiveCard(null)
    setActiveColumn(null)
    dragOrigin.current = null
    setColumns(snapshot.current)
    afterDrop()
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
      onDragCancel={handleDragCancel}
    >
      <div className="flex h-full items-start gap-4 overflow-x-auto pb-4">
        <SortableContext items={columns.map((c) => c.id)} strategy={horizontalListSortingStrategy}>
          {columns.map((column) => (
            <ColumnView
              key={column.id}
              column={column}
              onOpenCard={onOpenCard}
              onCardAdded={(columnId, card) =>
                setColumns((prev) => prev.map((c) => (c.id === columnId ? { ...c, cards: [...c.cards, card] } : c)))
              }
              onRenamed={(columnId, name) =>
                setColumns((prev) => prev.map((c) => (c.id === columnId ? { ...c, name } : c)))
              }
              onDeleted={(columnId) => setColumns((prev) => prev.filter((c) => c.id !== columnId))}
            />
          ))}
        </SortableContext>

        <AddColumn
          boardId={boardId}
          onAdded={(column) => setColumns((prev) => [...prev, column])}
        />
      </div>

      {/* Sürüklenen öğenin imleci takip eden kopyası. */}
      <DragOverlay>
        {activeCard && <CardFace card={activeCard} lifted />}
        {activeColumn && (
          <div className="w-72 rotate-2 rounded-lg bg-slate-100 p-3 shadow-lg">
            <p className="text-sm font-semibold text-slate-700">{activeColumn.name}</p>
            <p className="mt-1 text-xs text-slate-400">{activeColumn.cards.length} kart</p>
          </div>
        )}
      </DragOverlay>
    </DndContext>
  )
}

function AddColumn({ boardId, onAdded }: { boardId: string; onAdded: (column: Column) => void }) {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) return
    onAdded(await columnsApi.create(boardId, trimmed))
    setName('')
  }

  if (!open)
    return (
      <button
        onClick={() => setOpen(true)}
        className="w-72 shrink-0 rounded-lg border-2 border-dashed border-slate-300 px-3 py-3 text-left text-sm text-slate-500 hover:border-slate-400 hover:text-slate-700"
      >
        + Sütun ekle
      </button>
    )

  return (
    <form onSubmit={handleSubmit} className="w-72 shrink-0 rounded-lg bg-slate-100 p-3">
      <input
        autoFocus
        value={name}
        maxLength={100}
        onChange={(e) => setName(e.target.value)}
        onKeyDown={(e) => e.key === 'Escape' && setOpen(false)}
        placeholder="Sütun adı…"
        className="block w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm outline-none focus:border-indigo-500"
      />
      <div className="mt-2 flex gap-2">
        <button type="submit" className="rounded-md bg-indigo-600 px-3 py-1 text-sm font-medium text-white hover:bg-indigo-700">
          Ekle
        </button>
        <button type="button" onClick={() => setOpen(false)} className="px-2 text-sm text-slate-500 hover:text-slate-700">
          Vazgeç
        </button>
      </div>
    </form>
  )
}
