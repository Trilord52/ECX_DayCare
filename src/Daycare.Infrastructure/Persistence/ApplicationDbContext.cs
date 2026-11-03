using Daycare.Application.Abstractions;
using Daycare.Domain.Entities;
using Daycare.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<ChildGuardian> ChildGuardians => Set<ChildGuardian>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffClassroom> StaffClassrooms => Set<StaffClassroom>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();
    public DbSet<DailyReportEntry> DailyReportEntries => Set<DailyReportEntry>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<FeePlan> FeePlans => Set<FeePlan>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();

    public async Task<long> GetNextInvoiceSequenceAsync(CancellationToken ct = default)
    {
        var connection = Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT nextval('invoice_number_seq');";
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasSequence<long>("invoice_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters for soft-deletable entities per Section 5.6
        builder.Entity<Child>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<Guardian>().HasQueryFilter(g => !g.IsDeleted);
        builder.Entity<Staff>().HasQueryFilter(s => !s.IsDeleted);
    }
}
