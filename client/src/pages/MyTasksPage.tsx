import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { cardsApi, tasksApi, type MyTask } from '../api/boards'
import { ApiError } from '../api/client'
import { Spinner } from '../components/Spinner'
import { formatDueDate, priorityMeta, toDateInput } from '../utils/format'
import { labelColors } from '../utils/labels'

type GroupKey = 'overdue' | 'today' | 'tomorrow' | 'week' | 'later' | 'noDate'

const groups: { key: GroupKey; title: string; className: string }[] = [
  { key: 'overdue', title: 'Gecikmiş', className: 'text-red-700' },
  { key: 'today', title: 'Bugün', className: 'text-indigo-700' },
  { key: 'tomorrow', title: 'Yarın', className: 'text-slate-900' },
  { key: 'week', title: 'Bu hafta', className: 'text-slate-900' },
  { key: 'later', title: 'Daha sonra', className: 'text-slate-900' },
  { key: 'noDate', title: 'Tarihsiz (bana atanan)', className: 'text-slate-500' },
]

function localDateKey(offsetDays: number) {
  const d = new Date()
  d.setDate(d.getDate() + offsetDays)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

// Gruplama burada, kullanıcının kendi saat dilimine göre yapılır ("bugün" herkes için farklı olabilir).
function groupOf(task: MyTask): GroupKey {
  if (!task.dueDate) return 'noDate'
  const due = toDateInput(task.dueDate)
  if (due < localDateKey(0)) return 'overdue'
  if (due === localDateKey(0)) return 'today'
  if (due === localDateKey(1)) return 'tomorrow'
  if (due <= localDateKey(7)) return 'week'
  return 'later'
}

// "Görevlerim": tüm panolardaki açık işler, son tarihe göre gruplu. Sabah açıp günü planlamak için.
export function MyTasksPage() {
  const [tasks, setTasks] = useState<MyTask[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    tasksApi
      .mine()
      .then(setTasks)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Görevler yüklenemedi.'))
  }, [])

  async function complete(task: MyTask) {
    // Listeden hemen düşür; sunucu reddederse geri koy.
    setTasks((list) => list?.filter((t) => t.cardId !== task.cardId) ?? null)
    try {
      await cardsApi.setCompleted(task.cardId, true)
    } catch {
      setTasks((list) => (list ? [...list, task] : list))
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-2xl font-semibold text-slate-900">Görevlerim</h1>
      <p className="mt-1 text-sm text-slate-500">
        Tüm panolarındaki açık işler: sana atananlar ve kendi panolarında kimseye atanmamış, son tarihi olan kartlar.
      </p>

      {error && <p className="mt-8 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {!tasks && !error && <Spinner />}

      {tasks?.length === 0 && (
        <div className="mt-8 rounded-xl border-2 border-dashed border-slate-300 py-16 text-center">
          <p className="font-medium text-slate-700">Bekleyen işin yok 🎉</p>
          <p className="mt-1 text-sm text-slate-500">Kartlara son tarih verdiğinde ya da sana atandığında burada görünür.</p>
        </div>
      )}

      {tasks &&
        groups.map((group) => {
          const items = tasks.filter((t) => groupOf(t) === group.key)
          if (items.length === 0) return null
          return (
            <section key={group.key} className="mt-8">
              <h2 className={`text-sm font-semibold ${group.className}`}>
                {group.title} <span className="font-normal text-slate-400">{items.length}</span>
              </h2>
              <ul className="mt-2 divide-y divide-slate-100 rounded-xl border border-slate-200 bg-white">
                {items.map((task) => (
                  <li key={task.cardId} className="flex items-start gap-3 px-4 py-3">
                    <button
                      type="button"
                      onClick={() => complete(task)}
                      title="Tamamlandı olarak işaretle"
                      aria-label={`${task.title}: tamamlandı olarak işaretle`}
                      className="mt-0.5 grid h-5 w-5 shrink-0 place-items-center rounded-full border border-slate-300 text-xs text-transparent hover:border-emerald-500 hover:text-emerald-500"
                    >
                      ✓
                    </button>
                    <Link to={`/boards/${task.boardId}?card=${task.cardId}`} className="min-w-0 flex-1 group">
                      <p className="break-words text-sm font-medium text-slate-800 group-hover:text-indigo-700">{task.title}</p>
                      <p className="mt-0.5 text-xs text-slate-500">
                        {task.boardName} · {task.columnName}
                      </p>
                      <div className="mt-1.5 flex flex-wrap items-center gap-1.5 text-xs empty:hidden">
                        {task.labels.map((l) => (
                          <span key={l.id} className={`rounded px-1.5 py-0.5 font-medium ${labelColors[l.color].chip}`}>
                            {l.name}
                          </span>
                        ))}
                        {task.priority !== 'Medium' && (
                          <span className={`rounded px-1.5 py-0.5 font-medium ${priorityMeta(task.priority).className}`}>
                            {priorityMeta(task.priority).label}
                          </span>
                        )}
                        {task.checklistTotal > 0 && (
                          <span className="rounded bg-slate-100 px-1.5 py-0.5 text-slate-600">
                            ☑ {task.checklistDone}/{task.checklistTotal}
                          </span>
                        )}
                      </div>
                    </Link>
                    {task.dueDate && (
                      <span
                        className={`shrink-0 text-xs ${group.key === 'overdue' ? 'font-medium text-red-700' : 'text-slate-500'}`}
                      >
                        {formatDueDate(task.dueDate)}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </section>
          )
        })}
    </div>
  )
}
