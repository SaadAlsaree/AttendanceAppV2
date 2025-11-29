using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", Schemas.Default);

        // Configure ID as auto-incrementing integer primary key
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .HasIdentityOptions(startValue: 1, incrementBy: 1);

        builder.Property(e => e.Code)
            .HasMaxLength(20);

        builder.Property(e => e.RFID)
            .HasMaxLength(50);

        builder.Property(e => e.FirstName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.SecondName)
            .HasMaxLength(50);

        builder.Property(e => e.ThirdName)
            .HasMaxLength(50);

        builder.Property(e => e.FourthName)
            .HasMaxLength(50);

        builder.Property(e => e.FamilyName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.Property(e => e.ProfileImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.NationalIdFrontUrl)
            .HasMaxLength(500);

        builder.Property(e => e.NationalIdBackUrl)
            .HasMaxLength(500);

        builder.Property(e => e.FaceImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.ProfileImageUrl)
            .HasMaxLength(500);



        // Relationships
        builder.HasOne(e => e.OrganizationalUnit)
            .WithMany(u => u.Employees)
            .HasForeignKey(e => e.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Manager)
            .WithMany(m => m.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.ManagedUnits)
            .WithOne(u => u.Manager)
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.AttendanceSchedules)
            .WithOne(s => s.Employee)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attendances)
            .WithOne(a => a.Employee)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);




        // Indexes
        //builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.FullName);
        builder.HasIndex(e => e.RFID);
        builder.HasIndex(e => e.EmpID).IsUnique();
    }
}
