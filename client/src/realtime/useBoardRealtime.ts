import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import { getAccessToken, refreshSession, setRealtimeConnectionId } from '../api/client'

export type BoardEventType =
  | 'BoardUpdated'
  | 'BoardDeleted'
  | 'MembersChanged'
  | 'ColumnCreated'
  | 'ColumnUpdated'
  | 'ColumnMoved'
  | 'ColumnDeleted'
  | 'CardCreated'
  | 'CardUpdated'
  | 'CardMoved'
  | 'CardDeleted'
  | 'CommentAdded'
  | 'CommentDeleted'

export type BoardEvent = { type: BoardEventType; cardId: string | null; columnId: string | null }
export type Actor = { userId: string; fullName: string }
export type OnlineUser = { userId: string; fullName: string }

type Handlers = {
  onEvent: (event: BoardEvent, actor: Actor) => void
  // Bağlantı koptu ve geri geldi: arada kaçan olaylar olabilir, pano baştan yüklenmeli.
  onReconnected: () => void
}

// Pano sayfası açıkken SignalR bağlantısını yönetir:
// bağlan → panonun grubuna katıl → olayları dinle → sayfadan çıkınca ayrıl ve kapat.
export function useBoardRealtime(boardId: string | undefined, handlers: Handlers) {
  const [online, setOnline] = useState<OnlineUser[]>([])
  const [connected, setConnected] = useState(false)

  // Handler'lar her render'da yeniden oluşur; bağlantıyı yeniden kurmamak için ref'te tutuyoruz.
  const handlersRef = useRef(handlers)
  useEffect(() => {
    handlersRef.current = handlers
  })

  useEffect(() => {
    if (!boardId) return

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/board', {
        // Her bağlanma/yeniden bağlanma denemesinde çağrılır; token süresi dolmuşsa yenisini alır.
        accessTokenFactory: async () => getAccessToken() ?? (await refreshSession())?.accessToken ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('BoardEvent', (eventBoardId: string, event: BoardEvent, actor: Actor) => {
      if (eventBoardId === boardId) handlersRef.current.onEvent(event, actor)
    })
    connection.on('PresenceChanged', (eventBoardId: string, users: OnlineUser[]) => {
      if (eventBoardId === boardId) setOnline(users)
    })

    connection.onreconnecting(() => {
      setConnected(false)
      setRealtimeConnectionId(null)
    })
    connection.onreconnected(async (connectionId) => {
      setRealtimeConnectionId(connectionId ?? null)
      setConnected(true)
      await connection.invoke('JoinBoard', boardId)
      handlersRef.current.onReconnected()
    })
    connection.onclose(() => {
      setConnected(false)
      setRealtimeConnectionId(null)
    })

    let disposed = false
    // Başlatmayı bir sonraki tur'a erteliyoruz: geliştirme modunda React (StrictMode) effect'i
    // hemen kapatıp yeniden açar; böylece ilk turda hiç bağlantı denenmez, konsolda hata çıkmaz.
    const startTimer = setTimeout(() => {
      connection
        .start()
        .then(async () => {
          if (disposed) return
          setRealtimeConnectionId(connection.connectionId)
          await connection.invoke('JoinBoard', boardId)
          setConnected(true)
        })
        .catch(() => {
          // Canlı güncelleme olmadan da uygulama çalışır; sadece başkalarının değişikliği anında gelmez.
          if (!disposed) setConnected(false)
        })
    })

    return () => {
      disposed = true
      clearTimeout(startTimer)
      setRealtimeConnectionId(null)
      setOnline([])
      if (connection.state === HubConnectionState.Connected)
        connection.invoke('LeaveBoard', boardId).finally(() => connection.stop())
      else connection.stop()
    }
  }, [boardId])

  return { online, connected }
}
