import { useState } from 'react'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Employee, Project, ProjectMember } from '../api/types'
import { ErrorBanner } from '../components/ErrorBanner'

interface ProjectDetailPageProps {
  projectId: string
  isManager: boolean
  onBack: () => void
}

export function ProjectDetailPage({ projectId, isManager, onBack }: ProjectDetailPageProps) {
  const api = useApi()
  const project = useQuery<Project>(`/api/v1/projects/${projectId}`)
  const members = useQuery<ProjectMember[]>(`/api/v1/projects/${projectId}/members`)
  const [renaming, setRenaming] = useState(false)
  const [name, setName] = useState('')
  const [error, setError] = useState<unknown>(null)

  const back = (
    <button className="back" onClick={onBack}>
      ← Back to projects
    </button>
  )

  if (project.loading || members.loading) return <p>Loading...</p>
  if (project.error || members.error) {
    return (
      <>
        {back}
        <ErrorBanner error={project.error ?? members.error} />
      </>
    )
  }
  if (!project.data || !members.data) return null

  const isActive = project.data.status === 'Active'
  const canManage = isManager && isActive

  async function run(action: () => Promise<unknown>) {
    setError(null)
    try {
      await action()
      project.reload()
      members.reload()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section>
      {back}

      <h2>
        {project.data.name} <span className={`badge ${isActive ? '' : 'archived'}`}>{project.data.status}</span>
      </h2>

      <dl className="details">
        <dt>Created</dt>
        <dd>{new Date(project.data.createdAtUtc).toLocaleDateString()}</dd>
        {project.data.archivedAtUtc && (
          <>
            <dt>Archived</dt>
            <dd>{new Date(project.data.archivedAtUtc).toLocaleDateString()}</dd>
          </>
        )}
      </dl>

      {canManage && (
        <div className="toolbar">
          {renaming ? (
            <>
              <input value={name} onChange={(e) => setName(e.target.value)} />
              <button
                className="primary"
                onClick={() =>
                  void run(async () => {
                    await api.put(`/api/v1/projects/${projectId}`, { name })
                    setRenaming(false)
                  })
                }
              >
                Save
              </button>
              <button onClick={() => setRenaming(false)}>Cancel</button>
            </>
          ) : (
            <button
              onClick={() => {
                setName(project.data?.name ?? '')
                setRenaming(true)
              }}
            >
              Rename
            </button>
          )}
          <button
            className="danger"
            onClick={() => {
              if (window.confirm('Archive this project? This cannot be undone.')) {
                void run(() => api.post(`/api/v1/projects/${projectId}/archive`))
              }
            }}
          >
            Archive
          </button>
        </div>
      )}

      <ErrorBanner error={error} />

      <h3>Members</h3>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Added</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {members.data.map((m) => (
            <tr key={m.employeeId} className={m.employeeIsActive ? '' : 'inactive'}>
              <td>{m.employeeName}</td>
              <td>{new Date(m.addedAtUtc).toLocaleDateString()}</td>
              <td>
                {canManage && (
                  <button
                    className="danger"
                    onClick={() => void run(() => api.del(`/api/v1/projects/${projectId}/members/${m.employeeId}`))}
                  >
                    Remove
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {members.data.length === 0 && <p>No members yet.</p>}

      {canManage && (
        <AddMemberForm
          projectId={projectId}
          memberIds={members.data.map((m) => m.employeeId)}
          onAdded={() => {
            project.reload()
            members.reload()
          }}
        />
      )}
    </section>
  )
}

// Composant séparé : la liste des employés (réservée aux Managers) n'est chargée que quand il est affiché.
function AddMemberForm({
  projectId,
  memberIds,
  onAdded,
}: {
  projectId: string
  memberIds: string[]
  onAdded: () => void
}) {
  const api = useApi()
  const employees = useQuery<Employee[]>('/api/v1/employees')
  const [employeeId, setEmployeeId] = useState('')
  const [error, setError] = useState<unknown>(null)

  if (employees.loading) return <p>Loading...</p>
  if (employees.error) return <ErrorBanner error={employees.error} />
  if (!employees.data) return null

  const candidates = employees.data.filter((e) => e.isActive && !memberIds.includes(e.employeeId))

  async function add() {
    setError(null)
    try {
      await api.post(`/api/v1/projects/${projectId}/members`, { employeeId })
      setEmployeeId('')
      onAdded()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <div className="panel">
      <h3>Add a member</h3>
      {candidates.length === 0 ? (
        <p>No employee available to add.</p>
      ) : (
        <div className="toolbar">
          <select value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
            <option value="" disabled>
              Select an employee…
            </option>
            {candidates.map((e) => (
              <option key={e.employeeId} value={e.employeeId}>
                {e.firstName} {e.lastName}
              </option>
            ))}
          </select>
          <button className="primary" disabled={!employeeId} onClick={() => void add()}>
            Add
          </button>
        </div>
      )}
      <ErrorBanner error={error} />
    </div>
  )
}