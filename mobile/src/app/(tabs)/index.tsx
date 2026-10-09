import { router } from 'expo-router'
import { Alert, Pressable, RefreshControl, SectionList, StyleSheet, Text, View } from 'react-native'
import { CheckCircle, Chip, EmptyState, ErrorText, LabelChip, Loading } from '@/components/ui'
import { api, errorMessage } from '@/lib/api'
import { colors, dateKey, formatDue, localDateKey, priorityMeta } from '@/lib/format'
import type { MyTask } from '@/lib/types'
import { useLoader } from '@/lib/useLoader'

const groups = [
  { key: 'overdue', title: 'Gecikmiş', color: colors.danger },
  { key: 'today', title: 'Bugün', color: colors.primary },
  { key: 'tomorrow', title: 'Yarın', color: colors.text },
  { key: 'week', title: 'Bu hafta', color: colors.text },
  { key: 'later', title: 'Daha sonra', color: colors.text },
  { key: 'noDate', title: 'Tarihsiz (bana atanan)', color: colors.textMuted },
] as const

// Gruplama telefonun kendi saatine göre: "bugün" herkes için farklı olabilir.
function groupOf(task: MyTask) {
  if (!task.dueDate) return 'noDate'
  const due = dateKey(task.dueDate)
  if (due < localDateKey(0)) return 'overdue'
  if (due === localDateKey(0)) return 'today'
  if (due === localDateKey(1)) return 'tomorrow'
  if (due <= localDateKey(7)) return 'week'
  return 'later'
}

// Ana ekran: tüm panolardaki açık işler, son tarihe göre gruplu.
export default function MyTasksScreen() {
  const { data: tasks, setData, error, refreshing, refresh } = useLoader(() => api<MyTask[]>('/api/me/tasks'))

  async function complete(task: MyTask) {
    // İyimser: listeden hemen düşür, sunucu reddederse geri getir.
    setData((list) => list?.filter((t) => t.cardId !== task.cardId) ?? null)
    try {
      await api(`/api/cards/${task.cardId}/complete`, { method: 'PUT', body: { completed: true } })
    } catch (err) {
      setData((list) => (list ? [...list, task] : list))
      Alert.alert('Kaydedilemedi', errorMessage(err))
    }
  }

  if (!tasks && !error) return <Loading />

  const sections = groups
    .map((g) => ({ ...g, data: (tasks ?? []).filter((t) => groupOf(t) === g.key) }))
    .filter((s) => s.data.length > 0)

  return (
    <SectionList
      sections={sections}
      keyExtractor={(t) => t.cardId}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
      contentContainerStyle={{ paddingBottom: 24 }}
      stickySectionHeadersEnabled={false}
      ListHeaderComponent={error ? <View style={{ padding: 16 }}><ErrorText message={error} /></View> : null}
      ListEmptyComponent={
        tasks ? (
          <EmptyState title="Bekleyen işin yok 🎉" text="Kartlara son tarih verdiğinde ya da sana atandığında burada görünür." />
        ) : null
      }
      renderSectionHeader={({ section }) => (
        <Text style={[s.sectionTitle, { color: section.color }]}>
          {section.title} <Text style={s.sectionCount}>{section.data.length}</Text>
        </Text>
      )}
      renderItem={({ item, section }) => (
        <Pressable
          onPress={() => router.push({ pathname: '/card/[id]', params: { id: item.cardId, boardId: item.boardId } })}
          accessibilityRole="button"
          style={({ pressed }) => [s.row, pressed && { backgroundColor: colors.surfaceMuted }]}
        >
          <CheckCircle checked={false} onPress={() => complete(item)} />
          <View style={{ flex: 1, gap: 4 }}>
            <Text style={s.title}>{item.title}</Text>
            <Text style={s.meta}>
              {item.boardName} · {item.columnName}
            </Text>
            <View style={s.chips}>
              {item.labels.map((l) => (
                <LabelChip key={l.id} label={l} />
              ))}
              {item.priority !== 'Medium' && (
                <Chip text={priorityMeta(item.priority).label} fg={priorityMeta(item.priority).fg} bg={priorityMeta(item.priority).bg} />
              )}
              {item.checklistTotal > 0 && <Chip text={`☑ ${item.checklistDone}/${item.checklistTotal}`} />}
            </View>
          </View>
          {item.dueDate && (
            <Text style={[s.due, section.key === 'overdue' && { color: colors.danger, fontWeight: '700' }]}>
              {formatDue(item.dueDate)}
            </Text>
          )}
        </Pressable>
      )}
    />
  )
}

const s = StyleSheet.create({
  sectionTitle: { fontSize: 15, fontWeight: '700', paddingHorizontal: 16, paddingTop: 20, paddingBottom: 8 },
  sectionCount: { color: colors.textFaint, fontWeight: '500' },
  row: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 12,
    marginHorizontal: 16,
    marginBottom: 8,
    padding: 14,
    borderRadius: 14,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  title: { fontSize: 16, fontWeight: '600', color: colors.text },
  meta: { fontSize: 13, color: colors.textMuted },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 6 },
  due: { fontSize: 13, color: colors.textMuted },
})
