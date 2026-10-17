import { useCallback, useEffect, useState } from 'react'
import { ApiError } from './http'
import { useApi } from './useApi'

export interface QueryState<T> {
  data: T | null
  error: ApiError | null
  loading: boolean // vrai uniquement pendant le premier chargement
  reload: () => void
}

export function useQuery<T>(path: string): QueryState<T> {
  const api = useApi()
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    api
      .get<T>(path)
      .then((result) => {
        if (cancelled) return
        setData(result)
        setError(null)
      })
      .catch((e: unknown) => {
        if (cancelled) return
        setError(e instanceof ApiError ? e : new ApiError(0, 'unexpected_error', 'Unexpected error.'))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [api, path, version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])

  return { data, error, loading, reload }
}