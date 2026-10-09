import { router } from 'expo-router'
import { useState } from 'react'
import { FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native'
import { Button, Chip, EmptyState, ErrorText, Field, Loading } from '@/components/ui'
import { api, errorMessage } from '@/lib/api'
import { boardColor, colors } from '@/lib/format'
import type { BoardDetail, BoardSummary } from '@/lib/types'
import { useLoader } from '@/lib/useLoader'

const templates = [
  { id: 'personal', name: 'Kişisel' },
  { id: 'basic', name: 'Basit' },
  { id: 'software', name: 'Yazılım' },
  { id: 'school', name: 'Ders' },
]

export default function BoardsScreen() {
  const { data: boards, error, refreshing, refresh } = useLoader(() => api<BoardSummary[]>('/api/boards'))
  const [creating, setCreating] = useState(false)

  if (!boards && !error) return <Loading />

  return (
    <FlatList
      data={boards ?? []}
      keyExtractor={(b) => b.id}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
      contentContainerStyle={{ padding: 16, gap: 12 }}
      ListHeaderComponent={
        <View style={{ gap: 12 }}>
          <ErrorText message={error} />
          {creating ? (
            <NewBoardForm onCancel={() => setCreating(false)} />
          ) : (
            <Button title="+ Yeni pano" onPress={() => setCreating(true)} />
          )}
        </View>
      }
      ListEmptyComponent={boards ? <EmptyState title="Henüz bir panon yok" text="İlk panonu oluşturarak başla." /> : null}
      renderItem={({ item }) => <BoardCard board={item} />}
    />
  )
}

function BoardCard({ board }: { board: BoardSummary }) {
  const color = boardColor(board.id)
  const percent = board.cardCount === 0 ? 0 : Math.round((board.completedCount / board.cardCount) * 100)

  return (
    <Pressable
      onPress={() => router.push({ pathname: '/board/[id]', params: { id: board.id } })}
      accessibilityRole="button"
      style={({ pressed }) => [s.card, pressed && { opacity: 0.85 }]}
    >
      <View style={[s.band, { backgroundColor: color }]}>
        <View style={s.initial}>
          <Text style={s.initialText}>{board.name.trim().charAt(0).toLocaleUpperCase('tr')}</Text>
        </View>
        <Text style={s.role}>{board.myRole === 'Owner' ? 'Sahip' : 'Üye'}</Text>
      </View>
      <View style={{ padding: 16, gap: 8 }}>
        <Text style={s.name}>{board.name}</Text>
        {board.description && <Text style={s.desc}>{board.description}</Text>}
        <View style={s.progressRow}>
          <Text style={s.desc}>
            {board.cardCount === 0 ? 'Henüz kart yok' : `${board.completedCount}/${board.cardCount} tamamlandı`}
          </Text>
          {board.cardCount > 0 && <Text style={s.percent}>%{percent}</Text>}
        </View>
        <View style={s.track}>
          <View style={[s.bar, { width: `${percent}%`, backgroundColor: color }]} />
        </View>
        <View style={{ flexDirection: 'row', gap: 6 }}>
          <Chip text={`👥 ${board.memberCount} üye`} />
          {board.overdueCount > 0 && <Chip text={`⏰ ${board.overdueCount} gecikmiş`} fg={colors.danger} bg={colors.dangerSoft} />}
        </View>
      </View>
    </Pressable>
  )
}

function NewBoardForm({ onCancel }: { onCancel: () => void }) {
  const [name, setName] = useState('')
  const [template, setTemplate] = useState('personal')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function create() {
    setBusy(true)
    try {
      const board = await api<BoardDetail>('/api/boards', { method: 'POST', body: { name, description: null, template } })
      onCancel()
      router.push({ pathname: '/board/[id]', params: { id: board.id } })
    } catch (err) {
      setError(errorMessage(err))
      setBusy(false)
    }
  }

  return (
    <View style={s.form}>
      <Field label="Pano adı" value={name} onChangeText={setName} placeholder="Örn. Ev işleri" autoFocus />
      <Text style={s.formLabel}>Şablon</Text>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        {templates.map((t) => (
          <Pressable
            key={t.id}
            onPress={() => setTemplate(t.id)}
            accessibilityRole="radio"
            accessibilityState={{ selected: template === t.id }}
            style={[s.template, template === t.id && { borderColor: colors.primary, backgroundColor: colors.primarySoft }]}
          >
            <Text style={[s.templateText, template === t.id && { color: colors.primary }]}>{t.name}</Text>
          </Pressable>
        ))}
      </View>
      <ErrorText message={error} />
      <View style={{ flexDirection: 'row', gap: 8 }}>
        <Button title="Vazgeç" variant="secondary" onPress={onCancel} style={{ flex: 1 }} />
        <Button title="Oluştur" onPress={create} loading={busy} disabled={!name.trim()} style={{ flex: 1 }} />
      </View>
    </View>
  )
}

const s = StyleSheet.create({
  card: {
    borderRadius: 18,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: 'hidden',
  },
  band: { height: 64, justifyContent: 'flex-end', paddingHorizontal: 16 },
  initial: {
    position: 'absolute',
    left: 16,
    bottom: -18,
    width: 44,
    height: 44,
    borderRadius: 12,
    backgroundColor: '#fff',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 3,
    borderColor: '#fff',
  },
  initialText: { fontSize: 20, fontWeight: '800', color: colors.text },
  role: {
    position: 'absolute',
    top: 10,
    right: 12,
    backgroundColor: 'rgba(255,255,255,0.9)',
    paddingHorizontal: 10,
    paddingVertical: 2,
    borderRadius: 999,
    fontSize: 12,
    fontWeight: '600',
    color: '#334155',
    overflow: 'hidden',
  },
  name: { fontSize: 18, fontWeight: '700', color: colors.text, marginTop: 14 },
  desc: { fontSize: 14, color: colors.textMuted },
  progressRow: { flexDirection: 'row', justifyContent: 'space-between' },
  percent: { fontSize: 14, fontWeight: '600', color: '#334155' },
  track: { height: 8, borderRadius: 4, backgroundColor: colors.surfaceMuted, overflow: 'hidden' },
  bar: { height: '100%', borderRadius: 4 },
  form: { gap: 12, padding: 16, borderRadius: 16, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border },
  formLabel: { fontSize: 14, fontWeight: '600', color: '#334155' },
  template: { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 10, borderWidth: 1, borderColor: colors.border },
  templateText: { fontSize: 14, fontWeight: '600', color: '#334155' },
})
