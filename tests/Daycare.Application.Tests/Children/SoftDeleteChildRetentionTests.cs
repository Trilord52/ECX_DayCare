using Daycare.Application.Abstractions;
using Daycare.Application.Features.Billing.Invoices.DTOs;
using Daycare.Application.Features.Billing.Invoices.GetInvoices;
using Daycare.Application.Features.Incidents.DTOs;
using Daycare.Application.Features.Incidents.GetIncidents;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Children;

public class SoftDeleteChildRetentionTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateOnly _today = new(2026, 4, 1);
    private readonly DateTime _nowUtc = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

    public SoftDeleteChildRetentionTests()
    {
        _currentUser.Role.Returns("Admin");
        _currentUser.UserId.Returns(Guid.NewGuid());
        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
    }

    [Fact]
    public async Task GetInvoices_WhenChildIsSoftDeleted_StillReturnsInvoicesWithChildName()
    {
        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1));
        var childId = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        var guardian = new Guardian("Abebe", "Tadesse", "+251911111111", "parent@daycare.local");
        var guardianId = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian, guardianId);

        var invoice = new Invoice(
            "INV-202604-00001",
            childId,
            guardianId,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 10),
            "ETB");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(invoice, Guid.NewGuid());
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(invoice, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(invoice, guardian);
        invoice.AddLine(InvoiceLineType.Tuition, "Tuition", 1, 4500m, 0);

        // Soft delete the child
        child.SoftDelete(_nowUtc);
        child.IsDeleted.ShouldBeTrue();

        var invoiceSet = MockDbSetHelper.CreateMockDbSet([invoice]);
        _dbContext.Invoices.Returns(invoiceSet);

        var validator = Substitute.For<IValidator<GetInvoicesRequest>>();
        validator.ValidateAsync(Arg.Any<GetInvoicesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        var useCase = new GetInvoicesUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);
        var result = await useCase.ExecuteAsync(new GetInvoicesRequest());

        result.Items.Count.ShouldBe(1);
        result.Items[0].ChildName.ShouldBe("Abel Abebe");
        result.Items[0].InvoiceNumber.ShouldBe("INV-202604-00001");
    }

    [Fact]
    public async Task GetIncidents_WhenChildIsSoftDeleted_StillReturnsIncidentsWithChildName()
    {
        var classroom = new Classroom("Toddlers", null, 12, 36, 12, 5);
        var classroomId = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom, classroomId);

        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), classroomId);
        var childId = Guid.NewGuid();
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);
        typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(child, classroom);

        var incident = new Incident(
            childId,
            classroomId,
            _nowUtc,
            IncidentType.Injury,
            IncidentSeverity.Minor,
            "Minor scratch during play",
            Guid.NewGuid(),
            "Teacher Aster",
            "Washed with water");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(incident, Guid.NewGuid());
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, classroom);

        // Soft delete the child
        child.SoftDelete(_nowUtc);
        child.IsDeleted.ShouldBeTrue();

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        var validator = Substitute.For<IValidator<GetIncidentsRequest>>();
        validator.ValidateAsync(Arg.Any<GetIncidentsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        var useCase = new GetIncidentsUseCase(_dbContext, _currentUser, validator);
        var result = await useCase.ExecuteAsync(new GetIncidentsRequest());

        result.Items.Count.ShouldBe(1);
        result.Items[0].ChildName.ShouldBe("Abel Abebe");
    }
}
