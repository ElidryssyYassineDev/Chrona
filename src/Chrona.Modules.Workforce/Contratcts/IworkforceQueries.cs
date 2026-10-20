namespace Workforce.Contracts;

public sealed record EmployeeSummary(Guid EmployeeId, string FirstName, string LastName, bool IsActive)
{
    public string FullName => $"{FirstName} {LastName}";
}

// Surface publique de Workforce pour les autres modules (03-module-design.md, §3 et §5).
public interface IWorkforceQueries
{
    Task<bool> EmployeeExistsAndIsActiveAsync(Guid employeeId);
    Task<Guid?> GetEmployeeIdForPrincipalAsync(Guid subjectId);
    Task<IReadOnlyDictionary<Guid, EmployeeSummary>> GetEmployeeSummariesAsync(IReadOnlyCollection<Guid> employeeIds);
}