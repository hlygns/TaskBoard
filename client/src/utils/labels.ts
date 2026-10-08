import type { LabelColor } from '../api/boards'

// Sunucudaki paletle aynı renkler (LabelService.Colors). Tailwind sınıfları tam yazılmalı;
// "bg-${color}-100" gibi birleştirilmiş sınıfları Tailwind derleme sırasında bulamaz.
export const labelColors: Record<LabelColor, { chip: string; dot: string; name: string }> = {
  slate: { chip: 'bg-slate-200 text-slate-700', dot: 'bg-slate-500', name: 'Gri' },
  red: { chip: 'bg-red-100 text-red-700', dot: 'bg-red-500', name: 'Kırmızı' },
  orange: { chip: 'bg-orange-100 text-orange-700', dot: 'bg-orange-500', name: 'Turuncu' },
  amber: { chip: 'bg-amber-100 text-amber-800', dot: 'bg-amber-500', name: 'Sarı' },
  green: { chip: 'bg-green-100 text-green-700', dot: 'bg-green-500', name: 'Yeşil' },
  teal: { chip: 'bg-teal-100 text-teal-700', dot: 'bg-teal-500', name: 'Turkuaz' },
  sky: { chip: 'bg-sky-100 text-sky-700', dot: 'bg-sky-500', name: 'Mavi' },
  indigo: { chip: 'bg-indigo-100 text-indigo-700', dot: 'bg-indigo-500', name: 'Lacivert' },
  violet: { chip: 'bg-violet-100 text-violet-700', dot: 'bg-violet-500', name: 'Mor' },
  pink: { chip: 'bg-pink-100 text-pink-700', dot: 'bg-pink-500', name: 'Pembe' },
}

export const labelColorList = Object.keys(labelColors) as LabelColor[]
