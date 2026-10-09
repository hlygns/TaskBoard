import { router, Stack, useLocalSearchParams } from 'expo-router'
import { useState, type ReactNode } from 'react'
import { Alert, KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native'
import { Button, CheckCircle, ErrorText, Loading, Toggle } from '@/components/ui'
import { api, errorMessage } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { colors, dateKey, formatDue, labelColors, localDateKey, priorities, timeAgo, toDueDate } from '@/lib/format'
import type { BoardDetail, CardDetail, CardPriority, ChecklistItem, Comment } from '@/lib/types'
import { useLoader } from '@/lib/useLoader'

type Loaded = { card: CardDetail; board: BoardDetail }

const dueOptions = [
  { label: 'Bugün', offset: 0 },
  { label: 'Yarın', offset: 1 },
  { label: 'Gelecek hafta', offset: 7 },
]

// Telefonda "Kaydet" düğmesi yok: her değişiklik anında kaydedilir.
export default function CardScreen() {
  const { id, boardId } = useLocalSearchParams<{ id: string; boardId: string }>()
  const { user } = useAuth()
  const { data, setData, error } = useLoader<Loaded>(async () => {
    const [card, board] = await Promise.all([
      api<CardDetail>(`/api/cards/${id}`),
      api<BoardDetail>(`/api/boards/${boardId}`),
    ])
    return { card, board }
  })
  const [title, setTitle] = useState<string | null>(null)
  const [description, setDescription] = useState<string | null>(null)
  const [newItem, setNewItem] = useState('')
  const [comment, setComment] = useState('')

  if (!data && !error) return <Loading />
  if (!data) return <View style={{ padding: 16 }}><ErrorText message={error} /></View>

  const { card, board } = data
  const setCard = (change: Partial<CardDetail>) => setData((d) => d && { ...d, card: { ...d.card, ...change } })

  // Hata olursa kullanıcıya göster ve kartı sunucudan geri yükle.
  async function run(action: () => Promise<unknown>) {
    try {
      await action()
    } catch (err) {
      Alert.alert('İşlem başarısız', errorMessage(err))
      const fresh = await api<CardDetail>(`/api/cards/${id}`).catch(() => null)
      if (fresh) setCard(fresh)
    }
  }

  // Başlık, açıklama, tarih ve öncelik tek bir PUT ile güncellenir (sunucuda tam güncelleme).
  function saveDetails(change: Partial<Pick<CardDetail, 'title' | 'description' | 'dueDate' | 'priority'>>) {
    const next = { ...card, ...change }
    setCard(change)
    return run(() =>
      api(`/api/cards/${id}`, {
        method: 'PUT',
        body: {
          title: next.title,
          description: next.description,
          dueDate: next.dueDate,
          priority: next.priority,
          assigneeId: next.assignee?.userId ?? null,
        },
      }),
    )
  }

  function commitTitle() {
    const value = (title ?? card.title).trim()
    setTitle(null)
    if (value && value !== card.title) saveDetails({ title: value })
  }

  function commitDescription() {
    const value = (description ?? card.description ?? '').trim()
    setDescription(null)
    if (value !== (card.description ?? '')) saveDetails({ description: value || null })
  }

  const toggleComplete = () => {
    const completed = card.completedAt === null
    setCard({ completedAt: completed ? new Date().toISOString() : null })
    run(() => api(`/api/cards/${id}/complete`, { method: 'PUT', body: { completed } }))
  }

  const moveTo = (columnId: string) => {
    const target = board.columns.find((c) => c.id === columnId)!
    setCard({ columnId, columnName: target.name })
    // Büyük index: sunucu sütunun sonuna yerleştirir.
    run(() => api(`/api/cards/${id}/move`, { method: 'PUT', body: { columnId, index: 100000 } }))
  }

  const toggleLabel = (labelId: string) => {
    const labelIds = card.labelIds.includes(labelId) ? card.labelIds.filter((x) => x !== labelId) : [...card.labelIds, labelId]
    setCard({ labelIds })
    run(() => api(`/api/cards/${id}/labels`, { method: 'PUT', body: { labelIds } }))
  }

  const toggleItem = (item: ChecklistItem) => {
    setCard({ checklist: card.checklist.map((i) => (i.id === item.id ? { ...i, isDone: !i.isDone } : i)) })
    run(() => api(`/api/checklist/${item.id}`, { method: 'PATCH', body: { isDone: !item.isDone } }))
  }

  const removeItem = (item: ChecklistItem) => {
    setCard({ checklist: card.checklist.filter((i) => i.id !== item.id) })
    run(() => api(`/api/checklist/${item.id}`, { method: 'DELETE' }))
  }

  async function addItem() {
    if (!newItem.trim()) return
    await run(async () => {
      const item = await api<ChecklistItem>(`/api/cards/${id}/checklist`, { method: 'POST', body: { text: newItem.trim() } })
      setCard({ checklist: [...card.checklist, item] })
      setNewItem('')
    })
  }

  async function addComment() {
    if (!comment.trim()) return
    await run(async () => {
      const added = await api<Comment>(`/api/cards/${id}/comments`, { method: 'POST', body: { content: comment.trim() } })
      setCard({ comments: [...card.comments, added] })
      setComment('')
    })
  }

  function confirm(title: string, message: string, onYes: () => void) {
    if (Platform.OS === 'web') {
      if (window.confirm(message)) onYes()
      return
    }
    Alert.alert(title, message, [
      { text: 'Vazgeç', style: 'cancel' },
      { text: 'Evet', style: 'destructive', onPress: onYes },
    ])
  }

  const archive = () =>
    confirm('Arşivle', 'Kart panodan kaldırılsın mı? Web\'deki Arşiv panelinden geri alabilirsin.', () =>
      run(async () => {
        await api(`/api/cards/${id}/archive`, { method: 'PUT', body: { archived: true } })
        router.back()
      }),
    )

  const remove = () =>
    confirm('Kartı sil', 'Kart kalıcı olarak silinsin mi?', () =>
      run(async () => {
        await api(`/api/cards/${id}`, { method: 'DELETE' })
        router.back()
      }),
    )

  const done = card.checklist.filter((i) => i.isDone).length
  const percent = card.checklist.length ? Math.round((done / card.checklist.length) * 100) : 0
  const due = card.dueDate ? dateKey(card.dueDate) : null

  return (
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined} keyboardVerticalOffset={90}>
      <Stack.Screen options={{ title: board.name }} />
      <ScrollView contentContainerStyle={{ padding: 16, gap: 22, paddingBottom: 48 }} keyboardShouldPersistTaps="handled">
        {/* Başlık ve tamamla */}
        <View style={{ flexDirection: 'row', gap: 12, alignItems: 'flex-start' }}>
          <CheckCircle checked={card.completedAt !== null} onPress={toggleComplete} size={30} />
          <TextInput
            value={title ?? card.title}
            onChangeText={setTitle}
            onBlur={commitTitle}
            onSubmitEditing={commitTitle}
            multiline
            blurOnSubmit
            style={[s.title, card.completedAt && s.strike]}
            accessibilityLabel="Başlık"
          />
        </View>

        <Section title="Sütun">
          <View style={s.wrap}>
            {board.columns.map((c) => (
              <Toggle key={c.id} text={c.name} active={c.id === card.columnId} onPress={() => moveTo(c.id)} color={colors.text} />
            ))}
          </View>
        </Section>

        <Section title={`Son tarih${card.dueDate ? ` · ${formatDue(card.dueDate)}` : ''}`}>
          <View style={s.wrap}>
            {dueOptions.map((o) => (
              <Toggle
                key={o.label}
                text={o.label}
                active={due === localDateKey(o.offset)}
                onPress={() => saveDetails({ dueDate: toDueDate(localDateKey(o.offset)) })}
              />
            ))}
            {card.dueDate && <Toggle text="Kaldır" active={false} onPress={() => saveDetails({ dueDate: null })} />}
          </View>
        </Section>

        <Section title="Öncelik">
          <View style={s.wrap}>
            {priorities.map((p) => (
              <Toggle
                key={p.value}
                text={p.label}
                active={card.priority === p.value}
                color={p.fg}
                onPress={() => saveDetails({ priority: p.value as CardPriority })}
              />
            ))}
          </View>
        </Section>

        {board.labels.length > 0 && (
          <Section title="Etiketler">
            <View style={s.wrap}>
              {board.labels.map((l) => {
                const selected = card.labelIds.includes(l.id)
                const c = labelColors[l.color]
                return (
                  <Pressable
                    key={l.id}
                    onPress={() => toggleLabel(l.id)}
                    accessibilityRole="button"
                    accessibilityState={{ selected }}
                    style={[s.label, { backgroundColor: c.bg, borderColor: selected ? c.fg : 'transparent', opacity: selected ? 1 : 0.55 }]}
                  >
                    <Text style={{ color: c.fg, fontWeight: '600' }}>
                      {selected ? '✓ ' : ''}
                      {l.name}
                    </Text>
                  </Pressable>
                )
              })}
            </View>
          </Section>
        )}

        <Section title="Açıklama">
          <TextInput
            value={description ?? card.description ?? ''}
            onChangeText={setDescription}
            onBlur={commitDescription}
            multiline
            placeholder="Daha ayrıntılı bir açıklama ekle…"
            placeholderTextColor={colors.textFaint}
            style={s.textarea}
          />
        </Section>

        <Section title={`Alt görevler${card.checklist.length ? ` · ${done}/${card.checklist.length}` : ''}`}>
          {card.checklist.length > 0 && (
            <View style={s.track}>
              <View style={[s.bar, { width: `${percent}%`, backgroundColor: percent === 100 ? colors.success : colors.primary }]} />
            </View>
          )}
          {card.checklist.map((item) => (
            <View key={item.id} style={s.item}>
              <CheckCircle checked={item.isDone} onPress={() => toggleItem(item)} size={22} />
              <Text style={[s.itemText, item.isDone && s.strike]}>{item.text}</Text>
              <Pressable onPress={() => removeItem(item)} hitSlop={10} accessibilityLabel="Alt görevi sil">
                <Text style={{ color: colors.textFaint, fontSize: 16 }}>✕</Text>
              </Pressable>
            </View>
          ))}
          <View style={s.inline}>
            <TextInput
              value={newItem}
              onChangeText={setNewItem}
              onSubmitEditing={addItem}
              placeholder="Alt görev ekle…"
              placeholderTextColor={colors.textFaint}
              style={s.inlineInput}
            />
            <Button title="Ekle" onPress={addItem} disabled={!newItem.trim()} style={{ minHeight: 44 }} />
          </View>
        </Section>

        <Section title={`Yorumlar (${card.comments.length})`}>
          <View style={s.inline}>
            <TextInput
              value={comment}
              onChangeText={setComment}
              placeholder="Yorum yaz…"
              placeholderTextColor={colors.textFaint}
              multiline
              style={s.inlineInput}
            />
            <Button title="Gönder" onPress={addComment} disabled={!comment.trim()} style={{ minHeight: 44 }} />
          </View>
          {[...card.comments].reverse().map((c) => (
            <View key={c.id} style={s.comment}>
              <Text style={s.commentAuthor}>
                {c.author.userId === user?.id ? 'Sen' : c.author.fullName} <Text style={s.commentTime}>· {timeAgo(c.createdAt)}</Text>
              </Text>
              <Text style={s.commentText}>{c.content}</Text>
            </View>
          ))}
        </Section>

        <View style={{ flexDirection: 'row', gap: 10 }}>
          <Button title="Arşivle" variant="secondary" onPress={archive} style={{ flex: 1 }} />
          <Button title="Kartı sil" variant="danger" onPress={remove} style={{ flex: 1 }} />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <View style={{ gap: 10 }}>
      <Text style={s.sectionTitle}>{title}</Text>
      {children}
    </View>
  )
}

