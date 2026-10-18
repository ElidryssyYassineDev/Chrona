import { useState } from 'react'
import type { FormEvent } from 'react'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Project } from '../api/types'
import { ErrorBanner } from '../components/ErrorBanner'
import { ProjectDetailPage } from './ProjectDetailPage'

export function ProjectsPage({ isManager }: { isManager: boolean }) {
  const api = useApi()
  const projects = useQuery<Project[]>('/api/v1/projects')
  const [openId, setOpenId] = useState<string | null>(null)
  const [newName, setNewName] = useState('')
  const [showArchived, setShowArchived] = useState(false)
  const [error, setError] = useState<unknown>(null)

  // Navigation liste -> détail par état local (pas de routeur dans ce MVP).
  if (openId !== null) {
    return (
      <ProjectDetailPage
        key={openId}
        projectId={openId}
        isManager={isManager}
        onBack={() => {
          setOpenId(null)
          projects.reload()
        }}
      />
    )
  }

  if (projects.loading) return <p>Loading...</p>
  if (projects.error) return <ErrorBanner error={projects.error} />
  if (!projects.data) return null

  const visible = projects.data.filter((p) => showArchived || p.status === 'Active')

  async function create(event: FormEvent) {
    event.preventDefault()
    setError(null)
    try {
      await api.post<Project>('/api/v1/projects', { name: newName })
      setNewName('')
      projects.reload()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section>
      <h2>Projects</h2>

      <div className="toolbar">
        {isManager && (
          <form className="toolbar" onSubmit={(e) => void create(e)}>
            <input placeholder="New project name" value={newName} onChange={(e) => setNewName(e.target.value)} />
            <button type="submit" className="primary">
              Create
            </button>
          </form>
        )}
        <span className="spacer" />
        <label>
          <input type="checkbox" checked={showArchived} onChange={(e) => setShowArchived(e.target.checked)} /> Show
          archived
        </label>
      </div>

      <ErrorBanner error={error} />

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Members</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          {visible.map((p) => (
            <tr key={p.projectId} className={p.status === 'Active' ? '' : 'inactive'}>
              <td>
                <button className="linklike" onClick={() => setOpenId(p.projectId)}>
                  {p.name}
                </button>
              </td>
              <td>
                <span className={`badge ${p.status === 'Archived' ? 'archived' : ''}`}>{p.status}</span>
              </td>
              <td>{p.memberCount}</td>
              <td>{new Date(p.createdAtUtc).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}