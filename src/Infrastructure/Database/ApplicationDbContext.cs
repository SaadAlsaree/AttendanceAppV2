using Application.Abstractions.Data;
using Domain.Entities.Attendance;
using Domain.Entities.Devices;
using Domain.Entities.Organizations;
using Domain.Entities.Users;
using Domain.Todos;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<TodoItem> TodoItems { get; set; }

    public DbSet<Employee> Employees { get; set; }

    public DbSet<EmployeeWeeklyShift> EmployeeWeeklyShifts { get; set; }

    public DbSet<OrganizationalUnit> OrganizationalUnits { get; set; }

    public DbSet<WorkLocation> WorkLocations { get; set; }

    public DbSet<Site> Sites { get; set; }

    public DbSet<Shift> Shifts { get; set; }

    public DbSet<Holiday> Holidays { get; set; }

    public DbSet<AttendanceSchedule> AttendanceSchedules { get; set; }

    public DbSet<Attendance> Attendances { get; set; }

    public DbSet<AttendanceLog> AttendanceLogs { get; set; }

    public DbSet<AttendanceBreak> AttendanceBreaks { get; set; }

    public DbSet<ScheduleIssue> ScheduleIssues { get; set; }

    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

    public DbSet<Device> Devices { get; set; }

    public DbSet<Leave> Leaves { get; set; }

    public DbSet<Attachment> Attachments { get; set; }

    public DbSet<ScheduleDay> ScheduleDays { get; set; }

    public DbSet<AttendanceException> AttendanceExceptions { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.HasDefaultSchema(Schemas.Default);




    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}
