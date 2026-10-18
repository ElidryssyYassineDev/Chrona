import './App.css'
import { useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { useQuery } from './api/useQuery'
import type { MyProfile } from './api/types'
import { DepartmentsPage } from './pages/DepartmentsPage'
import { EmployeesPage } from './pages/EmployeesPage'
import { ProfilePage } from './pages/ProfilePage'
import { ProjectsPage } from './pages/ProjectsPage'

type Page = 'profile' | 'employees' | 'departments' | 'projects'

function App() {
  const auth = useAuth()

  if (auth.isLoading) return <p className="center">Loading...</p>

  if (!auth.isAuthenticated) {
    return (
      <div className="center">
        <h1>Chrona</h1>
        <button className="primary" onClick={() => auth.signinRedirect()}>
          Login
        </button>
      </div>
    )
  }

  return <Shell />
}

function Shell() {
  const auth = useAuth()
  const me = useQuery<MyProfile>('/api/v1/employees/me')
  const [page, setPage] = useState<Page>('profile')

  if (me.loading) return <p className="center">Loading employee...</p>

  if (me.error) {
    const message =
      me.error.status === 401
        ? 'Session expired.'
        : me.error.status === 404
          ? 'No employee record exists for this account.'
          : me.error.message
    return (
      <div className="center">
        <p role="alert">{message}</p>
        <button onClick={() => auth.signoutRedirect()}>Logout</button>
      </div>
    )
  }

  if (!me.data) return null

  const isManager = me.data.roles.includes('Manager')

  return (
    <>
      <header className="topbar">
        <strong>Chrona</strong>
        <nav>
          <button className={page === 'profile' ? 'active' : ''} onClick={() => setPage('profile')}>
            Profile
          </button>

             <button className={page === 'projects' ? 'active' : ''} onClick={() => setPage('projects')}>
                Projects
             </button>
          {isManager && (
            <>
              <button className={page === 'employees' ? 'active' : ''} onClick={() => setPage('employees')}>
                Employees
              </button>
              <button className={page === 'departments' ? 'active' : ''} onClick={() => setPage('departments')}>
                Departments
              </button>
            </>
          )}
        </nav>
        <span className="who">{me.data.employee.firstName}</span>
        <button onClick={() => auth.signoutRedirect()}>Logout</button>
      </header>

      <main>
        {page === 'profile' && <ProfilePage profile={me.data} />}
        {isManager && page === 'employees' && <EmployeesPage />}
        {isManager && page === 'departments' && <DepartmentsPage />}
        {page === 'projects' && <ProjectsPage isManager={isManager} />}
      </main>
    </>
  )
}

export default App