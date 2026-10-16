using Chrona.Shared.Domain;

namespace Workforce.Domain;

public class Department : Entity
{
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Department() { } // Réservé à la matérialisation par EF Core

    public Department(string name) : base(Guid.NewGuid())
    {
        ValidateAndSetName(name);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Rename(string newName) => ValidateAndSetName(newName);

    private void ValidateAndSetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Department name cannot be empty or whitespace.", nameof(name));
        }

        Name = name.Trim();
    }
}