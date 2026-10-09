import { Link } from 'expo-router'
import { useState } from 'react'
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { Button, ErrorText, Field } from '@/components/ui'
import { errorMessage, getServerUrl, saveServerUrl } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { colors } from '@/lib/format'

export default function LoginScreen() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [server, setServer] = useState(getServerUrl())
  const [showServer, setShowServer] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleLogin() {
    setError(null)
    setBusy(true)
    try {
      await saveServerUrl(server)
      await login(email.trim(), password)
    } catch (err) {
      setError(errorMessage(err))
      setBusy(false)
    }
  }

  return (
    <SafeAreaView style={{ flex: 1 }}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView contentContainerStyle={s.container} keyboardShouldPersistTaps="handled">
          <View style={s.brand}>
            <View style={s.logo}>
              <Text style={s.logoText}>T</Text>
            </View>
            <Text style={s.title}>TaskBoard</Text>
            <Text style={s.subtitle}>Panolarına kaldığın yerden devam et.</Text>
          </View>

          <View style={{ gap: 14 }}>
            <Field
              label="E-posta"
              value={email}
              onChangeText={setEmail}
              autoCapitalize="none"
              autoComplete="email"
              keyboardType="email-address"
              placeholder="ornek@mail.com"
            />
            <Field
              label="Şifre"
              value={password}
              onChangeText={setPassword}
              secureTextEntry
              autoComplete="password"
              onSubmitEditing={handleLogin}
            />

            {showServer && (
              <Field
                label="Sunucu adresi"
                value={server}
                onChangeText={setServer}
                autoCapitalize="none"
                autoCorrect={false}
                keyboardType="url"
                placeholder="http://192.168.1.20:5009"
              />
            )}

            <ErrorText message={error} />
            <Button title="Giriş yap" onPress={handleLogin} loading={busy} disabled={!email || !password} />

            <Pressable onPress={() => setShowServer((v) => !v)} hitSlop={8}>
              <Text style={s.serverLink}>
                {showServer ? 'Sunucu ayarını gizle' : `Sunucu: ${server.replace(/^https?:\/\//, '')} · değiştir`}
              </Text>
            </Pressable>

            <View style={s.footer}>
              <Text style={{ color: colors.textMuted }}>Hesabın yok mu? </Text>
              <Link href="/register" style={s.link}>
                Kayıt ol
              </Link>
            </View>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  )
}

const s = StyleSheet.create({
  container: { flexGrow: 1, justifyContent: 'center', padding: 24, gap: 32 },
  brand: { alignItems: 'center', gap: 8 },
  logo: {
    width: 64,
    height: 64,
    borderRadius: 20,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoText: { color: '#fff', fontSize: 30, fontWeight: '800' },
  title: { fontSize: 30, fontWeight: '800', color: colors.text },
  subtitle: { fontSize: 15, color: colors.textMuted },
  serverLink: { textAlign: 'center', color: colors.textFaint, fontSize: 13 },
  footer: { flexDirection: 'row', justifyContent: 'center', marginTop: 4 },
  link: { color: colors.primary, fontWeight: '600' },
})
