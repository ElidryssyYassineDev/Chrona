using Chrona.Shared.Domain;

namespace ProjectManagement.Domain;

public class Project : Entity
{
    private readonly List<ProjectMember> _members = [];

    public string Name { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private Project() { } // Réservé à EF Core

    public Project(string name) : base(Guid.NewGuid())
    {
        Name = RequireName(name);
        Status = ProjectStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Rename(string newName)
    {
        EnsureActive();
        Name = RequireName(newName);
    }

    public void Archive()
    {
        EnsureActive();
        Status = ProjectStatus.Archived;
        ArchivedAtUtc = DateTimeOffset.UtcNow;
    }

    public ProjectMember AddMember(Guid employeeId)
    {
        EnsureActive();
        if (employeeId == Guid.Empty)
            throw new ArgumentException("employeeId cannot be an empty GUID.", nameof(employeeId));
        if (_members.Any(m => m.EmployeeId == employeeId))
            throw new InvalidOperationException("The employee is already a member of this project.");

        var member = new ProjectMember(Id, employeeId);
        _members.Add(member);
        return member;
    }

    public ProjectMember RemoveMember(Guid employeeId)
    {
        EnsureActive();
        var member = _members.FirstOrDefault(m => m.EmployeeId == employeeId)
            ?? throw new InvalidOperationException("The employee is not a member of this project.");
        _members.Remove(member);
        return member;
    }

    // Projet archivé = figé (06-domain-model.md, §3) : aucun changement, adhésions comprises.
    private void EnsureActive()
    {
        if (Status == ProjectStatus.Archived)
            throw new InvalidOperationException("An archived project can no longer be changed.");
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(name));
        return name.Trim();
    }
}