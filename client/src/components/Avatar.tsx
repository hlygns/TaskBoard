// İsmin baş harflerinden renkli yuvarlak avatar. Renk isimden türetilir, böylece hep aynı kalır.
const colors = ['bg-indigo-500', 'bg-emerald-500', 'bg-amber-500', 'bg-rose-500', 'bg-sky-500', 'bg-violet-500']

type AvatarProps = {
  name: string
  size?: 'sm' | 'md'
  // Şu an panoda olanlar için sağ altta yeşil nokta.
  online?: boolean
}

export function Avatar({ name, size = 'md', online = false }: AvatarProps) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toLocaleUpperCase('tr'))
    .join('')
  const color = colors[[...name].reduce((sum, ch) => sum + ch.charCodeAt(0), 0) % colors.length]
  const sizeClass = size === 'sm' ? 'h-7 w-7 text-xs' : 'h-9 w-9 text-sm'

  return (
    <span className="relative inline-flex shrink-0">
      <span
        title={online ? `${name} · şu an panoda` : name}
        className={`inline-grid place-items-center rounded-full font-medium text-white ring-2 ring-white ${color} ${sizeClass}`}
      >
        {initials}
      </span>
      {online && (
        // z-10: avatarlar üst üste bindiğinde nokta yandaki avatarın altında kalmasın.
        <span className="absolute right-0 bottom-0 z-10 h-2.5 w-2.5 rounded-full bg-emerald-500 ring-2 ring-white" />
      )}
    </span>
  )
}
