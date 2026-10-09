import type { CardPriority, LabelColor } from './types'

export const colors = {
  primary: '#4f46e5',
  primarySoft: '#eef2ff',
  text: '#0f172a',
  textMuted: '#64748b',
  textFaint: '#94a3b8',
  border: '#e2e8f0',
  background: '#f8fafc',
  surface: '#ffffff',
  surfaceMuted: '#f1f5f9',
  danger: '#dc2626',
  dangerSoft: '#fef2f2',
  success: '#10b981',
}

export const priorities: { value: CardPriority; label: string; fg: string; bg: string }[] = [
  { value: 'Low', label: 'Düşük', fg: '#475569', bg: '#f1f5f9' },
  { value: 'Medium', label: 'Orta', fg: '#0369a1', bg: '#f0f9ff' },
  { value: 'High', label: 'Yüksek', fg: '#b45309', bg: '#fffbeb' },
  { value: 'Urgent', label: 'Acil', fg: '#b91c1c', bg: '#fef2f2' },
]

export const priorityMeta = (p: CardPriority) => priorities.find((x) => x.value === p)!

// Web'deki etiket paletinin (Tailwind) karşılıkları.
export const labelColors: Record<LabelColor, { fg: string; bg: string }> = {
  slate: { fg: '#334155', bg: '#e2e8f0' },
  red: { fg: '#b91c1c', bg: '#fee2e2' },
  orange: { fg: '#c2410c', bg: '#ffedd5' },
  amber: { fg: '#92400e', bg: '#fef3c7' },
  green: { fg: '#15803d', bg: '#dcfce7' },
  teal: { fg: '#0f766e', bg: '#ccfbf1' },
  sky: { fg: '#0369a1', bg: '#e0f2fe' },
  indigo: { fg: '#4338ca', bg: '#e0e7ff' },
  violet: { fg: '#6d28d9', bg: '#ede9fe' },
  pink: { fg: '#be185d', bg: '#fce7f3' },
}

// Panoya kimliğinden türetilen sabit renk (web ile aynı mantık).
const boardColors = ['#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#f43f5e', '#8b5cf6']
export function boardColor(boardId: string) {
  const hash = [...boardId].reduce((sum, ch) => (sum * 31 + ch.charCodeAt(0)) >>> 0, 7)
  return boardColors[hash % boardColors.length]
}

// --- Tarihler ----------------------------------------------------------------
// Son tarih gün olarak tutulur (UTC gece yarısı). Saat dilimi kaymasın diye sadece tarih kısmı kullanılır.
export const dateKey = (iso: string) => iso.slice(0, 10)

export function localDateKey(offsetDays = 0) {
  const d = new Date()
  d.setDate(d.getDate() + offsetDays)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

export const toDueDate = (key: string) => `${key}T00:00:00Z`

const months = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara']

export function formatDue(iso: string) {
  const [, m, d] = dateKey(iso).split('-').map(Number)
  if (dateKey(iso) === localDateKey(0)) return 'Bugün'
  if (dateKey(iso) === localDateKey(1)) return 'Yarın'
  return `${d} ${months[m - 1]}`
}

export const isOverdue = (iso: string) => dateKey(iso) < localDateKey(0)

export function timeAgo(iso: string) {
  const minutes = Math.round((Date.now() - new Date(iso).getTime()) / 60000)
  if (minutes < 1) return 'az önce'
  if (minutes < 60) return `${minutes} dk önce`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} sa önce`
  return `${Math.round(hours / 24)} gün önce`
}
