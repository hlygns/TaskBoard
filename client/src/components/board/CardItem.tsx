import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import type { CardSummary } from '../../api/boards'
import { formatDueDate, isOverdue, priorityMeta } from '../../utils/format'
import { labelColors } from '../../utils/labels'
import { Avatar } from '../Avatar'
import { useBoardView } from './boardView'

type CardItemProps = {
  card: CardSummary
  columnId: string
  onOpen: (cardId: string) => void
}

// Sürüklenebilir kart. useSortable, kartı hem sürüklenebilir hem de üzerine bırakılabilir yapar.
export function CardItem({ card, columnId, onOpen }: CardItemProps) {
  const { dragDisabled } = useBoardView()
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: 'card', columnId },
    disabled: dragDisabled,
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
  const { labels, toggleComplete } = useBoardView()
  const priority = priorityMeta(card.priority)
  // Tamamlanan kartın tarihi artık "gecikmiş" sayılmaz.
  const overdue = !card.isCompleted && card.dueDate !== null && isOverdue(card.dueDate)
  const cardLabels = card.labelIds.map((id) => labels.get(id)).filter((l) => l !== undefined)
  const checklistComplete = card.checklistTotal > 0 && card.checklistDone === card.checklistTotal

  return (
    <div
      className={`group/card cursor-pointer rounded-md border border-slate-200 bg-white p-3 text-sm shadow-sm hover:border-indigo-300 ${
        lifted ? 'rotate-2 shadow-lg' : ''
      } ${card.isCompleted ? 'opacity-60' : ''}`}
    >
      {cardLabels.length > 0 && (
        <div className="mb-2 flex flex-wrap gap-1">
          {cardLabels.map((label) => (
            <span key={label.id} className={`rounded px-1.5 py-0.5 text-[11px] font-medium ${labelColors[label.color].chip}`}>
              {label.name}
            </span>
          ))}
        </div>
      )}

      <div className="flex items-start gap-2">
        <button
          type="button"
          title={card.isCompleted ? 'Tamamlanmadı olarak işaretle' : 'Tamamlandı olarak işaretle'}
          aria-label={card.isCompleted ? 'Tamamlanmadı olarak işaretle' : 'Tamamlandı olarak işaretle'}
          aria-pressed={card.isCompleted}
          // Tıklama kartı açmasın, basılı tutma sürüklemeyi başlatmasın.
          onClick={(e) => {
            e.stopPropagation()
            toggleComplete(card.id, !card.isCompleted)
          }}
          onPointerDown={(e) => e.stopPropagation()}
          onKeyDown={(e) => e.stopPropagation()}
          className={`mt-0.5 grid h-4 w-4 shrink-0 place-items-center rounded-full border text-[10px] leading-none ${
            card.isCompleted
              ? 'border-emerald-500 bg-emerald-500 text-white'
              : 'border-slate-300 text-transparent hover:border-emerald-500 hover:text-emerald-500'
          }`}
        >
          ✓
        </button>
        <p className={`break-words text-slate-800 ${card.isCompleted ? 'line-through decoration-slate-400' : ''}`}>
          {card.title}
        </p>
      </div>

      <div className="mt-2 flex flex-wrap items-center gap-1.5 text-xs empty:hidden">
        {card.priority !== 'Medium' && (
          <span className={`rounded px-1.5 py-0.5 font-medium ${priority.className}`}>{priority.label}</span>
        )}
        {card.dueDate && (
          <span className={`rounded px-1.5 py-0.5 ${overdue ? 'bg-red-50 text-red-700' : 'bg-slate-100 text-slate-600'}`}>
            📅 {formatDueDate(card.dueDate)}
          </span>
        )}
        {card.checklistTotal > 0 && (
          <span
            title="Alt görevler"
            className={`rounded px-1.5 py-0.5 ${checklistComplete ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}
          >
            ☑ {card.checklistDone}/{card.checklistTotal}
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
