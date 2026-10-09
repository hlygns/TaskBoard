// Her panoya kimliğinden türetilen sabit bir renk: adı değişse de rengi aynı kalır.
// Tailwind sınıfları tam yazılmalı (birleştirilmiş sınıf adlarını derleme sırasında bulamaz).
const themes = [
  { gradient: 'from-indigo-500 to-violet-500', soft: 'bg-indigo-50 text-indigo-700', bar: 'bg-indigo-500' },
  { gradient: 'from-sky-500 to-cyan-400', soft: 'bg-sky-50 text-sky-700', bar: 'bg-sky-500' },
  { gradient: 'from-emerald-500 to-teal-400', soft: 'bg-emerald-50 text-emerald-700', bar: 'bg-emerald-500' },
  { gradient: 'from-amber-500 to-orange-400', soft: 'bg-amber-50 text-amber-800', bar: 'bg-amber-500' },
  { gradient: 'from-rose-500 to-pink-400', soft: 'bg-rose-50 text-rose-700', bar: 'bg-rose-500' },
  { gradient: 'from-violet-500 to-fuchsia-400', soft: 'bg-violet-50 text-violet-700', bar: 'bg-violet-500' },
]

export type BoardTheme = (typeof themes)[number]

export function boardTheme(boardId: string): BoardTheme {
  const hash = [...boardId].reduce((sum, ch) => (sum * 31 + ch.charCodeAt(0)) >>> 0, 7)
  return themes[hash % themes.length]
}

export function boardInitial(name: string) {
  return name.trim().charAt(0).toLocaleUpperCase('tr') || '?'
}
