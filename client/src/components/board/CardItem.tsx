import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import type { CardSummary } from '../../api/boards'
import { formatDueDate, isOverdue, priorityMeta } from '../../utils/format'
import { Avatar } from '../Avatar'

type CardItemProps = {
  card: CardSummary
  columnId: string
  onOpen: (cardId: string) => void
}

// Sürüklenebilir kart. useSortable, kartı hem sürüklenebilir hem de üzerine bırakılabilir yapar.
export function CardItem({ card, columnId, onOpen }: CardItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: 'card', columnId },
  })

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      {...attributes}
      {...listeners}
      onClick={() => onOpen(card.id)}
      // Sürüklenirken yerinde soluk bir "gölge" kalır; asıl kart DragOverlay'de gezer.
      className={isDragging ? 'opacity-40' : ''}
    >
      <CardFace card={card} />
    </div>
  )
}

export function CardFace({ card, lifted = false }: { card: CardSummary; lifted?: boolean }) {
  const priority = priorityMeta(card.priority)
  const overdue = card.dueDate !== null && isOverdue(card.dueDate)

  return (
    <div
      className={`cursor-pointer rounded-md border border-slate-200 bg-white p-3 text-sm shadow-sm hover:border-indigo-300 ${
        lifted ? 'rotate-2 shadow-lg' : ''
      }`}
    >
      <p className="break-words text-slate-800">{card.title}</p>

      <div className="mt-2 flex flex-wrap items-center gap-1.5 text-xs">
        {card.priority !== 'Medium' && (
          <span className={`rounded px-1.5 py-0.5 font-medium ${priority.className}`}>{priority.label}</span>
        )}
        {card.dueDate && (
          <span className={`rounded px-1.5 py-0.5 ${overdue ? 'bg-red-50 text-red-700' : 'bg-slate-100 text-slate-600'}`}>
            📅 {formatDueDate(card.dueDate)}
          </span>
        )}
        {card.hasDescription && <span title="Açıklaması var" className="text-slate-400">≡</span>}
        {card.commentCount > 0 && <span className="text-slate-400">💬 {card.commentCount}</span>}
        {card.assignee && (
          <span className="ml-auto">
            <Avatar name={card.assignee.fullName} size="sm" />
          </span>
        )}
      </div>
    </div>
  )
}
