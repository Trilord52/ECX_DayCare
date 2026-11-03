using Daycare.Application.Abstractions;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Infrastructure.Configuration;
using Daycare.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Daycare.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly SeedAdminOptions _adminOptions;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<SeedAdminOptions> adminOptions,
        IDateTimeProvider dateTimeProvider,
        ILogger<DataSeeder> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _adminOptions = adminOptions.Value;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedRolesAndAdminAsync()
    {
        string[] roles = ["Admin", "Staff", "Parent"];

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("Seeded role: {Role}", role);
                }
            }
        }

        var adminEmail = _adminOptions.Email.Trim().ToLowerInvariant();
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FullName = _adminOptions.FullName,
                MustChangePassword = false,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(adminUser, _adminOptions.Password);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Seeded Admin user: {Email}", adminEmail);
            }
        }
        else if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    public async Task SeedCoreRecordsAsync()
    {
        var adminUser = await _userManager.FindByEmailAsync(_adminOptions.Email.Trim().ToLowerInvariant());
        var adminUserId = adminUser?.Id ?? Guid.NewGuid();
        var today = _dateTimeProvider.LocalToday;
        var nowUtc = _dateTimeProvider.UtcNow;

        // 1. Fee Plans per Section 12
        var fullTimePlan = await _dbContext.FeePlans.FirstOrDefaultAsync(fp => fp.Name == "Full-time monthly");
        if (fullTimePlan == null)
        {
            fullTimePlan = new FeePlan("Full-time monthly", "Full day infant/toddler/preschool care", 4500m, true);
            _dbContext.FeePlans.Add(fullTimePlan);
        }

        var partTimePlan = await _dbContext.FeePlans.FirstOrDefaultAsync(fp => fp.Name == "Part-time monthly");
        if (partTimePlan == null)
        {
            partTimePlan = new FeePlan("Part-time monthly", "Morning half-day program", 3000m, true);
            _dbContext.FeePlans.Add(partTimePlan);
        }

        // 2. Classrooms per Section 12
        var infants = await _dbContext.Classrooms.FirstOrDefaultAsync(c => c.Name == "Infants");
        if (infants == null)
        {
            infants = new Classroom("Infants", "Infant care classroom (3-12 months)", 3, 12, 8, 4);
            _dbContext.Classrooms.Add(infants);
        }

        var toddlers = await _dbContext.Classrooms.FirstOrDefaultAsync(c => c.Name == "Toddlers");
        if (toddlers == null)
        {
            toddlers = new Classroom("Toddlers", "Toddler care classroom (12-30 months)", 12, 30, 12, 6);
            _dbContext.Classrooms.Add(toddlers);
        }

        var preschool = await _dbContext.Classrooms.FirstOrDefaultAsync(c => c.Name == "Preschool");
        if (preschool == null)
        {
            preschool = new Classroom("Preschool", "Preschool classroom (30-60 months)", 30, 60, 16, 8);
            _dbContext.Classrooms.Add(preschool);
        }

        await _dbContext.SaveChangesAsync();

        // 3. Staff Accounts per Section 12
        var staffConfigs = new[]
        {
            ("staff1@daycare.local", "Almaz", "Bekele", "+251911111111", "Lead Infant Caregiver", new DateOnly(2023, 1, 10), infants.Id),
            ("staff2@daycare.local", "Tigist", "Alemu", "+251922222222", "Toddler Teacher", new DateOnly(2023, 3, 15), toddlers.Id),
            ("staff3@daycare.local", "Dawit", "Haile", "+251933333333", "Preschool Teacher", new DateOnly(2023, 6, 1), preschool.Id)
        };

        foreach (var (email, first, last, phone, job, hireDate, classroomId) in staffConfigs)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    FullName = $"{first} {last}",
                    MustChangePassword = false,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createRes = await _userManager.CreateAsync(user, "Staff@12345");
                if (createRes.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                }
            }

            var staff = await _dbContext.Staff.Include(s => s.StaffClassrooms).FirstOrDefaultAsync(s => s.Email == email);
            if (staff == null)
            {
                staff = new Staff(first, last, phone, email, job, hireDate, user?.Id);
                staff.StaffClassrooms.Add(new StaffClassroom(staff.Id, classroomId, nowUtc));
                _dbContext.Staff.Add(staff);
            }
        }

        // 4th staff member without an account per Section 12
        var staff4 = await _dbContext.Staff.FirstOrDefaultAsync(s => s.Email == "meseret.staff@daycare.local");
        if (staff4 == null)
        {
            staff4 = new Staff("Meseret", "Tadesse", "+251944444444", "meseret.staff@daycare.local", "Teacher Assistant", new DateOnly(2024, 2, 1), null);
            staff4.StaffClassrooms.Add(new StaffClassroom(staff4.Id, preschool.Id, nowUtc));
            _dbContext.Staff.Add(staff4);
        }

        await _dbContext.SaveChangesAsync();

        // 4. Parent Accounts per Section 12
        var parentConfigs = new[]
        {
            ("parent1@daycare.local", "Abebe", "Tadesse", "+251911234567", "Bole, Addis Ababa"),
            ("parent2@daycare.local", "Marta", "Kebede", "+251922345678", "CMC, Addis Ababa"),
            ("parent3@daycare.local", "Solomon", "Girma", "+251933112233", "Kazanchis, Addis Ababa"),
            ("parent4@daycare.local", "Hiwot", "Alemu", "+251944223344", "Gerji, Addis Ababa"),
            ("parent5@daycare.local", "Berhanu", "Worku", "+251955334455", "Sarbet, Addis Ababa")
        };

        var parentUsers = new Dictionary<string, ApplicationUser>();
        foreach (var (email, first, last, phone, address) in parentConfigs)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    FullName = $"{first} {last}",
                    MustChangePassword = false,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var res = await _userManager.CreateAsync(user, "Parent@12345");
                if (res.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Parent");
                }
            }
            parentUsers[email] = user;
        }

        // 5. Guardians (~30 guardians with Ethiopian names)
        var existingGuardians = await _dbContext.Guardians.ToListAsync();
        Guardian EnsureGuardian(string first, string last, string phone, string? email, string? address, Guid? userId)
        {
            var existing = existingGuardians.FirstOrDefault(g => g.FirstName == first && g.LastName == last && g.Phone == phone);
            if (existing == null)
            {
                existing = new Guardian(first, last, phone, email, address, userId);
                _dbContext.Guardians.Add(existing);
                existingGuardians.Add(existing);
            }
            return existing;
        }

        var gParent1 = EnsureGuardian("Abebe", "Tadesse", "+251911234567", "parent1@daycare.local", "Bole, Addis Ababa", parentUsers["parent1@daycare.local"].Id);
        var gParent2 = EnsureGuardian("Marta", "Kebede", "+251922345678", "parent2@daycare.local", "CMC, Addis Ababa", parentUsers["parent2@daycare.local"].Id);
        var gParent3 = EnsureGuardian("Solomon", "Girma", "+251933112233", "parent3@daycare.local", "Kazanchis, Addis Ababa", parentUsers["parent3@daycare.local"].Id);
        var gParent4 = EnsureGuardian("Hiwot", "Alemu", "+251944223344", "parent4@daycare.local", "Gerji, Addis Ababa", parentUsers["parent4@daycare.local"].Id);
        var gParent5 = EnsureGuardian("Berhanu", "Worku", "+251955334455", "parent5@daycare.local", "Sarbet, Addis Ababa", parentUsers["parent5@daycare.local"].Id);

        // Spouses / Second parents
        var gSpouse1 = EnsureGuardian("Selam", "Bekele", "+251911998877", "selam.bekele@daycare.local", "Bole, Addis Ababa", null);
        var gSpouse2 = EnsureGuardian("Daniel", "Tesfaye", "+251922887766", "daniel.tesfaye@daycare.local", "CMC, Addis Ababa", null);
        var gSpouse3 = EnsureGuardian("Genet", "Assefa", "+251933776655", "genet.assefa@daycare.local", "Kazanchis, Addis Ababa", null);
        var gSpouse4 = EnsureGuardian("Tewodros", "Kassahun", "+251944665544", "tewodros.k@daycare.local", "Gerji, Addis Ababa", null);
        var gSpouse5 = EnsureGuardian("Rahel", "Tsegaye", "+251955554433", "rahel.tsegaye@daycare.local", "Sarbet, Addis Ababa", null);

        // Grandmothers without accounts (canPickUp = true)
        var gGrandmother1 = EnsureGuardian("Aster", "Desta", "+251933456789", null, "Bole, Addis Ababa", null);
        var gGrandmother2 = EnsureGuardian("Almaz", "Wolde", "+251966112233", null, "Kazanchis, Addis Ababa", null);

        // Relative with canPickUp = false (for denied-pickup demo)
        var gUncle = EnsureGuardian("Yohannes", "Tadesse", "+251944567890", null, "Bole, Addis Ababa", null);

        // Additional guardians for other children (~18 more)
        var extraGuardianData = new[]
        {
            ("Ephrem", "Desta", "+251911010101"),
            ("Tigist", "Mengistu", "+251911020202"),
            ("Yared", "Haile", "+251911030303"),
            ("Meron", "Abebe", "+251911040404"),
            ("Henok", "Teshome", "+251911050505"),
            ("Bethlehem", "Zewde", "+251911060606"),
            ("Senait", "Gashaw", "+251911070707"),
            ("Fikadu", "Tefera", "+251911080808"),
            ("Mesfin", "Belachew", "+251911090909"),
            ("Tsion", "Negash", "+251911101010"),
            ("Girma", "Wolde", "+251911121212"),
            ("Liya", "Kebede", "+251911131313"),
            ("Binyam", "Tilahun", "+251911141414"),
            ("Kalkidan", "Dagne", "+251911151515"),
            ("Tamrat", "Melaku", "+251911161616"),
            ("Helen", "Mekonnen", "+251911171717"),
            ("Tariku", "Getachew", "+251911181818"),
            ("Mulu", "Haile", "+251911191919")
        };

        var extraGuardians = new List<Guardian>();
        foreach (var (f, l, p) in extraGuardianData)
        {
            extraGuardians.Add(EnsureGuardian(f, l, p, null, "Addis Ababa", null));
        }

        await _dbContext.SaveChangesAsync();

        // 6. Children (~24 children enrolled across 3 classrooms)
        if (await _dbContext.Children.CountAsync() < 24)
        {
            // Clear existing partial data in reverse FK order to ensure a clean demo dataset
            _dbContext.AttendanceRecords.RemoveRange(await _dbContext.AttendanceRecords.ToListAsync());
            _dbContext.DailyReports.RemoveRange(await _dbContext.DailyReports.ToListAsync());
            _dbContext.Incidents.RemoveRange(await _dbContext.Incidents.ToListAsync());
            _dbContext.Payments.RemoveRange(await _dbContext.Payments.ToListAsync());
            _dbContext.Invoices.RemoveRange(await _dbContext.Invoices.ToListAsync());
            _dbContext.ChildGuardians.RemoveRange(await _dbContext.ChildGuardians.ToListAsync());
            _dbContext.Children.RemoveRange(await _dbContext.Children.ToListAsync());
            await _dbContext.SaveChangesAsync();

            // Sibling Pair 1: Abel (Toddlers, Peanuts allergy) & Sara (Infants)
            var abel = new Child("Abel", "Abebe", today.AddMonths(-18), Gender.Male, new DateOnly(2024, 1, 15), toddlers.Id, fullTimePlan.Id, "Peanuts");
            var sara = new Child("Sara", "Abebe", today.AddMonths(-6), Gender.Female, new DateOnly(2024, 3, 1), infants.Id, fullTimePlan.Id);

            // Sibling Pair 2: Natnael (Preschool, Asthma note) & Leah (Toddlers)
            var natnael = new Child("Natnael", "Marta", today.AddMonths(-36), Gender.Male, new DateOnly(2024, 1, 10), preschool.Id, fullTimePlan.Id, null, "Asthma inhaler needed");
            var leah = new Child("Leah", "Marta", today.AddMonths(-20), Gender.Female, new DateOnly(2024, 2, 1), toddlers.Id, fullTimePlan.Id);

            // Sibling Pair 3: Caleb (Toddlers) & Ruth (Preschool)
            var caleb = new Child("Caleb", "Solomon", today.AddMonths(-24), Gender.Male, new DateOnly(2024, 1, 5), toddlers.Id, fullTimePlan.Id);
            var ruth = new Child("Ruth", "Solomon", today.AddMonths(-42), Gender.Female, new DateOnly(2024, 1, 5), preschool.Id, fullTimePlan.Id);

            // Parent 4 Child
            var yonatan = new Child("Yonatan", "Tewodros", today.AddMonths(-16), Gender.Male, new DateOnly(2024, 2, 10), toddlers.Id, fullTimePlan.Id);

            // Parent 5 Child
            var miriam = new Child("Miriam", "Berhanu", today.AddMonths(-48), Gender.Female, new DateOnly(2024, 1, 12), preschool.Id, partTimePlan.Id);

            // Remaining Infants (Total 7 in Infants)
            var aaron = new Child("Aaron", "Ephrem", today.AddMonths(-4), Gender.Male, new DateOnly(2024, 4, 1), infants.Id, fullTimePlan.Id);
            var betelhem = new Child("Betelhem", "Yared", today.AddMonths(-8), Gender.Female, new DateOnly(2024, 3, 15), infants.Id, fullTimePlan.Id);
            var noah = new Child("Noah", "Henok", today.AddMonths(-5), Gender.Male, new DateOnly(2024, 4, 10), infants.Id, fullTimePlan.Id);
            var eden = new Child("Eden", "Fikadu", today.AddMonths(-9), Gender.Female, new DateOnly(2024, 2, 20), infants.Id, fullTimePlan.Id);
            var samuel = new Child("Samuel", "Mesfin", today.AddMonths(-11), Gender.Male, new DateOnly(2024, 1, 15), infants.Id, partTimePlan.Id);
            // Child without fee plan to demonstrate skipped invoice in demo
            var hanna = new Child("Hanna", "Girma", today.AddMonths(-7), Gender.Female, new DateOnly(2024, 3, 20), infants.Id, null);

            // Remaining Toddlers (Total 9 in Toddlers)
            var elias = new Child("Elias", "Berhanu", today.AddMonths(-22), Gender.Male, new DateOnly(2024, 1, 18), toddlers.Id, fullTimePlan.Id);
            var lydia = new Child("Lydia", "Binyam", today.AddMonths(-15), Gender.Female, new DateOnly(2024, 2, 25), toddlers.Id, fullTimePlan.Id);
            var nathan = new Child("Nathan", "Tamrat", today.AddMonths(-26), Gender.Male, new DateOnly(2024, 1, 8), toddlers.Id, fullTimePlan.Id);
            var naomi = new Child("Naomi", "Girma", today.AddMonths(-19), Gender.Female, new DateOnly(2024, 2, 14), toddlers.Id, partTimePlan.Id);
            var lucas = new Child("Lucas", "Senait", today.AddMonths(-14), Gender.Male, new DateOnly(2024, 3, 5), toddlers.Id, fullTimePlan.Id);

            // Remaining Preschool (Total 8 in Preschool)
            var daniel = new Child("Daniel", "Ephrem", today.AddMonths(-54), Gender.Male, new DateOnly(2024, 1, 10), preschool.Id, fullTimePlan.Id);
            var abigail = new Child("Abigail", "Yared", today.AddMonths(-38), Gender.Female, new DateOnly(2024, 2, 1), preschool.Id, fullTimePlan.Id);
            var joshua = new Child("Joshua", "Henok", today.AddMonths(-44), Gender.Male, new DateOnly(2024, 1, 15), preschool.Id, fullTimePlan.Id);
            var rebecca = new Child("Rebecca", "Fikadu", today.AddMonths(-50), Gender.Female, new DateOnly(2024, 1, 20), preschool.Id, partTimePlan.Id);
            var gabriel = new Child("Gabriel", "Mesfin", today.AddMonths(-35), Gender.Male, new DateOnly(2024, 2, 12), preschool.Id, fullTimePlan.Id);

            var allChildren = new List<Child>
            {
                abel, sara, natnael, leah, caleb, ruth, yonatan, miriam,
                aaron, betelhem, noah, eden, samuel, hanna,
                elias, lydia, nathan, naomi, lucas,
                daniel, abigail, joshua, rebecca, gabriel
            };

            _dbContext.Children.AddRange(allChildren);
            await _dbContext.SaveChangesAsync();

            // Link guardians
            void AddLinks(Child c, Guardian primary, Guardian? secondary = null, Guardian? grandma = null, Guardian? uncle = null)
            {
                _dbContext.ChildGuardians.Add(new ChildGuardian(c.Id, primary.Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true));
                if (secondary != null)
                {
                    _dbContext.ChildGuardians.Add(new ChildGuardian(c.Id, secondary.Id, Relationship.Mother, isPrimaryContact: false, canPickUp: true, isPayer: false, isEmergencyContact: true));
                }
                if (grandma != null)
                {
                    _dbContext.ChildGuardians.Add(new ChildGuardian(c.Id, grandma.Id, Relationship.Grandparent, isPrimaryContact: false, canPickUp: true, isPayer: false, isEmergencyContact: false));
                }
                if (uncle != null)
                {
                    _dbContext.ChildGuardians.Add(new ChildGuardian(c.Id, uncle.Id, Relationship.Relative, isPrimaryContact: false, canPickUp: false, isPayer: false, isEmergencyContact: false));
                }
            }

            // Sibling pair 1
            AddLinks(abel, gParent1, gSpouse1, gGrandmother1, gUncle);
            AddLinks(sara, gParent1, gSpouse1);

            // Sibling pair 2
            AddLinks(natnael, gParent2, gSpouse2);
            AddLinks(leah, gParent2, gSpouse2);

            // Sibling pair 3
            AddLinks(caleb, gParent3, gSpouse3, gGrandmother2);
            AddLinks(ruth, gParent3, gSpouse3);

            // Parent 4 & 5
            AddLinks(yonatan, gParent4, gSpouse4);
            AddLinks(miriam, gParent5, gSpouse5);

            // Other 16 children linked with extra guardians
            for (int i = 8; i < allChildren.Count; i++)
            {
                var ch = allChildren[i];
                var guardianIdx = (i - 8) % extraGuardians.Count;
                var g = extraGuardians[guardianIdx];
                _dbContext.ChildGuardians.Add(new ChildGuardian(ch.Id, g.Id, Relationship.Mother, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true));
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded 24 children and child-guardian links.");
        }

        // 7. Announcements (3 announcements per Section 12)
        if (!await _dbContext.Announcements.AnyAsync())
        {
            _dbContext.Announcements.Add(new Announcement(
                "Welcome to the New School Term",
                "We are excited to welcome all children and families to the new semester! Please make sure pickup authorizations are updated in the parent portal.",
                AnnouncementAudience.Everyone,
                adminUserId,
                "System Admin",
                nowUtc.AddDays(-5)));

            _dbContext.Announcements.Add(new Announcement(
                "Parent-Teacher Conferences Next Week",
                "Parent-Teacher conferences will be held next Thursday from 4:00 PM to 7:00 PM. Please schedule your appointment through the office.",
                AnnouncementAudience.Parents,
                adminUserId,
                "System Admin",
                nowUtc.AddDays(-3)));

            _dbContext.Announcements.Add(new Announcement(
                "Preschool Art Fair Supplies Needed",
                "The Preschool classroom is preparing for the Spring Art Showcase. We welcome donations of recyclable boxes, buttons, and watercolors.",
                AnnouncementAudience.Classroom,
                adminUserId,
                "System Admin",
                nowUtc.AddDays(-1),
                preschool.Id));

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded 3 demo announcements.");
        }

        // 8. Incidents (2 incidents per Section 12)
        if (!await _dbContext.Incidents.AnyAsync())
        {
            var abel = await _dbContext.Children.FirstOrDefaultAsync(c => c.FirstName == "Abel" && c.LastName == "Abebe");
            var natnael = await _dbContext.Children.FirstOrDefaultAsync(c => c.FirstName == "Natnael" && c.LastName == "Marta");

            if (abel != null)
            {
                var inc1 = new Incident(
                    abel.Id,
                    toddlers.Id,
                    nowUtc.AddDays(-4),
                    IncidentType.Injury,
                    IncidentSeverity.Minor,
                    "Scraped knee on outdoor playground climbing frame",
                    adminUserId,
                    "System Admin",
                    "Cleaned with antiseptic wash and applied adhesive bandage",
                    nowUtc.AddDays(-4).AddMinutes(30),
                    "System Admin");

                inc1.Acknowledge(gParent1.Id, nowUtc.AddDays(-4).AddHours(2));
                _dbContext.Incidents.Add(inc1);
            }

            if (natnael != null)
            {
                var inc2 = new Incident(
                    natnael.Id,
                    preschool.Id,
                    nowUtc.AddDays(-1),
                    IncidentType.Illness,
                    IncidentSeverity.Moderate,
                    "Sudden fever of 38.8C with shivering during circle time",
                    adminUserId,
                    "System Admin",
                    "Moved to quiet cot room with caregiver, offered water, called parent for pickup",
                    nowUtc.AddDays(-1).AddMinutes(15),
                    "System Admin");

                // Unacknowledged per Section 12
                _dbContext.Incidents.Add(inc2);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded 2 demo incidents (one acknowledged, one unacknowledged).");
        }

        // 9. Attendance for the previous 14 weekdays per Section 12
        if (!await _dbContext.AttendanceRecords.AnyAsync(a => a.Date < today))
        {
            var staffUser = await _userManager.FindByEmailAsync("staff2@daycare.local");
            var staffUserId = staffUser?.Id ?? adminUserId;
            var enrolledChildren = await _dbContext.Children.Where(c => c.Status == ChildStatus.Active).ToListAsync();

            var pastDates = new List<DateOnly>();
            var checkDate = today.AddDays(-1);
            while (pastDates.Count < 14)
            {
                if (checkDate.DayOfWeek != DayOfWeek.Saturday && checkDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    pastDates.Add(checkDate);
                }
                checkDate = checkDate.AddDays(-1);
            }
            pastDates.Reverse();

            int recordIdx = 0;
            foreach (var date in pastDates)
            {
                var checkInBaseUtc = new DateTime(date.Year, date.Month, date.Day, 5, 30, 0, DateTimeKind.Utc); // 8:30 AM EAT

                foreach (var child in enrolledChildren)
                {
                    if (!child.ClassroomId.HasValue) continue;

                    // A few absences scattered across children (approx 10%)
                    if ((recordIdx % 11) == 0 && child.FirstName != "Abel")
                    {
                        var reason = (recordIdx % 2 == 0) ? "Fever and cold" : "Family travel";
                        _dbContext.AttendanceRecords.Add(AttendanceRecord.CreateAbsent(
                            child.Id,
                            child.ClassroomId.Value,
                            date,
                            reason,
                            staffUserId,
                            "Tigist Alemu"));
                    }
                    else
                    {
                        var checkInTime = checkInBaseUtc.AddMinutes(recordIdx % 20);
                        var att = AttendanceRecord.CreateCheckIn(
                            child.Id,
                            child.ClassroomId.Value,
                            date,
                            checkInTime,
                            null,
                            staffUserId,
                            "Tigist Alemu");

                        // Late pickups on some records (check out after 18:00 local / 15:00 UTC)
                        int lateMinutes = 0;
                        DateTime checkOutTime;
                        if ((recordIdx % 7) == 0)
                        {
                            lateMinutes = 35;
                            checkOutTime = new DateTime(date.Year, date.Month, date.Day, 15, 35, 0, DateTimeKind.Utc); // 18:35 EAT
                        }
                        else
                        {
                            checkOutTime = new DateTime(date.Year, date.Month, date.Day, 14, 0, 0, DateTimeKind.Utc); // 17:00 EAT
                        }

                        att.CheckOut(checkOutTime, gParent1.Id, staffUserId, "Tigist Alemu", true, lateMinutes);
                        _dbContext.AttendanceRecords.Add(att);
                    }

                    recordIdx++;
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded attendance records for previous 14 weekdays.");
        }

        // 10. Invoices for the Previous Month per Section 12
        if (!await _dbContext.Invoices.AnyAsync())
        {
            var prevMonthDate = today.AddMonths(-1);
            var prevYear = prevMonthDate.Year;
            var prevMonth = prevMonthDate.Month;
            var periodStart = new DateOnly(prevYear, prevMonth, 1);
            var periodEnd = new DateOnly(prevYear, prevMonth, DateTime.DaysInMonth(prevYear, prevMonth));
            var issueDate = periodStart;
            var dueDate = new DateOnly(prevYear, prevMonth, 10);

            var childrenWithPlans = await _dbContext.Children
                .Include(c => c.FeePlan)
                .Include(c => c.Guardians)
                .Where(c => c.FeePlanId.HasValue && c.Status == ChildStatus.Active)
                .ToListAsync();

            int invSeq = 1;
            foreach (var child in childrenWithPlans)
            {
                var payer = child.Guardians.FirstOrDefault(cg => cg.IsPayer);
                if (payer == null || child.FeePlan == null) continue;

                var invNumber = $"INV-{prevYear}{prevMonth:D2}-{invSeq:D5}";
                var invoice = new Invoice(
                    invNumber,
                    child.Id,
                    payer.GuardianId,
                    periodStart,
                    periodEnd,
                    issueDate,
                    dueDate,
                    "ETB");

                invoice.AddLine(
                    InvoiceLineType.Tuition,
                    $"Tuition - {child.FeePlan.Name}",
                    1,
                    child.FeePlan.MonthlyAmount,
                    0);

                // Add late fee line to every 4th invoice
                if (invSeq % 4 == 0)
                {
                    invoice.AddLine(InvoiceLineType.LateFee, "Late pickup fees (35 min)", 1, 350m, 1);
                }

                _dbContext.Invoices.Add(invoice);

                // Payment distribution: 60% Paid, 20% PartiallyPaid, 20% Overdue/Unpaid
                var total = invoice.TotalAmount;
                if (invSeq % 5 == 1 || invSeq % 5 == 2 || invSeq % 5 == 3) // 60%
                {
                    invoice.ApplyPayment(total);
                    var method = (invSeq % 3 == 0) ? PaymentMethod.Telebirr : (invSeq % 2 == 0 ? PaymentMethod.BankTransfer : PaymentMethod.Cash);
                    var payment = new Payment(
                        invoice.Id,
                        total,
                        method,
                        new DateTime(prevYear, prevMonth, 5, 10, 0, 0, DateTimeKind.Utc),
                        adminUserId,
                        "System Admin",
                        $"REF-{prevYear}{prevMonth:D2}-{invSeq:D4}",
                        "Settled in full");
                    _dbContext.Payments.Add(payment);
                }
                else if (invSeq % 5 == 4) // 20% Partially Paid
                {
                    var partialAmount = decimal.Round(total / 2m, 2);
                    invoice.ApplyPayment(partialAmount);
                    var payment = new Payment(
                        invoice.Id,
                        partialAmount,
                        PaymentMethod.BankTransfer,
                        new DateTime(prevYear, prevMonth, 7, 11, 0, 0, DateTimeKind.Utc),
                        adminUserId,
                        "System Admin",
                        $"REF-PARTIAL-{invSeq:D4}",
                        "Initial installment");
                    _dbContext.Payments.Add(payment);
                }
                // invSeq % 5 == 0 -> 20% Unpaid & Overdue (no payment made, status remains Issued)

                invSeq++;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded invoices for previous month with mixed payments and overdue balances.");
        }

        // 11. Today's Sample (re-created on every Development startup if missing per Section 12)
        var todayAttendanceCount = await _dbContext.AttendanceRecords.CountAsync(a => a.Date == today);
        if (todayAttendanceCount == 0)
        {
            var staff2User = await _userManager.FindByEmailAsync("staff2@daycare.local");
            var staffUserId = staff2User?.Id ?? adminUserId;
            var staffName = "Tigist Alemu";

            var allChildren = await _dbContext.Children.Where(c => c.Status == ChildStatus.Active).ToListAsync();

            // Toddlers classroom: exactly 7 children present with 1 staff -> ratio 7/6 -> isOverLimit: true!
            var toddlerChildren = allChildren.Where(c => c.ClassroomId == toddlers.Id).ToList();
            int toddlerCount = 0;
            foreach (var ch in toddlerChildren)
            {
                if (toddlerCount < 7)
                {
                    // Check in and leave in building
                    var att = AttendanceRecord.CreateCheckIn(
                        ch.Id,
                        toddlers.Id,
                        today,
                        nowUtc.AddHours(-3),
                        null,
                        staffUserId,
                        staffName);
                    _dbContext.AttendanceRecords.Add(att);
                }
                else if (toddlerCount == 7)
                {
                    // 1 checked out earlier
                    var att = AttendanceRecord.CreateCheckIn(
                        ch.Id,
                        toddlers.Id,
                        today,
                        nowUtc.AddHours(-5),
                        null,
                        staffUserId,
                        staffName);
                    att.CheckOut(nowUtc.AddHours(-1), gParent1.Id, staffUserId, staffName, true, 0);
                    _dbContext.AttendanceRecords.Add(att);
                }
                else
                {
                    // 1 absent
                    _dbContext.AttendanceRecords.Add(AttendanceRecord.CreateAbsent(
                        ch.Id,
                        toddlers.Id,
                        today,
                        "Mild cold",
                        staffUserId,
                        staffName));
                }
                toddlerCount++;
            }

            // Infants: 4 checked in, 1 checked out, 1 absent, 1 not arrived
            var infantChildren = allChildren.Where(c => c.ClassroomId == infants.Id).ToList();
            for (int i = 0; i < infantChildren.Count; i++)
            {
                var ch = infantChildren[i];
                if (i < 4)
                {
                    _dbContext.AttendanceRecords.Add(AttendanceRecord.CreateCheckIn(
                        ch.Id, infants.Id, today, nowUtc.AddHours(-2), null, staffUserId, staffName));
                }
                else if (i == 4)
                {
                    var att = AttendanceRecord.CreateCheckIn(
                        ch.Id, infants.Id, today, nowUtc.AddHours(-4), null, staffUserId, staffName);
                    att.CheckOut(nowUtc.AddHours(-1), gParent1.Id, staffUserId, staffName, true, 0);
                    _dbContext.AttendanceRecords.Add(att);
                }
                else if (i == 5)
                {
                    _dbContext.AttendanceRecords.Add(AttendanceRecord.CreateAbsent(
                        ch.Id, infants.Id, today, "Doctor appointment", staffUserId, staffName));
                }
                // i == 6: not arrived
            }

            // Preschool: 4 checked in, 4 not arrived
            var preschoolChildren = allChildren.Where(c => c.ClassroomId == preschool.Id).ToList();
            for (int i = 0; i < preschoolChildren.Count; i++)
            {
                var ch = preschoolChildren[i];
                if (i < 4)
                {
                    _dbContext.AttendanceRecords.Add(AttendanceRecord.CreateCheckIn(
                        ch.Id, preschool.Id, today, nowUtc.AddHours(-2), null, staffUserId, staffName));
                }
            }

            await _dbContext.SaveChangesAsync();

            // Daily report entries for Abel and a couple of other present children
            var abel = allChildren.FirstOrDefault(c => c.FirstName == "Abel");
            if (abel != null)
            {
                var rep = new DailyReport(abel.Id, toddlers.Id, today, Mood.Happy, "Very energetic and curious today");
                _dbContext.DailyReports.Add(rep);

                _dbContext.DailyReportEntries.Add(new DailyReportEntry(
                    rep.Id,
                    EntryType.Meal,
                    nowUtc.AddHours(-2),
                    staffUserId,
                    staffName,
                    mealType: MealType.Lunch,
                    amountEaten: AmountEaten.All,
                    description: "Ate all vegetables and injera with chicken tibs"));

                _dbContext.DailyReportEntries.Add(new DailyReportEntry(
                    rep.Id,
                    EntryType.Nap,
                    nowUtc.AddHours(-1),
                    staffUserId,
                    staffName,
                    endedAt: nowUtc.AddMinutes(-10),
                    description: "Slept soundly for 50 minutes"));

                _dbContext.DailyReportEntries.Add(new DailyReportEntry(
                    rep.Id,
                    EntryType.Diaper,
                    nowUtc.AddMinutes(-30),
                    staffUserId,
                    staffName,
                    diaperType: DiaperType.Wet,
                    description: "Diaper changed, clean and comfortable"));

                _dbContext.DailyReportEntries.Add(new DailyReportEntry(
                    rep.Id,
                    EntryType.Activity,
                    nowUtc.AddMinutes(-15),
                    staffUserId,
                    staffName,
                    description: "Built large block tower with classmates"));
            }

            var natnael = allChildren.FirstOrDefault(c => c.FirstName == "Natnael");
            if (natnael != null)
            {
                var rep = new DailyReport(natnael.Id, preschool.Id, today, Mood.Calm, "Enjoyed story reading and coloring");
                _dbContext.DailyReports.Add(rep);

                _dbContext.DailyReportEntries.Add(new DailyReportEntry(
                    rep.Id,
                    EntryType.Meal,
                    nowUtc.AddHours(-2),
                    staffUserId,
                    staffName,
                    mealType: MealType.Lunch,
                    amountEaten: AmountEaten.Most,
                    description: "Ate rice and lentil stew"));

                _dbContext.DailyReportEntries.Add(new DailyReportEntry(
                    rep.Id,
                    EntryType.Activity,
                    nowUtc.AddMinutes(-40),
                    staffUserId,
                    staffName,
                    description: "Painted a colorful tree picture"));
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded today's sample attendance, over-ratio Toddlers classroom, and daily report entries.");
        }
    }
}
