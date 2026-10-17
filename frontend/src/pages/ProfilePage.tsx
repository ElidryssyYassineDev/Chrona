import type { MyProfile } from '../api/types'

export function ProfilePage({ profile }: { profile: MyProfile }) {
  const { employee, roles } = profile
  return (
    <section>
      <h2>
        {employee.firstName} {employee.lastName}
      </h2>
      <dl className="details">
        <dt>Employee ID</dt>
        <dd>{employee.employeeId}</dd>
        <dt>Department</dt>
        <dd>{employee.departmentName}</dd>
        <dt>Manager</dt>
        <dd>{employee.managerName ?? '—'}</dd>
        <dt>Active</dt>
        <dd>{employee.isActive ? 'Yes' : 'No'}</dd>
        <dt>Roles</dt>
        <dd>{roles.join(', ') || '—'}</dd>
      </dl>
    </section>
  )
}