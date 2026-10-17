import { ApiError } from '../api/http'

export function ErrorBanner({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof ApiError ? error.message : 'Unexpected error.'
  return (
    <p role="alert" className="error">
      {message}
    </p>
  )
}