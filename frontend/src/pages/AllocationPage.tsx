import { useState } from 'react'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Allocation, AllocationDetail, Employee, Project } from '../api/types'
import { AllocationForm } from '../components/AllocationForm'
import { ErrorBanner } from '../components/ErrorBanner'

interface Filters {
  employeeId: string
  projectId: string
}

export function AllocationsPage({ isManager }: { isManager: boolean }) {
  const api = useApi()
  const [filters, setFilters] = useState<Filters>({ employeeId: '', projectId: '' })
  const [editing, setEditing] = useState<Allocation | 'new' | null>(null)
  const [historyId, setHistoryId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)

  const params = new URLSearchParams()
  if (filters.employeeId) params.set('employeeId', filters.employeeId)
  if (filters.projectId) params.set('projectId', filters.projectId)
  const queryString = params.toString()
  const allocations = useQuery<Allocation[]>(`/api/v1/allocations${queryString ? `?${queryString}` : ''}`)

  if (allocations.loading) return <p>Loading...</p>
  if (allocations.error) return <ErrorBanner error={allocations.error} />
  if (!allocations.data) return null

  async function cancel(allocation: Allocation) {
    if (!window.confirm(`Cancel the allocation of ${allocation.employeeName} on ${allocation.projectName}?`)) return
    setActionError(null)
    try {
      await api.post(`/api/v1/allocations/${allocation.allocationId}/cancel`)
      allocations.reload()
    } catch (e) {
      setActionError(e)
    }
  }

  return (
    <section>
      <h2>{isManager ? 'Allocations' : 'My allocations'}</h2>

      {isManager && (
        <ManagerPanel
          filters={filters}
          onFilters={setFilters}
          editing={editing}
          onEditing={setEditing}
          onSaved={allocations.reload}
        />
      )}

      <ErrorBanner error={actionError} />

      <table>
        <thead>
          <tr>
            <th>Employee</th>
            <th>Project</th>
            <th>Period</th>
            <th>%</th>
            <th>Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {allocations.data.map((a) => (
            <tr key={a.allocationId} className={a.status === 'Active' ? '' : 'inactive'}>
              <td>{a.employeeName}</td>
              <td>{a.projectName}</td>
              <td>
                {a.periodStart} → {a.periodEnd}
              </td>
              <td>{a.percentage}%</td>
              <td>
                <span className={`badge ${a.status === 'Cancelled' ? 'archived' : ''}`}>{a.status}</span>
              </td>
              <td>
                <div className="row-actions">
                  <button onClick={() => setHistoryId(historyId === a.allocationId ? null : a.allocationId)}>History</button>
                  {isManager && a.status === 'Active' && (
                    <>
                      <button onClick={() => setEditing(a)}>Modify</button>
                      <button className="danger" onClick={() => void cancel(a)}>
                        Cancel
                      </button>
                    </>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {allocations.data.length === 0 && <p>No allocation.</p>}

      {historyId !== null && <HistoryPanel key={historyId} allocationId={historyId} />}
    </section>
  )
}

// Composant séparé : la liste des employés (réservée aux Managers) n'est chargée que pour eux.
function ManagerPanel({
  filters,
  onFilters,
  editing,
  onEditing,
  onSaved,
}: {
  filters: Filters
  onFilters: (filters: Filters) => void
  editing: Allocation | 'new' | null
  onEditing: (editing: Allocation | 'new' | null) => void
  onSaved: () => void
}) {
  const employees = useQuery<Employee[]>('/api/v1/employees')
  const projects = useQuery<Project[]>('/api/v1/projects')

  if (employees.loading || projects.loading) return <p>Loading...</p>
  if (employees.error) return <ErrorBanner error={employees.error} />
  if (projects.error) return <ErrorBanner error={projects.error} />
  if (!employees.data || !projects.data) return null

  return (
    <>
      <div className="toolbar">
        <button className="primary" onClick={() => onEditing('new')}>
          New allocation
        </button>
        <span className="spacer" />
        <select value={filters.employeeId} onChange={(e) => onFilters({ ...filters, employeeId: e.target.value })}>
          <option value="">All employees</option>
          {employees.data.map((e) => (
            <option key={e.employeeId} value={e.employeeId}>
              {e.firstName} {e.lastName}
            </option>
          ))}
        </select>
        <select value={filters.projectId} onChange={(e) => onFilters({ ...filters, projectId: e.target.value })}>
          <option value="">All projects</option>
          {projects.data.map((p) => (
            <option key={p.projectId} value={p.projectId}>
              {p.name}
            </option>
          ))}
        </select>
      </div>

      {editing !== null && (
        <AllocationForm
          key={editing === 'new' ? 'new' : editing.allocationId}
          employees={employees.data}
          projects={projects.data}
          initial={editing === 'new' ? null : editing}
          onCancel={() => onEditing(null)}
          onSaved={() => {
            onEditing(null)
            onSaved()
          }}
        />
      )}
    </>
  )
}

function HistoryPanel({ allocationId }: { allocationId: string }) {
  const detail = useQuery<AllocationDetail>(`/api/v1/allocations/${allocationId}`)

  if (detail.loading) return <p>Loading...</p>
  if (detail.error) return <ErrorBanner error={detail.error} />
  if (!detail.data) return null

  return (
    <div className="panel">
      <h3>History</h3>
      <table>
        <thead>
          <tr>
            <th>When</th>
            <th>Change</th>
            <th>From</th>
            <th>To</th>
          </tr>
        </thead>
        <tbody>
          {detail.data.history.map((h, index) => (
            <tr key={index}>
              <td>{new Date(h.changedAtUtc).toLocaleString()}</td>
              <td>{h.changeType}</td>
              <td>{h.oldValue ?? '—'}</td>
              <td>{h.newValue ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}