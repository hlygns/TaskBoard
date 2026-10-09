import type { BoardLayout } from './layout'

const options: { value: BoardLayout; label: string; icon: string }[] = [
  { value: 'grid', label: 'Izgara', icon: '▦' },
  { value: 'row', label: 'Yan yana', icon: '▥' },
]

export function LayoutToggle({ layout, onChange }: { layout: BoardLayout; onChange: (layout: BoardLayout) => void }) {
  return (
    <div role="radiogroup" aria-label="Sütun düzeni" className="flex rounded-lg border border-slate-300 bg-white p-0.5">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          role="radio"
          aria-checked={layout === o.value}
          onClick={() => onChange(o.value)}
          className={`rounded-md px-3 py-1.5 text-sm font-medium ${
            layout === o.value ? 'bg-indigo-600 text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          <span aria-hidden="true">{o.icon}</span> {o.label}
        </button>
      ))}
    </div>
  )
}
