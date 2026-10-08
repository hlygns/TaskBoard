import { useEffect, useState, type FormEvent } from 'react'
import { cardsApi, type BoardMember, type CardDetail, type CardPriority, type Label } from '../../api/boards'
import { ApiError } from '../../api/client'
import { fromDateInput, priorities, timeAgo, toDateInput } from '../../utils/format'
import { Avatar } from '../Avatar'
import { Modal } from '../Modal'
import { Spinner } from '../Spinner'
import { Checklist } from './Checklist'
import { LabelPicker } from './LabelPicker'

type CardModalProps = {
  cardId: string
  boardId: string
  labels: Label[]
  members: BoardMember[]
  currentUserId: string
  isOwner: boolean
  onClose: () => void
  // Kart değiştiğinde pano ekranındaki kart yüzü de güncellensin.
  onChanged: () => void
  // Başka biri bu kartı değiştirdiğinde (canlı bildirim) artar; kart sunucudan yeniden çekilir.
  refreshKey: number
}

const inputClass =
  'mt-1 block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100'

export function CardModal({
  cardId,
  boardId,
  labels,
  members,
  currentUserId,
  isOwner,
  onClose,
  onChanged,
  refreshKey,
}: CardModalProps) {
  const [card, setCard] = useState<CardDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  // Kart sunucudan her yüklendiğinde artar; form bu değerle yeniden kurulur.
  const [loadVersion, setLoadVersion] = useState(0)

  useEffect(() => {
    cardsApi
      .get(cardId)
      .then((loaded) => {
        setCard(loaded)
        setLoadVersion((v) => v + 1)
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Kart yüklenemedi.'))
  }, [cardId, refreshKey])

  async function toggleCompleted() {
    if (!card) return
    const completed = card.completedAt === null
    setCard({ ...card, completedAt: completed ? new Date().toISOString() : null })
    await cardsApi.setCompleted(card.id, completed)
    onChanged()
  }

  async function archive() {
    if (!card) return
    await cardsApi.setArchived(card.id, true)
    onChanged()
    onClose()
  }

  return (
    <Modal title={card ? `${card.columnName} sütununda` : 'Kart'} size="lg" onClose={onClose}>
      {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {!card && !error && <Spinner />}
      {card && (
        <>
          <div className="mb-5 flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={toggleCompleted}
              aria-pressed={card.completedAt !== null}
              className={`rounded-md px-3 py-1.5 text-sm font-medium ${
                card.completedAt
                  ? 'bg-emerald-600 text-white hover:bg-emerald-700'
                  : 'border border-slate-300 text-slate-700 hover:border-emerald-500 hover:text-emerald-700'
              }`}
            >
              {card.completedAt ? '✓ Tamamlandı' : '✓ Tamamla'}
            </button>
            {card.completedAt && <span className="text-xs text-slate-500">Tamamlandı · {timeAgo(card.completedAt)}</span>}
            <button
              type="button"
              onClick={archive}
              title="Panodan kaldırır; Arşiv panelinden geri alabilirsin"
              className="ml-auto rounded-md px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100"
            >
              Arşivle
            </button>
          </div>

          <div className="mb-5">
            <LabelPicker
              boardId={boardId}
              cardId={card.id}
              boardLabels={labels}
              selectedIds={card.labelIds}
              onChange={(labelIds) => {
                setCard({ ...card, labelIds })
                onChanged()
              }}
              onLabelsChanged={onChanged}
            />
          </div>

          <CardEditor
            // Başkası kartı değiştirince form yeni değerlerle baştan kurulsun.
            // (Kendi kaydımızda yeniden kurulmaz; "Kaydedildi" mesajı kaybolmasın.)
            key={loadVersion}
            card={card}
            members={members}
            onSaved={(updated) => {
              setCard(updated)
              onChanged()
            }}
            onDeleted={() => {
              onChanged()
              onClose()
            }}
          />
          <Checklist
            cardId={card.id}
            items={card.checklist}
            onChange={(checklist) => {
              setCard({ ...card, checklist })
              onChanged()
            }}
          />
          <Comments
            card={card}
            currentUserId={currentUserId}
            isOwner={isOwner}
            onChange={(comments) => {
              setCard({ ...card, comments })
              onChanged()
            }}
          />
        </>
      )}
    </Modal>
  )
}

function CardEditor({
  card,
  members,
  onSaved,
  onDeleted,
}: {
  card: CardDetail
  members: BoardMember[]
  onSaved: (card: CardDetail) => void
  onDeleted: () => void
}) {
  const [title, setTitle] = useState(card.title)
  const [description, setDescription] = useState(card.description ?? '')
  const [priority, setPriority] = useState<CardPriority>(card.priority)
  const [dueDate, setDueDate] = useState(toDateInput(card.dueDate))
  const [assigneeId, setAssigneeId] = useState(card.assignee?.userId ?? '')
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState<{ type: 'ok' | 'error'; text: string } | null>(null)

  async function handleSave(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setMessage(null)
    try {
      const updated = await cardsApi.update(card.id, {
        title,
        description: description.trim() || null,
        priority,
        dueDate: fromDateInput(dueDate),
        assigneeId: assigneeId || null,
      })
      onSaved(updated)
      setMessage({ type: 'ok', text: 'Kaydedildi.' })
    } catch (err) {
      setMessage({ type: 'error', text: err instanceof ApiError ? err.message : 'Kaydedilemedi.' })
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    if (!confirm(`"${card.title}" kartı silinsin mi?`)) return
    await cardsApi.remove(card.id)
    onDeleted()
  }

  return (
    <form onSubmit={handleSave} className="space-y-4">
      <label className="block">
        <span className="text-sm font-medium text-slate-700">Başlık</span>
        <input required maxLength={200} value={title} onChange={(e) => setTitle(e.target.value)} className={inputClass} />
      </label>

      <div className="grid gap-4 sm:grid-cols-3">
        <label className="block">
          <span className="text-sm font-medium text-slate-700">Atanan kişi</span>
          <select value={assigneeId} onChange={(e) => setAssigneeId(e.target.value)} className={inputClass}>
            <option value="">Kimse</option>
            {members.map((m) => (
              <option key={m.userId} value={m.userId}>
                {m.fullName}
              </option>
            ))}
          </select>
        </label>
        <label className="block">
          <span className="text-sm font-medium text-slate-700">Son tarih</span>
          <input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} className={inputClass} />
        </label>
        <label className="block">
          <span className="text-sm font-medium text-slate-700">Öncelik</span>
          <select value={priority} onChange={(e) => setPriority(e.target.value as CardPriority)} className={inputClass}>
            {priorities.map((p) => (
              <option key={p.value} value={p.value}>
                {p.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label className="block">
        <span className="text-sm font-medium text-slate-700">Açıklama</span>
        <textarea
          rows={4}
          maxLength={5000}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Daha ayrıntılı bir açıklama ekle…"
          className={inputClass}
        />
      </label>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={saving}
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
        >
          {saving ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
        {message && (
          <span className={`text-sm ${message.type === 'ok' ? 'text-emerald-700' : 'text-red-700'}`}>{message.text}</span>
        )}
        <button type="button" onClick={handleDelete} className="ml-auto rounded-md px-3 py-2 text-sm text-red-600 hover:bg-red-50">
          Kartı sil
        </button>
      </div>
    </form>
  )
}

function Comments({
  card,
  currentUserId,
  isOwner,
  onChange,
}: {
  card: CardDetail
  currentUserId: string
  isOwner: boolean
  onChange: (comments: CardDetail['comments']) => void
}) {
  const [content, setContent] = useState('')
  const [sending, setSending] = useState(false)

  async function handleAdd(e: FormEvent) {
    e.preventDefault()
    if (!content.trim()) return
    setSending(true)
    try {
      const comment = await cardsApi.addComment(card.id, content)
      onChange([...card.comments, comment])
      setContent('')
    } finally {
      setSending(false)
    }
  }

  async function handleDelete(commentId: string) {
    if (!confirm('Yorum silinsin mi?')) return
    await cardsApi.removeComment(commentId)
    onChange(card.comments.filter((c) => c.id !== commentId))
  }

  return (
    <section className="mt-8 border-t border-slate-100 pt-6">
      <h3 className="text-sm font-semibold text-slate-900">Yorumlar ({card.comments.length})</h3>

      <form onSubmit={handleAdd} className="mt-3">
        <textarea
          rows={2}
          maxLength={2000}
          value={content}
          onChange={(e) => setContent(e.target.value)}
          placeholder="Yorum yaz…"
          className={inputClass}
        />
        <button
          type="submit"
          disabled={sending || !content.trim()}
          className="mt-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-900 disabled:opacity-50"
        >
          Gönder
        </button>
      </form>

      <ul className="mt-5 space-y-4">
        {[...card.comments].reverse().map((comment) => (
          <li key={comment.id} className="flex gap-3">
            <Avatar name={comment.author.fullName} size="sm" />
            <div className="min-w-0 flex-1">
              <p className="text-sm">
                <span className="font-medium text-slate-800">{comment.author.fullName}</span>{' '}
                <span className="text-xs text-slate-400">{timeAgo(comment.createdAt)}</span>
              </p>
              <p className="mt-0.5 whitespace-pre-wrap break-words text-sm text-slate-700">{comment.content}</p>
              {(comment.author.userId === currentUserId || isOwner) && (
                <button onClick={() => handleDelete(comment.id)} className="mt-1 text-xs text-slate-400 hover:text-red-600">
                  Sil
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
