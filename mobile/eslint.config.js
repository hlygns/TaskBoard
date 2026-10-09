// Expo'nun önerdiği ESLint ayarları (flat config). Çalıştırmak için: npx expo lint
const { defineConfig } = require('eslint/config')
const expoConfig = require('eslint-config-expo/flat')

module.exports = defineConfig([
  expoConfig,
  {
    ignores: ['dist/*'],
  },
])
