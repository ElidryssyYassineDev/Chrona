const baseUrl: string = import.meta.env.VITE_API_URL

export interface FieldIssue {
  field: string
  issue: string
}

export class ApiError extends Error {
  status: number
  code: string
  details: FieldIssue[]

  constructor(status: number, code: string, message: string, details: FieldIssue[] = []) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.details = details
  }
}

export async function request<T>(
  token: string | undefined,
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      method,
      headers: {
        Authorization: `Bearer ${token ?? ''}`,
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    // fetch ne rejette que sur un échec réseau/CORS, jamais sur un 4xx/5xx.
    throw new ApiError(0, 'network_error', 'Cannot reach the server.')
  }

  if (response.ok) {
    return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
  }

  let code = `http_${response.status}`
  let message =
    response.status === 401
      ? 'Your session has expired.'
      : response.status === 403
        ? 'You are not allowed to do this.'
        : `Request failed (${response.status}).`
  let details: FieldIssue[] = []

  try {
    const payload = await response.json()
    if (payload?.error) {
      code = payload.error.code ?? code
      message = payload.error.message ?? message
      details = payload.error.details ?? []
    } else {
      message = payload?.message ?? payload?.detail ?? payload?.title ?? message
    }
  } catch {
    // Corps vide (401/403) : on garde le message par défaut.
  }

  throw new ApiError(response.status, code, message, details)
}