import type { CardPriority } from '../api/boards'

export const priorities: { value: CardPriority; label: string; className: string }[] = [
  { value: 'Low', label: 'Düşük', className: 'bg-slate-100 text-slate-600' },
  { value: 'Medium', label: 'Orta', className: 'bg-sky-50 text-sky-700' },
  { value: 'High', label: 'Yüksek', className: 'bg-amber-50 text-amber-700' },
  { value: 'Urgent', label: 'Acil', className: 'bg-red-50 text-red-700' },
]

export function priorityMeta(priority: CardPriority) {
  return priorities.find((p) => p.value === priority)!
}

// Son tarih gün olarak tutuluyor (UTC gece yarısı). Saat dilimi kaymasın diye
// ISO metnin sadece tarih kısmını kullanıyoruz: "2026-10-20T00:00:00Z" → "2026-10-20"
export function toDateInput(iso: string | null): string {
  return iso ? iso.slice(0, 10) : ''
}

export function fromDateInput(value: string): string | null {
  return value ? `${value}T00:00:00Z` : null
}

export function formatDueDate(iso: string): string {
  const [y, m, d] = toDateInput(iso).split('-').map(Number)
  return new Date(y, m - 1, d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' })
}

export function isOverdue(iso: string): boolean {
  const today = new Date()
  const todayKey = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
  return toDateInput(iso) < todayKey
}

const relative = new Intl.RelativeTimeFormat('tr', { numeric: 'auto' })

// "5 dakika önce", "dün" gibi.
export function timeAgo(iso: string): string {
  const seconds = Math.round((new Date(iso).getTime() - Date.now()) / 1000)
  const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ['day', 86400],
    ['hour', 3600],
    ['minute', 60],
  ]
  for (const [unit, size] of units) {
    if (Math.abs(seconds) >= size) return relative.format(Math.round(seconds / size), unit)
  }
  return 'az önce'
}
