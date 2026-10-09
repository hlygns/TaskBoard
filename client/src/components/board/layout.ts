// Sütun düzeni: "grid" = sütunlar ızgarada alt alta da dizilir ve ekranı doldurur,
// "row" = klasik Kanban, hepsi yan yana (yatay kaydırma).
export type BoardLayout = 'grid' | 'row'

const STORAGE_KEY = 'taskboard.boardLayout'

// Tercih sadece bu tarayıcıda hatırlanır. Gizli pencerede ya da depolama kapalıysa varsayılana döner.
export function readLayout(): BoardLayout {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'row' ? 'row' : 'grid'
  } catch {
    return 'grid'
  }
}

export function saveLayout(layout: BoardLayout) {
  try {
    localStorage.setItem(STORAGE_KEY, layout)
  } catch {
    // Depolama kullanılamıyorsa tercih sadece bu oturumda geçerli olur.
  }
}

// Izgarada satır başına sütun sayısı: 4 sütun → 2×2, 6 sütun → 3×2. En fazla 3.
// Tailwind sınıfları tam yazılmalı; küçük ekranda tek sütuna iner.
export function gridColumnsClass(columnCount: number) {
  const perRow = Math.min(3, Math.max(1, Math.ceil(columnCount / 2)))
  if (perRow === 1) return 'grid-cols-1'
  if (perRow === 2) return 'grid-cols-1 md:grid-cols-2'
  return 'grid-cols-1 md:grid-cols-2 xl:grid-cols-3'
}
