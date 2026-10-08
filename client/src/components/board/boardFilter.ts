import type { CardPriority, CardSummary } from '../../api/boards'
import { isOverdue, toDateInput } from '../../utils/format'

export type DueFilter = 'all' | 'overdue' | 'today' | 'week' | 'none'

export type BoardFilter = {
  text: string
  labelIds: string[]
  priority: CardPriority | 'all'
  due: DueFilter
  hideCompleted: boolean
}

export const emptyFilter: BoardFilter = { text: '', labelIds: [], priority: 'all', due: 'all', hideCompleted: false }

export function isFilterActive(f: BoardFilter) {
  return f.text.trim() !== '' || f.labelIds.length > 0 || f.priority !== 'all' || f.due !== 'all' || f.hideCompleted
}

function localDateKey(offsetDays = 0) {
  const d = new Date()
  d.setDate(d.getDate() + offsetDays)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

// Filtreyi "kart görünsün mü?" fonksiyonuna çevirir. Tüm pano zaten bellekte olduğu için
// filtreleme istemcide yapılır; sunucuya ek istek gitmez.
export function buildPredicate(f: BoardFilter): (card: CardSummary) => boolean {
  const text = f.text.trim().toLocaleLowerCase('tr')
  const today = localDateKey()
  const weekEnd = localDateKey(7)

  return (card) => {
    if (text && !card.title.toLocaleLowerCase('tr').includes(text)) return false
    // Seçilen etiketlerden en az biri kartta olmalı.
    if (f.labelIds.length > 0 && !f.labelIds.some((id) => card.labelIds.includes(id))) return false
    if (f.priority !== 'all' && card.priority !== f.priority) return false
    if (f.hideCompleted && card.isCompleted) return false

    const due = card.dueDate ? toDateInput(card.dueDate) : null
    switch (f.due) {
      case 'overdue':
        return due !== null && isOverdue(card.dueDate!) && !card.isCompleted
      case 'today':
        return due === today
      case 'week':
        return due !== null && due >= today && due <= weekEnd
      case 'none':
        return due === null
      default:
        return true
    }
  }
}
