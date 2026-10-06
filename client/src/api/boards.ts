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

export type Column = {
  id: string
  name: string
  position: number
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

export const invitationsApi = {
  preview: (token: string) => api<InvitationPreview>(`/api/invitations/${token}`),
  accept: (token: string) => api<{ boardId: string }>(`/api/invitations/${token}/accept`, { method: 'POST' }),
  decline: (token: string) => api<void>(`/api/invitations/${token}/decline`, { method: 'POST' }),
}
