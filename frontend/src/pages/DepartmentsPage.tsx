import { useState } from 'react'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Department } from '../api/types'
import { ErrorBanner } from '../components/ErrorBanner'

export function DepartmentsPage() {
  const api = useApi()
  const departments = useQuery<Department[]>('/api/v1/departments')
  const [newName, setNewName] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [error, setError] = useState<unknown>(null)

  if (departments.loading) return <p>Loading...</p>
  if (departments.error) return <ErrorBanner error={departments.error} />
  if (!departments.data) return null

  async function run(action: () => Promise<unknown>) {
    setError(null)
    try {
      await action()
      departments.reload()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section>
      <h2>Departments</h2>

      <form
        className="toolbar"
        onSubmit={(e) => {
          e.preventDefault()
          void run(async () => {
            await api.post('/api/v1/departments', { name: newName })
            setNewName('')
          })
        }}
      >
        <input placeholder="New department name" value={newName} onChange={(e) => setNewName(e.target.value)} />
        <button type="submit" className="primary">
          Add
        </button>
      </form>

      <ErrorBanner error={error} />

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Created</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {departments.data.map((d) => (
            <tr key={d.departmentId}>
              <td>
                {editingId === d.departmentId ? (
                  <input value={editName} onChange={(e) => setEditName(e.target.value)} />
                ) : (
                  d.name
                )}
              </td>
              <td>{new Date(d.createdAtUtc).toLocaleDateString()}</td>
              <td>
                <div className="row-actions">
                  {editingId === d.departmentId ? (
                    <>
                      <button
                        className="primary"
                        onClick={() =>
                          void run(async () => {
                            await api.put(`/api/v1/departments/${d.departmentId}`, { name: editName })
                            setEditingId(null)
                          })
                        }
                      >
                        Save
                      </button>
                      <button onClick={() => setEditingId(null)}>Cancel</button>
                    </>
                  ) : (
                    <>
                      <button
                        onClick={() => {
                          setEditingId(d.departmentId)
                          setEditName(d.name)
                        }}
                      >
                        Rename
                      </button>
                      <button
                        className="danger"
                        onClick={() => {
                          if (window.confirm(`Delete "${d.name}"?`)) {
                            void run(() => api.del(`/api/v1/departments/${d.departmentId}`))
                          }
                        }}
                      >
                        Delete
                      </button>
                    </>
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