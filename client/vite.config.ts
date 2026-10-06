import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    // /api ile başlayan istekler .NET API'ye gider. Tarayıcı açısından her şey aynı
    // adresten (localhost:5173) geldiği için CORS ayarı gerekmez ve cookie'ler sorunsuz çalışır.
    proxy: {
      '/api': 'http://localhost:5009',
    },
  },
})
