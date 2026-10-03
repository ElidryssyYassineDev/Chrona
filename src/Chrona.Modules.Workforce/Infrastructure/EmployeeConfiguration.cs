using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workforce.Domain;

namespace Workforce.Infrastructure;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.Property(e => e.Id).HasColumnName("employee_id");
        builder.HasIndex(e => e.KeycloakSubjectId).IsUnique();

    }

}