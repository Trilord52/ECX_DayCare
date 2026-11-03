using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Classroom> Classrooms { get; }
    DbSet<Child> Children { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<ChildGuardian> ChildGuardians { get; }
    DbSet<Staff> Staff { get; }
    DbSet<StaffClassroom> StaffClassrooms { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<DailyReport> DailyReports { get; }
    DbSet<DailyReportEntry> DailyReportEntries { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<FeePlan> FeePlans { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<Payment> Payments { get; }

    Task<long> GetNextInvoiceSequenceAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
