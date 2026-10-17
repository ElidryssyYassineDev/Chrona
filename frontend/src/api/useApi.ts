import { useMemo } from 'react'
import { useAuth } from 'react-oidc-context'
import { request } from './http'

// Lie le jeton d'accès courant aux appels : les composants n'ont jamais à toucher au jeton.
export function useApi() {
  const token = useAuth().user?.access_token

  return useMemo(
    () => ({
      get: <T>(path: string) => request<T>(token, 'GET', path),
      post: <T>(path: string, body?: unknown) => request<T>(token, 'POST', path, body),
      put: <T>(path: string, body: unknown) => request<T>(token, 'PUT', path, body),
      del: (path: string) => request<void>(token, 'DELETE', path),
    }),
    [token],
  )
}