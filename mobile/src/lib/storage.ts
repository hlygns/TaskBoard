import * as SecureStore from 'expo-secure-store'
import { Platform } from 'react-native'

// Telefonda şifreli depo (iOS Keychain / Android Keystore). Refresh token burada durur.
// expo-secure-store web'de çalışmaz; web sadece geliştirme sırasında tarayıcıda denemek için,
// orada localStorage'a düşüyoruz.
export const storage = {
  async get(key: string): Promise<string | null> {
    if (Platform.OS === 'web') {
      try {
        return localStorage.getItem(key)
      } catch {
        return null
      }
    }
    return SecureStore.getItemAsync(key)
  },

  async set(key: string, value: string): Promise<void> {
    if (Platform.OS === 'web') {
      try {
        localStorage.setItem(key, value)
      } catch {
        // Depolama kapalıysa oturum sadece bu açılışta geçerli olur.
      }
      return
    }
    await SecureStore.setItemAsync(key, value)
  },

  async remove(key: string): Promise<void> {
    if (Platform.OS === 'web') {
      try {
        localStorage.removeItem(key)
      } catch {
        // yok say
      }
      return
    }
    await SecureStore.deleteItemAsync(key)
  },
}
