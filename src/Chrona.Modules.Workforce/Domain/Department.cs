using Chrona.Shared.Domain;

namespace Workforce.Domain;

public class Department : Entity
{
    public string Name { get; protected set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; protected set; }

    private Department() { } // EF Core materialization only

    public Department(string name) : base(Guid.NewGuid())
    {
        ValidateAndSetName(name);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Department(Guid id, string name) : base(id)
    {
        ValidateAndSetName(name);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Rename(string newName)
    {
        ValidateAndSetName(newName);
    }

    private void ValidateAndSetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Department name cannot be empty or whitespace.", nameof(name));
        }

        Name = name.Trim();
    }
}