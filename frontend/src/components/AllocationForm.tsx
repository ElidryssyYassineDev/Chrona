import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../api/http'
import { useApi } from '../api/useApi'
import type { Allocation, Capacity, Employee, Project } from '../api/types'
import { ErrorBanner } from './ErrorBanner'

interface AllocationFormProps {
  employees: Employee[]
  projects: Project[]
  initial: Allocation | null
  onSaved: () => void
  onCancel: () => void
}

export function AllocationForm({ employees, projects, initial, onSaved, onCancel }: AllocationFormProps) {
  const api = useApi()
  const [employeeId, setEmployeeId] = useState(initial?.employeeId ?? '')
  const [projectId, setProjectId] = useState(initial?.projectId ?? '')
  const [periodStart, setPeriodStart] = useState(initial?.periodStart ?? '')
  const [periodEnd, setPeriodEnd] = useState(initial?.periodEnd ?? '')
  const [percentage, setPercentage] = useState(initial ? String(initial.percentage) : '')
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)
  const [capacity, setCapacity] = useState<{ key: string; data: Capacity } | null>(null)

  // Clé de la requête de capacité courante ; null tant que le formulaire n'est pas assez rempli.
  const capacityKey =
    employeeId && periodStart && periodEnd && periodStart <= periodEnd
      ? `${employeeId}|${periodStart}|${periodEnd}|${initial?.allocationId ?? ''}`
      : null

  useEffect(() => {
    if (capacityKey === null) return
    let cancelled = false
    const exclude = initial ? `&excludeAllocationId=${initial.allocationId}` : ''
    api
      .get<Capacity>(
        `/api/v1/allocations/capacity?employeeId=${employeeId}&periodStart=${periodStart}&periodEnd=${periodEnd}${exclude}`,
      )
      .then((data) => {
        if (!cancelled) setCapacity({ key: capacityKey, data })
      })
      .catch(() => {
        // L'aperçu est un confort : le serveur reste l'autorité au moment de l'enregistrement.
      })
    return () => {
      cancelled = true
    }
  }, [api, capacityKey, employeeId, periodStart, periodEnd, initial])

  const currentCapacity = capacity && capacity.key === capacityKey ? capacity.data : null
  const requested = Number(percentage)
  const overCapacity = currentCapacity !== null && percentage !== '' && requested > currentCapacity.availablePercentage

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (!initial && (!employeeId || !projectId)) {
      setError(new ApiError(400, 'validation_error', 'Choose an employee and a project.'))
      return
    }

    setSaving(true)
    try {
      const body = { periodStart, periodEnd, percentage: Number(percentage) }
      if (initial) {
        await api.put(`/api/v1/allocations/${initial.allocationId}`, body)
      } else {
        await api.post('/api/v1/allocations', { employeeId, projectId, ...body })
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
      <h3>{initial ? 'Modify allocation' : 'New allocation'}</h3>
      <div className="form-grid">
        <label htmlFor="alloc-employee">Employee</label>
        <select
          id="alloc-employee"
          value={employeeId}
          onChange={(e) => setEmployeeId(e.target.value)}
          disabled={initial !== null}
        >
          <option value="" disabled>
            Select…
          </option>
          {employees
            .filter((e) => e.isActive || e.employeeId === initial?.employeeId)
            .map((e) => (
              <option key={e.employeeId} value={e.employeeId}>
                {e.firstName} {e.lastName}
              </option>
            ))}
        </select>

        <label htmlFor="alloc-project">Project</label>
        <select
          id="alloc-project"
          value={projectId}
          onChange={(e) => setProjectId(e.target.value)}
          disabled={initial !== null}
        >
          <option value="" disabled>
            Select…
          </option>
          {projects
            .filter((p) => p.status === 'Active' || p.projectId === initial?.projectId)
            .map((p) => (
              <option key={p.projectId} value={p.projectId}>
                {p.name}
              </option>
            ))}
        </select>

        <label htmlFor="alloc-start">From</label>
        <input id="alloc-start" type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value)} />

        <label htmlFor="alloc-end">To (included)</label>
        <input id="alloc-end" type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)} />

        <label htmlFor="alloc-pct">Percentage</label>
        <input
          id="alloc-pct"
          type="number"
          min="0.01"
          max="100"
          step="0.01"
          value={percentage}
          onChange={(e) => setPercentage(e.target.value)}
        />
      </div>

      {currentCapacity && (
        <p className={overCapacity ? 'error' : 'hint'}>
          Already allocated in this period (peak): {currentCapacity.peakAllocatedPercentage}% · Available:{' '}
          {currentCapacity.availablePercentage}%{overCapacity && ' — this exceeds the available capacity.'}
        </p>
      )}

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