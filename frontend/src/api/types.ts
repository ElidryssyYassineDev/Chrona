export interface Department {
  departmentId: string
  name: string
  createdAtUtc: string
}

export interface Employee {
  employeeId: string
  keycloakSubjectId: string
  firstName: string
  lastName: string
  departmentId: string
  departmentName: string
  managerId: string | null
  managerName: string | null
  isActive: boolean
  createdAtUtc: string
  deactivatedAtUtc: string | null
}

export interface MyProfile {
  employee: Employee
  roles: string[]
}

export interface Project {
  projectId: string
  name: string
  status: 'Active' | 'Archived'
  createdAtUtc: string
  archivedAtUtc: string | null
  memberCount: number
}

export interface ProjectMember {
  employeeId: string
  employeeName: string
  employeeIsActive: boolean
  addedAtUtc: string
}