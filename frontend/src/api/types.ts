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