import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../api/http'
import { useApi } from '../api/useApi'
import type { Department, Employee } from '../api/types'
import { ErrorBanner } from './ErrorBanner'

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

interface EmployeeFormProps {
  departments: Department[]
  employees: Employee[]
  initial: Employee | null
  onSaved: () => void
  onCancel: () => void
}

export function EmployeeForm({ departments, employees, initial, onSaved, onCancel }: EmployeeFormProps) {
  const api = useApi()
  const [subjectId, setSubjectId] = useState(initial?.keycloakSubjectId ?? '')
  const [firstName, setFirstName] = useState(initial?.firstName ?? '')
  const [lastName, setLastName] = useState(initial?.lastName ?? '')
  const [departmentId, setDepartmentId] = useState(initial?.departmentId ?? '')
  const [managerId, setManagerId] = useState(initial?.managerId ?? '')
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  // Candidats manager : actifs, hors soi-même ; on garde le manager actuel même désactivé pour ne pas l'effacer en silence.
  const managerOptions = employees.filter(
    (e) => e.employeeId !== initial?.employeeId && (e.isActive || e.employeeId === initial?.managerId),
  )

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (!initial && !GUID.test(subjectId.trim())) {
      setError(new ApiError(400, 'validation_error', 'Keycloak account ID must be a valid GUID.'))
      return
    }
    if (!departmentId) {
      setError(new ApiError(400, 'validation_error', 'Choose a department.'))
      return
    }

    setSaving(true)
    try {
      const body = { firstName, lastName, departmentId, managerId: managerId || null }
      if (initial) {
        await api.put(`/api/v1/employees/${initial.employeeId}`, body)
      } else {
        await api.post('/api/v1/employees', { ...body, keycloakSubjectId: subjectId.trim() })
      }
      onSaved()
    } catch (e) {
      setError(e)
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="panel" onSubmit={(e) => void submit(e)}>
      <h3>{initial ? 'Edit employee' : 'New employee'}</h3>
      <div className="form-grid">
        <label htmlFor="subject">Keycloak account ID</label>
        <input
          id="subject"
          value={subjectId}
          onChange={(e) => setSubjectId(e.target.value)}
          disabled={initial !== null}
          placeholder="User ID from the Keycloak console"
        />

        <label htmlFor="first">First name</label>
        <input id="first" value={firstName} onChange={(e) => setFirstName(e.target.value)} />

        <label htmlFor="last">Last name</label>
        <input id="last" value={lastName} onChange={(e) => setLastName(e.target.value)} />

        <label htmlFor="dept">Department</label>
        <select id="dept" value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}>
          <option value="" disabled>
            Select…
          </option>
          {departments.map((d) => (
            <option key={d.departmentId} value={d.departmentId}>
              {d.name}
            </option>
          ))}
        </select>

        <label htmlFor="manager">Manager</label>
        <select id="manager" value={managerId} onChange={(e) => setManagerId(e.target.value)}>
          <option value="">None</option>
          {managerOptions.map((m) => (
            <option key={m.employeeId} value={m.employeeId}>
              {m.firstName} {m.lastName}
            </option>
          ))}
        </select>
      </div>

      <ErrorBanner error={error} />

      <div className="form-actions">
        <button type="submit" className="primary" disabled={saving}>
          {saving ? 'Saving…' : 'Save'}
        </button>
        <button type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  )
}