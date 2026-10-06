import { useEffect, useState, type FormEvent } from 'react'
import { boardsApi, type BoardDetail, type Invitation } from '../api/boards'
import { ApiError } from '../api/client'
import { Avatar } from './Avatar'

type MembersPanelProps = {
  board: BoardDetail
  currentUserId: string
  onChanged: () => void
}

export function MembersPanel({ board, currentUserId, onChanged }: MembersPanelProps) {
  const isOwner = board.myRole === 'Owner'
  const [invitations, setInvitations] = useState<Invitation[]>([])
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState<{ type: 'ok' | 'error'; text: string } | null>(null)
  const [sending, setSending] = useState(false)

  useEffect(() => {
    if (isOwner) boardsApi.invitations(board.id).then(setInvitations)
  }, [board.id, isOwner])

  async function handleInvite(e: FormEvent) {
    e.preventDefault()
    setMessage(null)
    setSending(true)
    try {
      const invitation = await boardsApi.invite(board.id, email)
      // Aynı kişiye tekrar gönderildiyse eski davet listeden düşer.
      setInvitations((list) => [invitation, ...list.filter((i) => i.email !== invitation.email)])
      setMessage({ type: 'ok', text: `${invitation.email} adresine davet gönderildi.` })
      setEmail('')
    } catch (err) {
      setMessage({ type: 'error', text: err instanceof ApiError ? err.message : 'Davet gönderilemedi.' })
    } finally {
      setSending(false)
    }
  }

  async function handleCancel(invitationId: string) {
    await boardsApi.cancelInvitation(board.id, invitationId)
    setInvitations((list) => list.filter((i) => i.id !== invitationId))
  }

  async function handleRemove(userId: string, fullName: string) {
    if (!confirm(`${fullName} panodan çıkarılsın mı?`)) return
    await boardsApi.removeMember(board.id, userId)
    onChanged()
  }

  return (
    <aside className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="font-semibold text-slate-900">Üyeler ({board.members.length})</h2>

      <ul className="mt-4 space-y-3">
        {board.members.map((member) => (
          <li key={member.userId} className="flex items-center gap-3">
            <Avatar name={member.fullName} size="sm" />
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-slate-800">
                {member.fullName}
                {member.userId === currentUserId && <span className="font-normal text-slate-400"> (sen)</span>}
              </p>
              <p className="truncate text-xs text-slate-500">{member.email}</p>
            </div>
            {member.role === 'Owner' ? (
              <span className="text-xs text-slate-400">Sahip</span>
            ) : (
              isOwner && (
                <button
                  onClick={() => handleRemove(member.userId, member.fullName)}
                  className="text-xs text-slate-400 hover:text-red-600"
                >
                  Çıkar
                </button>
              )
            )}
          </li>
        ))}
      </ul>

      {isOwner && (
        <>
          <form onSubmit={handleInvite} className="mt-6 border-t border-slate-100 pt-5">
            <label className="text-sm font-medium text-slate-700" htmlFor="invite-email">
              E-posta ile davet et
            </label>
            <div className="mt-2 flex gap-2">
              <input
                id="invite-email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="ornek@mail.com"
                className="min-w-0 flex-1 rounded-md border border-slate-300 px-3 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              />
              <button
                type="submit"
                disabled={sending}
                className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
              >
                Davet et
              </button>
            </div>
            {message && (
              <p className={`mt-2 text-xs ${message.type === 'ok' ? 'text-emerald-700' : 'text-red-700'}`}>
                {message.text}
              </p>
            )}
          </form>

          {invitations.length > 0 && (
            <div className="mt-5">
              <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-400">Bekleyen davetler</h3>
              <ul className="mt-2 space-y-2">
                {invitations.map((invitation) => (
                  <li key={invitation.id} className="flex items-center justify-between gap-2 text-sm">
                    <span className="truncate text-slate-600">{invitation.email}</span>
                    <button
                      onClick={() => handleCancel(invitation.id)}
                      className="shrink-0 text-xs text-slate-400 hover:text-red-600"
                    >
                      İptal
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}
    </aside>
  )
}
