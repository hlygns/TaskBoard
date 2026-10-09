import { StyleSheet, Text, View } from 'react-native'
import { Button } from '@/components/ui'
import { getServerUrl } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { colors } from '@/lib/format'

export default function ProfileScreen() {
  const { user, logout } = useAuth()
  if (!user) return null

  const initials = user.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0].toLocaleUpperCase('tr'))
    .join('')

  return (
    <View style={{ padding: 16, gap: 16 }}>
      <View style={s.card}>
        <View style={s.avatar}>
          <Text style={s.avatarText}>{initials}</Text>
        </View>
        <Text style={s.name}>{user.fullName}</Text>
        <Text style={s.email}>{user.email}</Text>
      </View>

      <View style={s.card}>
        <Text style={s.label}>Bağlı sunucu</Text>
        <Text style={s.server}>{getServerUrl()}</Text>
        <Text style={s.hint}>
          {"Telefon, aynı Wi-Fi'daki bilgisayarında çalışan API'ye bağlanır. Adres değişirse çıkış yapıp giriş ekranından \"Sunucu\" ayarını güncelle."}
        </Text>
      </View>

      <Button title="Çıkış yap" variant="danger" onPress={logout} />
    </View>
  )
}

const s = StyleSheet.create({
  card: {
    padding: 20,
    borderRadius: 18,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    gap: 6,
  },
  avatar: {
    width: 72,
    height: 72,
    borderRadius: 36,
    backgroundColor: colors.success,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  avatarText: { color: '#fff', fontSize: 26, fontWeight: '700' },
  name: { fontSize: 20, fontWeight: '700', color: colors.text },
  email: { fontSize: 15, color: colors.textMuted },
  label: { fontSize: 13, fontWeight: '600', color: colors.textMuted },
  server: { fontSize: 16, fontWeight: '600', color: colors.text },
  hint: { fontSize: 13, color: colors.textFaint, textAlign: 'center', marginTop: 4 },
})
