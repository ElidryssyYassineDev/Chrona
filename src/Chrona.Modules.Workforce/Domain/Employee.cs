using Chrona.Shared.Domain;

namespace Workforce.Domain;

public class Employee : Entity
{
    public Guid KeycloakSubjectId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    private Employee() { } // Réservé à la matérialisation par EF Core

    public Employee(Guid keycloakSubjectId, string firstName, string lastName, Guid departmentId, Guid? managerId = null)
        : base(Guid.NewGuid()) // L'identité existe dès la construction (08-database-design.md, §2)
    {
        KeycloakSubjectId = RequireId(keycloakSubjectId, nameof(keycloakSubjectId));
        FirstName = RequireText(firstName, nameof(firstName));
        LastName = RequireText(lastName, nameof(lastName));
        DepartmentId = RequireId(departmentId, nameof(departmentId));
        ManagerId = managerId;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(string firstName, string lastName, Guid departmentId, Guid? managerId)
    {
        // On valide tout avant de modifier quoi que ce soit : pas de mutation partielle.
        var first = RequireText(firstName, nameof(firstName));
        var last = RequireText(lastName, nameof(lastName));
        var department = RequireId(departmentId, nameof(departmentId));
        if (managerId == Id)
        {
            throw new ArgumentException("An employee cannot be their own manager.", nameof(managerId));
        }

        FirstName = first;
        LastName = last;
        DepartmentId = department;
        ManagerId = managerId;
    }

    public void Deactivate()
    {
        if (!IsActive) return; // Idempotent : on garde la première date de désactivation

        IsActive = false;
        DeactivatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be empty or whitespace.", paramName);
        }

        return value.Trim();
    }

    private static Guid RequireId(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
        }

        return value;
    }
}