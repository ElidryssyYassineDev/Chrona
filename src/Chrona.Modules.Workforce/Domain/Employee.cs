using Chrona.Shared.Domain;

namespace Workforce.Domain;

public class Employee : Entity
{
    public Guid KeycloakSubjectId { get; protected set; }
    public string FirstName { get; protected set; } = string.Empty;
    public string LastName { get; protected set; } = string.Empty;
    public Guid DepartmentId { get; protected set; }
    public Guid? ManagerId { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? DeactivatedAtUtc { get; protected set; }

    private Employee() { } //EF Core materialization only
    public Employee(Guid keycloakSubjectId,
                     string firstName,
                     string lastName,
                     Guid departmentId) : base(Guid.NewGuid())
    {
        if (keycloakSubjectId == Guid.Empty)
        {
            throw new ArgumentException("Keycloak subject ID cannot be empty.", nameof(keycloakSubjectId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be empty or whitespace.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be empty or whitespace.", nameof(lastName));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Department ID cannot be empty", nameof(departmentId));
        }

        KeycloakSubjectId = keycloakSubjectId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DepartmentId = departmentId;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive){
            return;
        }
        IsActive = false;
        DeactivatedAtUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new EmployeeDeactivated(Id, DeactivatedAtUtc.Value));
    }

    public void ChangeDepartment(Guid newDepartmentId){
        if (newDepartmentId == Guid.Empty){
            throw new ArgumentException(
                "Department ID cannot be empty", nameof(newDepartmentId));
        }
        if (newDepartmentId == DepartmentId){
            return;
        }

        DepartmentId = newDepartmentId;
    }
    
    public void AssignManager(Guid? managerId){
        if (managerId == Guid.Empty){
            throw new ArgumentException("Manager ID cannot be empty.", nameof(managerId));
        }
        if (managerId == Id){
            throw new InvalidOperationException("An employee cannot be their own manager.");
        }

        if (managerId == ManagerId){
            return;
        }

        ManagerId = managerId;
    }

    public void UpdateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "First name cannot be empty or whitespace.", nameof(firstName));
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "Last name cannot be empty or whitespace.", nameof(lastName));
        }
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }
}