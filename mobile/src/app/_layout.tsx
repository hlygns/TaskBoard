import { Stack } from 'expo-router'
import { StatusBar } from 'expo-status-bar'
import { Loading } from '@/components/ui'
import { AuthProvider, useAuth } from '@/lib/auth'
import { colors } from '@/lib/format'

export default function RootLayout() {
  return (
    <AuthProvider>
      <StatusBar style="dark" />
      <RootStack />
    </AuthProvider>
  )
}

function RootStack() {
  const { user, loading } = useAuth()

  // Kayıtlı oturum kontrol edilirken (refresh token ile) kısa bir bekleme ekranı.
  if (loading) return <Loading />

  // Stack.Protected: koşulu sağlanmayan ekranlara gidilemez; oturum açılınca/kapanınca
  // kullanıcı otomatik olarak erişebildiği ilk ekrana yönlendirilir.
  return (
    <Stack
      screenOptions={{
        headerTintColor: colors.primary,
        headerTitleStyle: { color: colors.text, fontWeight: '700' },
        contentStyle: { backgroundColor: colors.background },
      }}
    >
      <Stack.Protected guard={!user}>
        <Stack.Screen name="login" options={{ headerShown: false }} />
        <Stack.Screen name="register" options={{ title: 'Hesap oluştur' }} />
      </Stack.Protected>

      <Stack.Protected guard={!!user}>
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
        <Stack.Screen name="board/[id]" options={{ title: '' }} />
        <Stack.Screen name="card/[id]" options={{ title: 'Kart' }} />
      </Stack.Protected>
    </Stack>
  )
}
