import { router, Stack, useLocalSearchParams } from 'expo-router'
import { useState } from 'react'
import {
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native'
import { CheckCircle, Chip, EmptyState, ErrorText, LabelChip, Loading } from '@/components/ui'
import { api, errorMessage } from '@/lib/api'
import { boardColor, colors, formatDue, isOverdue, priorityMeta } from '@/lib/format'
import type { BoardDetail, CardSummary, Label } from '@/lib/types'
import { useLoader } from '@/lib/useLoader'

// Telefonda sütunlar yan yana sığmaz: üstte sütun sekmeleri, altta seçilen sütunun kartları.
// Kartı başka sütuna taşımak kart detayındaki "Sütun" seçimiyle yapılır (sürükle-bırak yerine).
export default function BoardScreen() {
  const { id } = useLocalSearchParams<{ id: string }>()
  const { data: board, setData, error, refreshing, refresh } = useLoader(() => api<BoardDetail>(`/api/boards/${id}`))
  const [columnId, setColumnId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [hideCompleted, setHideCompleted] = useState(false)

  if (!board && !error) return <Loading />
  if (!board) return <View style={{ padding: 16 }}><ErrorText message={error} /></View>

  const column = board.columns.find((c) => c.id === columnId) ?? board.columns[0]
  const labels = new Map(board.labels.map((l) => [l.id, l]))
  const cards = column ? column.cards.filter((c) => !(hideCompleted && c.isCompleted)) : []
  const color = boardColor(board.id)

  function updateCard(cardId: string, change: Partial<CardSummary>) {
    setData((b) =>
      b && {
        ...b,
        columns: b.columns.map((col) => ({
          ...col,
          cards: col.cards.map((c) => (c.id === cardId ? { ...c, ...change } : c)),
        })),
      },
    )
  }

  async function toggleComplete(card: CardSummary) {
    updateCard(card.id, { isCompleted: !card.isCompleted })
    try {
      await api(`/api/cards/${card.id}/complete`, { method: 'PUT', body: { completed: !card.isCompleted } })
    } catch (err) {
      updateCard(card.id, { isCompleted: card.isCompleted })
      Alert.alert('Kaydedilemedi', errorMessage(err))
    }
  }

  async function addCard() {
    if (!column || !title.trim()) return
    try {
      const card = await api<CardSummary>(`/api/columns/${column.id}/cards`, { method: 'POST', body: { title: title.trim() } })
      setData((b) => b && { ...b, columns: b.columns.map((c) => (c.id === column.id ? { ...c, cards: [...c.cards, card] } : c)) })
      setTitle('')
    } catch (err) {
      Alert.alert('Kart eklenemedi', errorMessage(err))
    }
  }

  const total = board.columns.reduce((n, c) => n + c.cards.length, 0)
  const done = board.columns.reduce((n, c) => n + c.cards.filter((x) => x.isCompleted).length, 0)

  return (
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined} keyboardVerticalOffset={90}>
      <Stack.Screen options={{ title: board.name }} />

      <View style={s.header}>
        <View style={[s.dot, { backgroundColor: color }]} />
        <Text style={s.summary}>
          {total} kart · {done} tamamlandı
        </Text>
        <Pressable onPress={() => setHideCompleted((v) => !v)} hitSlop={8} accessibilityRole="switch" accessibilityState={{ checked: hideCompleted }} style={{ marginLeft: 'auto' }}>
          <Text style={[s.filter, hideCompleted && { color: colors.primary }]}>
            {hideCompleted ? '✓ Tamamlananlar gizli' : 'Tamamlananları gizle'}
          </Text>
        </Pressable>
      </View>

      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={s.tabs} style={{ flexGrow: 0 }}>
        {board.columns.map((c) => {
          const active = c.id === column?.id
          return (
            <Pressable
              key={c.id}
              onPress={() => setColumnId(c.id)}
              accessibilityRole="tab"
              accessibilityState={{ selected: active }}
              style={[s.tab, active && { backgroundColor: colors.text, borderColor: colors.text }]}
            >
              <Text style={[s.tabText, active && { color: '#fff' }]}>
                {c.name} <Text style={{ opacity: 0.6 }}>{c.cards.length}</Text>
              </Text>
            </Pressable>
          )
        })}
      </ScrollView>

      <FlatList
        data={cards}
        keyExtractor={(c) => c.id}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
        contentContainerStyle={{ padding: 16, gap: 10, flexGrow: 1 }}
        ListEmptyComponent={<EmptyState title="Bu sütunda kart yok" text="Aşağıdan yeni kart ekleyebilirsin." />}
        renderItem={({ item }) => (
          <CardRow
            card={item}
            labels={labels}
            onToggle={() => toggleComplete(item)}
            onOpen={() => router.push({ pathname: '/card/[id]', params: { id: item.id, boardId: board.id } })}
          />
        )}
      />

      {column && (
        <View style={s.addBar}>
          <TextInput
            value={title}
            onChangeText={setTitle}
            onSubmitEditing={addCard}
            placeholder={`"${column.name}" sütununa kart ekle…`}
            placeholderTextColor={colors.textFaint}
            returnKeyType="done"
            style={s.addInput}
          />
          <Pressable onPress={addCard} disabled={!title.trim()} accessibilityRole="button" style={[s.addButton, !title.trim() && { opacity: 0.5 }]}>
            <Text style={s.addButtonText}>Ekle</Text>
          </Pressable>
        </View>
      )}
    </KeyboardAvoidingView>
  )
}

function CardRow({
  card,
  labels,
  onToggle,
  onOpen,
}: {
  card: CardSummary
  labels: Map<string, Label>
  onToggle: () => void
  onOpen: () => void
}) {
  const priority = priorityMeta(card.priority)
  const overdue = !card.isCompleted && card.dueDate !== null && isOverdue(card.dueDate)
  const cardLabels = card.labelIds.map((id) => labels.get(id)).filter((l) => l !== undefined)

  return (
    <Pressable onPress={onOpen} accessibilityRole="button" style={({ pressed }) => [s.card, pressed && { backgroundColor: colors.surfaceMuted }, card.isCompleted && { opacity: 0.6 }]}>
      {cardLabels.length > 0 && (
        <View style={s.chips}>
          {cardLabels.map((l) => (
            <LabelChip key={l.id} label={l} />
          ))}
        </View>
      )}
      <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}>
        <CheckCircle checked={card.isCompleted} onPress={onToggle} size={22} />
        <Text style={[s.cardTitle, card.isCompleted && s.strike]}>{card.title}</Text>
      </View>
      <View style={s.chips}>
        {card.priority !== 'Medium' && <Chip text={priority.label} fg={priority.fg} bg={priority.bg} />}
        {card.dueDate && (
          <Chip text={`📅 ${formatDue(card.dueDate)}`} fg={overdue ? colors.danger : colors.textMuted} bg={overdue ? colors.dangerSoft : colors.surfaceMuted} />
        )}
        {card.checklistTotal > 0 && <Chip text={`☑ ${card.checklistDone}/${card.checklistTotal}`} />}
        {card.commentCount > 0 && <Chip text={`💬 ${card.commentCount}`} />}
        {card.assignee && <Chip text={card.assignee.fullName.split(' ')[0]} fg={colors.primary} bg={colors.primarySoft} />}
      </View>
    </Pressable>
  )
}

