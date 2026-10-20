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

export interface Allocation {
  allocationId: string
  employeeId: string
  employeeName: string
  projectId: string
  projectName: string
  periodStart: string
  periodEnd: string
  percentage: number
  status: 'Active' | 'Cancelled'
  createdAtUtc: string
}

export interface AllocationHistoryEntry {
  changeType: string
  oldValue: string | null
  newValue: string | null
  changedAtUtc: string
}

export interface AllocationDetail {
  allocation: Allocation
  history: AllocationHistoryEntry[]
}

export interface Capacity {
  employeeId: string
  periodStart: string
  periodEnd: string
  peakAllocatedPercentage: number
  availablePercentage: number
}

export interface TimeEntry {
  timeEntryId: string
  allocationId: string
  projectName: string
  entryDate: string
  hours: number
  createdAtUtc: string
}

export interface Timesheet {
  timesheetId: string
  employeeId: string
  periodStart: string
  periodEnd: string
  status: 'Draft' | 'Submitted' | 'Approved'
  lastSubmittedAtUtc: string | null
  totalHours: number
  entries: TimeEntry[]
}

export interface TimesheetSummary {
  timesheetId: string
  periodStart: string
  periodEnd: string
  status: 'Draft' | 'Submitted' | 'Approved'
  lastSubmittedAtUtc: string | null
  entryCount: number
  totalHours: number
}