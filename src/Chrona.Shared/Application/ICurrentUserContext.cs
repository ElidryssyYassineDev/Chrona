namespace Chrona.Shared.Application;

public interface ICurrentUserContext
{
    Guid SubjectId {get;}
    IReadOnlyCollection<string> Roles {get;}
}