import { api } from './client'

export type BoardRole = 'Owner' | 'Member'

export type BoardSummary = {
  id: string
  name: string
  description: string | null
  myRole: BoardRole
  memberCount: number
  createdAt: string
}

export type BoardMember = {
  userId: string
  fullName: string
  email: string
  role: BoardRole
  joinedAt: string
}

export type CardPriority = 'Low' | 'Medium' | 'High' | 'Urgent'

export type MemberRef = {
  userId: string
  fullName: string
}

export type CardSummary = {
  id: string
  title: string
  position: number
  priority: CardPriority
  dueDate: string | null
  assignee: MemberRef | null
  commentCount: number
  hasDescription: boolean
}

export type Column = {
  id: string
  name: string
  position: number
  cards: CardSummary[]
}

export type Comment = {
  id: string
  content: string
  author: MemberRef
  createdAt: string
}

export type CardDetail = {
  id: string
  columnId: string
  columnName: string
  title: string
  description: string | null
  priority: CardPriority
  dueDate: string | null
  assignee: MemberRef | null
  createdAt: string
  updatedAt: string | null
  comments: Comment[]
}

export type CardInput = {
  title: string
  description: string | null
  dueDate: string | null
  priority: CardPriority
  assigneeId: string | null
}

export type BoardDetail = {
  id: string
  name: string
  description: string | null
  myRole: BoardRole
  members: BoardMember[]
  columns: Column[]
}

export type Invitation = {
  id: string
  email: string
  invitedByName: string
  createdAt: string
  expiresAt: string
}

export type InvitationPreview = {
  boardName: string
  invitedByName: string
  email: string
  status: 'Pending' | 'Accepted' | 'Declined' | 'Expired'
  expiresAt: string
}

type BoardInput = { name: string; description: string | null }

export const boardsApi = {
  list: () => api<BoardSummary[]>('/api/boards'),
  get: (id: string) => api<BoardDetail>(`/api/boards/${id}`),
  create: (input: BoardInput) => api<BoardDetail>('/api/boards', { method: 'POST', body: input }),
  update: (id: string, input: BoardInput) => api<void>(`/api/boards/${id}`, { method: 'PUT', body: input }),
  remove: (id: string) => api<void>(`/api/boards/${id}`, { method: 'DELETE' }),
  removeMember: (id: string, userId: string) =>
    api<void>(`/api/boards/${id}/members/${userId}`, { method: 'DELETE' }),

  invitations: (id: string) => api<Invitation[]>(`/api/boards/${id}/invitations`),
  invite: (id: string, email: string) =>
    api<Invitation>(`/api/boards/${id}/invitations`, { method: 'POST', body: { email } }),
  cancelInvitation: (id: string, invitationId: string) =>
    api<void>(`/api/boards/${id}/invitations/${invitationId}`, { method: 'DELETE' }),
}

export const columnsApi = {
  create: (boardId: string, name: string) =>
    api<Column>(`/api/boards/${boardId}/columns`, { method: 'POST', body: { name } }),
  rename: (columnId: string, name: string) => api<void>(`/api/columns/${columnId}`, { method: 'PUT', body: { name } }),
  // index: sütunun önünde kaç sütun olacağı (kendisi hariç)
  move: (columnId: string, index: number) =>
    api<void>(`/api/columns/${columnId}/move`, { method: 'PUT', body: { index } }),
  remove: (columnId: string) => api<void>(`/api/columns/${columnId}`, { method: 'DELETE' }),
}

export const cardsApi = {
  create: (columnId: string, title: string) =>
    api<CardSummary>(`/api/columns/${columnId}/cards`, { method: 'POST', body: { title } }),
  get: (cardId: string) => api<CardDetail>(`/api/cards/${cardId}`),
  update: (cardId: string, input: CardInput) =>
    api<CardDetail>(`/api/cards/${cardId}`, { method: 'PUT', body: input }),
  // index: hedef sütunda kartın önünde kaç kart olacağı (kendisi hariç)
  move: (cardId: string, columnId: string, index: number) =>
    api<void>(`/api/cards/${cardId}/move`, { method: 'PUT', body: { columnId, index } }),
  remove: (cardId: string) => api<void>(`/api/cards/${cardId}`, { method: 'DELETE' }),
  addComment: (cardId: string, content: string) =>
    api<Comment>(`/api/cards/${cardId}/comments`, { method: 'POST', body: { content } }),
  removeComment: (commentId: string) => api<void>(`/api/comments/${commentId}`, { method: 'DELETE' }),
}

export type ActivityType =
  | 'BoardCreated'
  | 'BoardUpdated'
  | 'MemberInvited'
  | 'MemberJoined'
  | 'MemberRemoved'
  | 'MemberLeft'
  | 'ColumnCreated'
  | 'ColumnRenamed'
  | 'ColumnMoved'
  | 'ColumnDeleted'
  | 'CardCreated'
  | 'CardUpdated'
  | 'CardMoved'
  | 'CardAssigned'
  | 'CardDeleted'
  | 'CommentAdded'

export type Activity = {
  id: string
  type: ActivityType
  actor: MemberRef
  // Kayıt anındaki adlar: cardTitle, columnName, fromColumn, toColumn...
  data: Record<string, string | null>
  createdAt: string
}

export const activitiesApi = {
  list: (boardId: string, before?: string) =>
    api<Activity[]>(`/api/boards/${boardId}/activities${before ? `?before=${encodeURIComponent(before)}` : ''}`),
}

export const invitationsApi = {
  preview: (token: string) => api<InvitationPreview>(`/api/invitations/${token}`),
  accept: (token: string) => api<{ boardId: string }>(`/api/invitations/${token}/accept`, { method: 'POST' }),
  decline: (token: string) => api<void>(`/api/invitations/${token}/decline`, { method: 'POST' }),
}
