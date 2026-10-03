using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workforce.Domain;

namespace Workforce.Infrastructure;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments", t =>
            t.HasCheckConstraint("ck_departments_name_not_empty", "length(trim(name)) > 0"));

        builder.Property(d => d.Id).HasColumnName("department_id");

        builder.Property(d => d.Name).IsRequired();

        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasDatabaseName("uk_departments_name");

        builder.Property(d => d.CreatedAtUtc).IsRequired();
    }
}