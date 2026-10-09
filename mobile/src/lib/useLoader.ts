import { useFocusEffect } from 'expo-router'
import { useCallback, useEffect, useRef, useState } from 'react'
import { errorMessage } from './api'

// Ekran her öne geldiğinde (sekmeye dönünce, alt ekrandan geri gelince) veriyi tazeler;
// aşağı çekerek yenileme (pull-to-refresh) için de "refresh" verir.
export function useLoader<T>(load: () => Promise<T>) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [refreshing, setRefreshing] = useState(false)
  // En güncel yükleme fonksiyonu; ekran her render'da yeni bir fonksiyon verse de odak efekti yeniden kurulmasın.
  const loadRef = useRef(load)
  useEffect(() => {
    loadRef.current = load
  })

  const run = useCallback(async () => {
    try {
      setData(await loadRef.current())
      setError(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }, [])

  useFocusEffect(
    useCallback(() => {
      run()
    }, [run]),
  )

  const refresh = useCallback(async () => {
    setRefreshing(true)
    await run()
    setRefreshing(false)
  }, [run])

  return { data, setData, error, refreshing, refresh, reload: run }
}
