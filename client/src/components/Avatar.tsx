// İsmin baş harflerinden renkli yuvarlak avatar. Renk isimden türetilir, böylece hep aynı kalır.
const colors = ['bg-indigo-500', 'bg-emerald-500', 'bg-amber-500', 'bg-rose-500', 'bg-sky-500', 'bg-violet-500']

export function Avatar({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' }) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toLocaleUpperCase('tr'))
    .join('')
  const color = colors[[...name].reduce((sum, ch) => sum + ch.charCodeAt(0), 0) % colors.length]
  const sizeClass = size === 'sm' ? 'h-7 w-7 text-xs' : 'h-9 w-9 text-sm'

  return (
    <span
      title={name}
      className={`inline-grid shrink-0 place-items-center rounded-full font-medium text-white ring-2 ring-white ${color} ${sizeClass}`}
    >
      {initials}
    </span>
  )
}
