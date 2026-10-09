import type { Label } from '../../api/boards'
import { priorities } from '../../utils/format'
import { labelColors } from '../../utils/labels'
import { emptyFilter, isFilterActive, type BoardFilter, type DueFilter } from './boardFilter'

type FilterBarProps = {
  filter: BoardFilter
  labels: Label[]
  onChange: (filter: BoardFilter) => void
}

const selectClass =
  'rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-700 outline-none focus:border-indigo-500'

export function FilterBar({ filter, labels, onChange }: FilterBarProps) {
  const active = isFilterActive(filter)
  const set = (change: Partial<BoardFilter>) => onChange({ ...filter, ...change })

  return (
    <div className="flex min-w-0 flex-1 flex-wrap items-center gap-2">
      <input
        type="search"
        value={filter.text}
        onChange={(e) => set({ text: e.target.value })}
        placeholder="Kartlarda ara…"
        aria-label="Kartlarda ara"
        className="w-48 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
      />

      <select value={filter.due} onChange={(e) => set({ due: e.target.value as DueFilter })} aria-label="Son tarih" className={selectClass}>
        <option value="all">Tüm tarihler</option>
        <option value="overdue">Gecikmiş</option>
        <option value="today">Bugün</option>
        <option value="week">Bu hafta</option>
        <option value="none">Tarihsiz</option>
      </select>

      <select
        value={filter.priority}
        onChange={(e) => set({ priority: e.target.value as BoardFilter['priority'] })}
        aria-label="Öncelik"
        className={selectClass}
      >
        <option value="all">Tüm öncelikler</option>
        {priorities.map((p) => (
          <option key={p.value} value={p.value}>
            {p.label}
          </option>
        ))}
      </select>

      {labels.map((label) => {
        const selected = filter.labelIds.includes(label.id)
        return (
          <button
            key={label.id}
            type="button"
            aria-pressed={selected}
            onClick={() =>
              set({ labelIds: selected ? filter.labelIds.filter((id) => id !== label.id) : [...filter.labelIds, label.id] })
            }
            className={`rounded-full px-2.5 py-1 text-xs font-medium ring-2 ${labelColors[label.color].chip} ${
              selected ? 'ring-slate-700' : 'opacity-60 ring-transparent hover:opacity-100'
            }`}
          >
            {label.name}
          </button>
        )
      })}

      <label className="flex items-center gap-1.5 text-sm text-slate-600">
        <input
          type="checkbox"
          checked={filter.hideCompleted}
          onChange={(e) => set({ hideCompleted: e.target.checked })}
          className="accent-indigo-600"
        />
        Tamamlananları gizle
      </label>

      {active && (
        <>
          <button type="button" onClick={() => onChange(emptyFilter)} className="text-sm font-medium text-indigo-600 hover:underline">
            Filtreyi temizle
          </button>
          <span className="text-xs text-slate-400">Filtre açıkken sürükle-bırak kapalı</span>
        </>
      )}
    </div>
  )
}
