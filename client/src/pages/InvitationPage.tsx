import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { invitationsApi, type InvitationPreview } from '../api/boards'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { Spinner } from '../components/Spinner'

const statusMessages: Record<Exclude<InvitationPreview['status'], 'Pending'>, string> = {
  Accepted: 'Bu davet zaten kabul edilmiş.',
  Declined: 'Bu davet reddedilmiş.',
  Expired: 'Bu davetin süresi dolmuş. Pano sahibinden yeni bir davet isteyebilirsin.',
}

// Mail'deki linkin açtığı sayfa: /invitations/:token
// Giriş yapmamış kişi de daveti görür; kabul etmek için giriş/kayıt sonrası buraya geri döner.
export function InvitationPage() {
  const { token } = useParams<{ token: string }>()
  const { user, loading } = useAuth()
  const navigate = useNavigate()
  const [invitation, setInvitation] = useState<InvitationPreview | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    invitationsApi
      .preview(token!)
      .then(setInvitation)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Davet yüklenemedi.'))
  }, [token])

  async function handleAccept() {
    setBusy(true)
    setError(null)
    try {
      const { boardId } = await invitationsApi.accept(token!)
      navigate(`/boards/${boardId}`, { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Davet kabul edilemedi.')
      setBusy(false)
    }
  }

  async function handleDecline() {
    setBusy(true)
    setError(null)
    try {
      await invitationsApi.decline(token!)
      navigate(user ? '/boards' : '/', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İşlem başarısız.')
      setBusy(false)
    }
  }

  if (loading || (!invitation && !error)) return <Spinner />

  const returnState = { from: `/invitations/${token}` }

  return (
    <div className="mx-auto max-w-md px-4 py-16">
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        {invitation ? (
          <>
            <p className="text-sm text-slate-500">
              <span className="font-medium text-slate-700">{invitation.invitedByName}</span> seni davet etti
            </p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-900">{invitation.boardName}</h1>
            <p className="mt-1 text-xs text-slate-400">Davet edilen: {invitation.email}</p>

            {invitation.status !== 'Pending' ? (
              <p className="mt-6 rounded-md bg-slate-50 px-3 py-2 text-sm text-slate-600">
                {statusMessages[invitation.status]}
              </p>
            ) : user ? (
              <div className="mt-8 flex justify-center gap-3">
                <button
                  onClick={handleDecline}
                  disabled={busy}
                  className="rounded-md border border-slate-300 px-5 py-2 text-sm text-slate-700 hover:bg-slate-50 disabled:opacity-60"
                >
                  Reddet
                </button>
                <button
                  onClick={handleAccept}
                  disabled={busy}
                  className="rounded-md bg-indigo-600 px-5 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
                >
                  Kabul et ve panoya git
                </button>
              </div>
            ) : (
              <div className="mt-8 space-y-3">
                <p className="text-sm text-slate-600">Daveti kabul etmek için {invitation.email} hesabıyla giriş yap.</p>
                <div className="flex justify-center gap-3">
                  <Link
                    to="/login"
                    state={returnState}
                    className="rounded-md border border-slate-300 px-5 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  >
                    Giriş yap
                  </Link>
                  <Link
                    to="/register"
                    state={returnState}
                    className="rounded-md bg-indigo-600 px-5 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                  >
                    Kayıt ol
                  </Link>
                </div>
              </div>
            )}
          </>
        ) : null}

        {error && <p className="mt-6 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      </div>
    </div>
  )
}