const s = StyleSheet.create({
  header: { flexDirection: 'row', alignItems: 'center', gap: 8, paddingHorizontal: 16, paddingTop: 12 },
  dot: { width: 10, height: 10, borderRadius: 5 },
  summary: { fontSize: 14, color: colors.textMuted },
  filter: { fontSize: 13, fontWeight: '600', color: colors.textMuted },
  tabs: { paddingHorizontal: 16, paddingVertical: 12, gap: 8 },
  tab: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
  },
  tabText: { fontSize: 14, fontWeight: '600', color: '#334155' },
  card: {
    padding: 14,
    gap: 8,
    borderRadius: 14,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardTitle: { flex: 1, fontSize: 16, color: colors.text, lineHeight: 22 },
  strike: { textDecorationLine: 'line-through', color: colors.textMuted },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 6 },
  addBar: {
    flexDirection: 'row',
    gap: 8,
    padding: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    backgroundColor: colors.surface,
  },
  addInput: {
    flex: 1,
    minHeight: 44,
    borderRadius: 12,
    paddingHorizontal: 14,
    backgroundColor: colors.surfaceMuted,
    fontSize: 15,
    color: colors.text,
  },
  addButton: { paddingHorizontal: 18, borderRadius: 12, backgroundColor: colors.primary, justifyContent: 'center' },
  addButtonText: { color: '#fff', fontWeight: '700', fontSize: 15 },
})
