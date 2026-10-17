import { useState } from 'react'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Department, Employee } from '../api/types'
import { EmployeeForm } from '../components/EmployeeForm'
import { ErrorBanner } from '../components/ErrorBanner'

export function EmployeesPage() {
  const api = useApi()
  const employees = useQuery<Employee[]>('/api/v1/employees')
  const departments = useQuery<Department[]>('/api/v1/departments')
  const [showInactive, setShowInactive] = useState(false)
  const [editing, setEditing] = useState<Employee | 'new' | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)

  if (employees.loading || departments.loading) return <p>Loading...</p>
  if (employees.error) return <ErrorBanner error={employees.error} />
  if (departments.error) return <ErrorBanner error={departments.error} />
  if (!employees.data || !departments.data) return null

  const visible = employees.data.filter((e) => showInactive || e.isActive)

  async function deactivate(employee: Employee) {
    if (!window.confirm(`Deactivate ${employee.firstName} ${employee.lastName}?`)) return
    setActionError(null)
    try {
      await api.post(`/api/v1/employees/${employee.employeeId}/deactivate`)
      employees.reload()
    } catch (e) {
      setActionError(e)
    }
  }

  return (
    <section>
      <h2>Employees</h2>

      <div className="toolbar">
        <button className="primary" onClick={() => setEditing('new')}>
          New employee
        </button>
        <span className="spacer" />
        <label>
          <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} /> Show
          inactive
        </label>
      </div>

      {editing !== null && (
        <EmployeeForm
          key={editing === 'new' ? 'new' : editing.employeeId}
          departments={departments.data}
          employees={employees.data}
          initial={editing === 'new' ? null : editing}
          onCancel={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            employees.reload()
          }}
        />
      )}

      <ErrorBanner error={actionError} />

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Department</th>
            <th>Manager</th>
            <th>Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {visible.map((e) => (
            <tr key={e.employeeId} className={e.isActive ? '' : 'inactive'}>
              <td>
                {e.firstName} {e.lastName}
              </td>
              <td>{e.departmentName}</td>
              <td>{e.managerName ?? '—'}</td>
              <td>{e.isActive ? 'Active' : 'Inactive'}</td>
              <td>
                <div className="row-actions">
                  <button onClick={() => setEditing(e)}>Edit</button>
                  {e.isActive && (
                    <button className="danger" onClick={() => void deactivate(e)}>
                      Deactivate
                    </button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}