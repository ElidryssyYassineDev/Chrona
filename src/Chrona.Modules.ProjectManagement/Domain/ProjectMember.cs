namespace ProjectManagement.Domain;

// Entité fille : identifiée par (ProjectId, EmployeeId), sans ID propre ; elle ne détient aucune donnée d'Employee.
public class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTimeOffset AddedAtUtc { get; private set; }

    private ProjectMember() { } // Réservé à EF Core

    internal ProjectMember(Guid projectId, Guid employeeId)
    {
        ProjectId = projectId;
        EmployeeId = employeeId;
        AddedAtUtc = DateTimeOffset.UtcNow;
    }
}