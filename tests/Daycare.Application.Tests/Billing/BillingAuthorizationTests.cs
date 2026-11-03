using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Billing.Invoices.GetInvoiceById;
using Daycare.Application.Features.Billing.Invoices.GetInvoices;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Billing;

public class BillingAuthorizationTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _parent1GuardianId = Guid.NewGuid();
    private readonly Guid _parent2GuardianId = Guid.NewGuid();
    private readonly Invoice _parent1Invoice;
    private readonly Invoice _parent2Invoice;

    public BillingAuthorizationTests()
    {
        _dateTimeProvider.LocalToday.Returns(new DateOnly(2026, 4, 15));
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc));

        var child = new Child("Kid", "One", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), Guid.NewGuid());
        var guardian1 = new Guardian("Parent", "One", "+251911111111", "p1@daycare.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian1, _parent1GuardianId);

        var guardian2 = new Guardian("Parent", "Two", "+251922222222", "p2@daycare.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian2, _parent2GuardianId);

        _parent1Invoice = new Invoice(
            "INV-202604-00001",
            _childId,
            _parent1GuardianId,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 10),
            "ETB");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_parent1Invoice, Guid.NewGuid());
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(_parent1Invoice, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(_parent1Invoice, guardian1);

        _parent2Invoice = new Invoice(
            "INV-202604-00002",
            _childId,
            _parent2GuardianId,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 10),
            "ETB");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_parent2Invoice, Guid.NewGuid());
        typeof(Invoice).GetProperty(nameof(Invoice.Child))!.SetValue(_parent2Invoice, child);
        typeof(Invoice).GetProperty(nameof(Invoice.Guardian))!.SetValue(_parent2Invoice, guardian2);

        var invoicesSet = MockDbSetHelper.CreateMockDbSet([_parent1Invoice, _parent2Invoice]);
        _dbContext.Invoices.Returns(invoicesSet);
    }

    [Fact]
    public async Task GetInvoices_WhenRoleIsStaff_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        var validator = new GetInvoicesValidator();
        var useCase = new GetInvoicesUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetInvoicesRequest()));
    }

    [Fact]
    public async Task GetInvoiceById_WhenRoleIsStaff_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Staff");
        var validator = new GetInvoiceByIdValidator();
        var useCase = new GetInvoiceByIdUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetInvoiceByIdRequest(_parent1Invoice.Id)));
    }

    [Fact]
    public async Task GetInvoiceById_WhenParentViewsAnotherParentsInvoice_ShouldThrowForbidden()
    {
        // Arrange
        _currentUser.Role.Returns("Parent");
        _currentUser.GuardianId.Returns(_parent1GuardianId);

        var validator = new GetInvoiceByIdValidator();
        var useCase = new GetInvoiceByIdUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert - parent 1 tries to view parent 2's invoice
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetInvoiceByIdRequest(_parent2Invoice.Id)));
    }

    [Fact]
    public async Task GetInvoiceById_WhenParentViewsOwnInvoice_ShouldSucceed()
    {
        // Arrange
        _currentUser.Role.Returns("Parent");
        _currentUser.GuardianId.Returns(_parent1GuardianId);

        var validator = new GetInvoiceByIdValidator();
        var useCase = new GetInvoiceByIdUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetInvoiceByIdRequest(_parent1Invoice.Id));

        // Assert
        result.Id.ShouldBe(_parent1Invoice.Id);
        result.GuardianId.ShouldBe(_parent1GuardianId);
    }

    [Fact]
    public async Task GetInvoices_WhenParentViewsInvoices_ShouldReturnOnlyOwnInvoices()
    {
        // Arrange
        _currentUser.Role.Returns("Parent");
        _currentUser.GuardianId.Returns(_parent1GuardianId);

        var validator = new GetInvoicesValidator();
        var useCase = new GetInvoicesUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetInvoicesRequest());

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].GuardianId.ShouldBe(_parent1GuardianId);
    }

    [Fact]
    public async Task GetInvoices_WhenAdminViewsInvoices_ShouldReturnAllInvoices()
    {
        // Arrange
        _currentUser.Role.Returns("Admin");

        var validator = new GetInvoicesValidator();
        var useCase = new GetInvoicesUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetInvoicesRequest());

        // Assert
        result.Items.Count.ShouldBe(2);
    }
}

