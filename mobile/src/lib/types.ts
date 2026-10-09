// API'nin döndürdüğü veriler (web istemcisindeki client/src/api/boards.ts ile aynı sözleşme).

export type User = { id: string; email: string; fullName: string }

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: User
  refreshToken: string
}

export type BoardRole = 'Owner' | 'Member'
export type CardPriority = 'Low' | 'Medium' | 'High' | 'Urgent'
export type LabelColor =
  | 'slate'
  | 'red'
  | 'orange'
  | 'amber'
  | 'green'
  | 'teal'
  | 'sky'
  | 'indigo'
  | 'violet'
  | 'pink'

export type Label = { id: string; name: string; color: LabelColor }
export type MemberRef = { userId: string; fullName: string }

export type BoardSummary = {
  id: string
  name: string
  description: string | null
  myRole: BoardRole
  memberCount: number
  cardCount: number
  completedCount: number
  overdueCount: number
}

export type CardSummary = {
  id: string
  title: string
  priority: CardPriority
  dueDate: string | null
  assignee: MemberRef | null
  commentCount: number
  hasDescription: boolean
  isCompleted: boolean
  checklistDone: number
  checklistTotal: number
  labelIds: string[]
}

export type Column = { id: string; name: string; cards: CardSummary[] }

export type BoardDetail = {
  id: string
  name: string
  description: string | null
  myRole: BoardRole
  members: { userId: string; fullName: string; role: BoardRole }[]
  columns: Column[]
  labels: Label[]
}

export type ChecklistItem = { id: string; text: string; isDone: boolean }
export type Comment = { id: string; content: string; author: MemberRef; createdAt: string }

export type CardDetail = {
  id: string
  columnId: string
  columnName: string
  title: string
  description: string | null
  priority: CardPriority
  dueDate: string | null
  assignee: MemberRef | null
  comments: Comment[]
  completedAt: string | null
  labelIds: string[]
  checklist: ChecklistItem[]
}

export type MyTask = {
  cardId: string
  title: string
  boardId: string
  boardName: string
  columnName: string
  dueDate: string | null
  priority: CardPriority
  checklistDone: number
  checklistTotal: number
  labels: Label[]
}