const s = StyleSheet.create({
  title: { flex: 1, fontSize: 22, fontWeight: '700', color: colors.text, padding: 0 },
  strike: { textDecorationLine: 'line-through', color: colors.textMuted },
  sectionTitle: { fontSize: 13, fontWeight: '700', color: colors.textMuted, textTransform: 'uppercase', letterSpacing: 0.5 },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  label: { paddingHorizontal: 12, paddingVertical: 7, borderRadius: 999, borderWidth: 2 },
  textarea: {
    minHeight: 90,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    padding: 12,
    fontSize: 15,
    color: colors.text,
    backgroundColor: colors.surface,
    textAlignVertical: 'top',
  },
  track: { height: 6, borderRadius: 3, backgroundColor: colors.surfaceMuted, overflow: 'hidden' },
  bar: { height: '100%', borderRadius: 3 },
  item: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingVertical: 4 },
  itemText: { flex: 1, fontSize: 15, color: colors.text },
  inline: { flexDirection: 'row', gap: 8, alignItems: 'flex-start' },
  inlineInput: {
    flex: 1,
    minHeight: 44,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 15,
    color: colors.text,
    backgroundColor: colors.surface,
  },
  comment: { padding: 12, borderRadius: 12, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, gap: 4 },
  commentAuthor: { fontSize: 14, fontWeight: '600', color: colors.text },
  commentTime: { fontWeight: '400', color: colors.textFaint },
  commentText: { fontSize: 15, color: '#334155' },
})
