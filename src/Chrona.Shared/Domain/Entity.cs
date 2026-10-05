namespace Chrona.Shared.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity()
    {
        //parameterless constuctor
    }

    protected Entity(Guid id)
    {
        this.Id = id;
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null) throw new ArgumentNullException(nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (!(obj is Entity other))
        {
            return false;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return (Id == other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static bool operator ==(Entity left, Entity right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (right is null || left is null)
        {
            return false;
        }
        return left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }

}