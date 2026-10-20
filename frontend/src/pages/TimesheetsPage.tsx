import { useState } from 'react'
import { currentWeek } from '../dates'
import { useApi } from '../api/useApi'
import { useQuery } from '../api/useQuery'
import type { Timesheet, TimesheetSummary } from '../api/types'
import { ErrorBanner } from '../components/ErrorBanner'
import { TimesheetDetailPage } from './TimesheetDetailPage'

export function TimesheetsPage() {
  const api = useApi()
  const timesheets = useQuery<TimesheetSummary[]>('/api/v1/timesheets')
  const [openId, setOpenId] = useState<string | null>(null)
  const [error, setError] = useState<unknown>(null)

  // Navigation liste -> détail par état local (pas de routeur dans ce MVP).
  if (openId !== null) {
    return (
      <TimesheetDetailPage
        key={openId}
        timesheetId={openId}
        onBack={() => {
          setOpenId(null)
          timesheets.reload()
        }}
      />
    )
  }

  if (timesheets.loading) return <p>Loading...</p>
  if (timesheets.error) return <ErrorBanner error={timesheets.error} />
  if (!timesheets.data) return null

  // L'API crée la feuille ou renvoie celle qui existe déjà pour cette période : l'appel est sans risque.
  async function openCurrentWeek() {
    setError(null)
    const week = currentWeek()
    try {
      const timesheet = await api.post<Timesheet>('/api/v1/timesheets', { periodStart: week.start, periodEnd: week.end })
      setOpenId(timesheet.timesheetId)
    } catch (e) {
      setError(e)
    }
  }

  return (
    <section>
      <h2>Timesheets</h2>

      <div className="toolbar">
        <button className="primary" onClick={() => void openCurrentWeek()}>
          Open this week's timesheet
        </button>
      </div>

      <ErrorBanner error={error} />

      <table>
        <thead>
          <tr>
            <th>Period</th>
            <th>Status</th>
            <th>Entries</th>
            <th>Hours</th>
            <th>Submitted</th>
          </tr>
        </thead>
        <tbody>
          {timesheets.data.map((t) => (
            <tr key={t.timesheetId}>
              <td>
                <button className="linklike" onClick={() => setOpenId(t.timesheetId)}>
                  {t.periodStart} → {t.periodEnd}
                </button>
              </td>
              <td>
                <span className={`badge ${t.status === 'Draft' ? '' : 'archived'}`}>{t.status}</span>
              </td>
              <td>{t.entryCount}</td>
              <td>{t.totalHours}</td>
              <td>{t.lastSubmittedAtUtc ? new Date(t.lastSubmittedAtUtc).toLocaleString() : '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {timesheets.data.length === 0 && <p>No timesheet yet.</p>}
    </section>
  )
}