import { useState } from 'react'
import type { FormEvent } from 'react'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Allocation, Timesheet } from '../api/types'
import { ErrorBanner } from '../components/ErrorBanner'

interface TimesheetDetailPageProps {
  timesheetId: string
  onBack: () => void
}

export function TimesheetDetailPage({ timesheetId, onBack }: TimesheetDetailPageProps) {
  const api = useApi()
  const timesheet = useQuery<Timesheet>(`/api/v1/timesheets/${timesheetId}`)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editDate, setEditDate] = useState('')
  const [editHours, setEditHours] = useState('')
  const [error, setError] = useState<unknown>(null)

  const back = (
    <button className="back" onClick={onBack}>
      ← Back to timesheets
    </button>
  )

  if (timesheet.loading) return <p>Loading...</p>
  if (timesheet.error) {
    return (
      <>
        {back}
        <ErrorBanner error={timesheet.error} />
      </>
    )
  }
  if (!timesheet.data) return null

  const sheet = timesheet.data
  const isDraft = sheet.status === 'Draft'

  async function run(action: () => Promise<unknown>) {
    setError(null)
    try {
      await action()
      timesheet.reload()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section>
      {back}

      <h2>
        {sheet.periodStart} → {sheet.periodEnd} <span className={`badge ${isDraft ? '' : 'archived'}`}>{sheet.status}</span>
      </h2>

      <dl className="details">
        <dt>Total hours</dt>
        <dd>{sheet.totalHours}</dd>
        {sheet.lastSubmittedAtUtc && (
          <>
            <dt>Submitted</dt>
            <dd>{new Date(sheet.lastSubmittedAtUtc).toLocaleString()}</dd>
          </>
        )}
      </dl>

      <ErrorBanner error={error} />

      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Project</th>
            <th>Hours</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {sheet.entries.map((entry) => (
            <tr key={entry.timeEntryId}>
              <td>
                {editingId === entry.timeEntryId ? (
                  <input
                    type="date"
                    min={sheet.periodStart}
                    max={sheet.periodEnd}
                    value={editDate}
                    onChange={(e) => setEditDate(e.target.value)}
                  />
                ) : (
                  entry.entryDate
                )}
              </td>
              <td>{entry.projectName}</td>
              <td>
                {editingId === entry.timeEntryId ? (
                  <input
                    type="number"
                    min="0.01"
                    max="24"
                    step="0.01"
                    value={editHours}
                    onChange={(e) => setEditHours(e.target.value)}
                  />
                ) : (
                  entry.hours
                )}
              </td>
              <td>
                {isDraft && (
                  <div className="row-actions">
                    {editingId === entry.timeEntryId ? (
                      <>
                        <button
                          className="primary"
                          onClick={() =>
                            void run(async () => {
                              await api.put(`/api/v1/timesheets/${timesheetId}/time-entries/${entry.timeEntryId}`, {
                                entryDate: editDate,
                                hours: Number(editHours),
                              })
                              setEditingId(null)
                            })
                          }
                        >
                          Save
                        </button>
                        <button onClick={() => setEditingId(null)}>Cancel</button>
                      </>
                    ) : (
                      <button
                        onClick={() => {
                          setEditingId(entry.timeEntryId)
                          setEditDate(entry.entryDate)
                          setEditHours(String(entry.hours))
                        }}
                      >
                        Edit
                      </button>
                    )}
                  </div>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {sheet.entries.length === 0 && <p>No time entry yet.</p>}

      {isDraft && (
        <>
          <AddEntryForm sheet={sheet} onAdded={timesheet.reload} />

          <div className="toolbar">
            <button
              className="primary"
              disabled={sheet.entries.length === 0}
              onClick={() => {
                if (window.confirm('Submit this timesheet? It becomes read-only until a manager decides.')) {
                  void run(() => api.post(`/api/v1/timesheets/${timesheetId}/submit`))
                }
              }}
            >
              Submit timesheet
            </button>
          </div>
        </>
      )}
    </section>
  )
}

function AddEntryForm({ sheet, onAdded }: { sheet: Timesheet; onAdded: () => void }) {
  const api = useApi()
  const allocations = useQuery<Allocation[]>('/api/v1/allocations')
  const [allocationId, setAllocationId] = useState('')
  const [entryDate, setEntryDate] = useState('')
  const [hours, setHours] = useState('')
  const [error, setError] = useState<unknown>(null)

  if (allocations.loading) return <p>Loading...</p>
  if (allocations.error) return <ErrorBanner error={allocations.error} />
  if (!allocations.data) return null

  // Allocations proposées : celles de cette personne, actives, qui chevauchent la période de la feuille.
  const usable = allocations.data.filter(
    (a) =>
      a.employeeId === sheet.employeeId &&
      a.status === 'Active' &&
      a.periodStart <= sheet.periodEnd &&
      a.periodEnd >= sheet.periodStart,
  )
  const selected = usable.find((a) => a.allocationId === allocationId)
  const minDate = selected && selected.periodStart > sheet.periodStart ? selected.periodStart : sheet.periodStart
  const maxDate = selected && selected.periodEnd < sheet.periodEnd ? selected.periodEnd : sheet.periodEnd

  async function add(event: FormEvent) {
    event.preventDefault()
    setError(null)
    try {
      await api.post(`/api/v1/timesheets/${sheet.timesheetId}/time-entries`, {
        allocationId,
        entryDate,
        hours: Number(hours),
      })
      setEntryDate('')
      setHours('')
      onAdded()
    } catch (e) {
      setError(e)
    }
  }

  return (
    <form className="panel" onSubmit={(e) => void add(e)}>
      <h3>Add a time entry</h3>
      {usable.length === 0 ? (
        <p>No active allocation covers this period: ask a manager to allocate you to a project first.</p>
      ) : (
        <div className="form-grid">
          <label htmlFor="entry-allocation">Allocation</label>
          <select id="entry-allocation" value={allocationId} onChange={(e) => setAllocationId(e.target.value)}>
            <option value="" disabled>
              Select…
            </option>
            {usable.map((a) => (
              <option key={a.allocationId} value={a.allocationId}>
                {a.projectName} ({a.periodStart} → {a.periodEnd})
              </option>
            ))}
          </select>

          <label htmlFor="entry-date">Date</label>
          <input
            id="entry-date"
            type="date"
            min={minDate}
            max={maxDate}
            value={entryDate}
            onChange={(e) => setEntryDate(e.target.value)}
          />

          <label htmlFor="entry-hours">Hours</label>
          <input
            id="entry-hours"
            type="number"
            min="0.01"
            max="24"
            step="0.01"
            value={hours}
            onChange={(e) => setHours(e.target.value)}
          />
        </div>
      )}
      <ErrorBanner error={error} />
      {usable.length > 0 && (
        <div className="form-actions">
          <button type="submit" className="primary" disabled={!allocationId || !entryDate || !hours}>
            Add entry
          </button>
        </div>
      )}
    </form>
  )
}