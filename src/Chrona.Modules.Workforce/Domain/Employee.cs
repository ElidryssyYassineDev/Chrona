using Chrona.Shared.Domain;

namespace Workforce.Domain
{
    public class Employee : Entity
    {
        public Guid KeycloakSubjectId {get; protected set;}
        public string FirstName {get; protected set;} = string.Empty;
        public string LastName {get; protected set;} = string.Empty;
        public Guid DepartmentId {get; protected set;}
        public Guid? ManagerId {get; protected set;}
        public bool IsActive {get; protected set;}
        public DateTimeOffset CreatedAtUtc {get; protected set;}
        public DateTimeOffset? DeactivatedAtUtc {get; protected set;}

        private Employee(){ } //EF Core materialization only
        public Employee (Guid keycloakSubjectId,
                         string firstName,
                         string lastName,
                         Guid departmentId)
        {
            this.KeycloakSubjectId = keycloakSubjectId;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.DepartmentId = departmentId;

            IsActive = true;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            DeactivatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}