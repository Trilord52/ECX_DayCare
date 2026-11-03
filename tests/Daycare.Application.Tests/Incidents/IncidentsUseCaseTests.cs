using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Incidents.AcknowledgeIncident;
using Daycare.Application.Features.Incidents.CreateIncident;
using Daycare.Application.Features.Incidents.GetIncidentById;
using Daycare.Application.Features.Incidents.GetIncidents;
using Daycare.Application.Features.Incidents.NotifyParentIncident;
using Daycare.Application.Features.Incidents.UpdateIncident;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Incidents;

public class IncidentsUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();

    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _otherClassroomId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _otherChildId = Guid.NewGuid();
    private readonly Guid _guardianId = Guid.NewGuid();
    private readonly Classroom _classroom;
    private readonly Classroom _otherClassroom;
    private readonly Child _child;
    private readonly Child _otherChild;
    private readonly Guardian _guardian;
    private readonly DateOnly _today = new(2026, 4, 1);
    private readonly DateTime _nowUtc = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

    public IncidentsUseCaseTests()
    {
        _classroom = new Classroom("Toddlers", null, 12, 36, 12, 5, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom, _classroomId);

        _otherClassroom = new Classroom("Infants", null, 3, 12, 8, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_otherClassroom, _otherClassroomId);

        _child = new Child("Abel", "Bekele", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_child, _childId);
        typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(_child, _classroom);

        _otherChild = new Child("Sara", "Bekele", new DateOnly(2025, 1, 1), Gender.Female, new DateOnly(2026, 1, 1), _otherClassroomId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_otherChild, _otherChildId);
        typeof(Child).GetProperty(nameof(Child.Classroom))!.SetValue(_otherChild, _otherClassroom);

        _guardian = new Guardian("Almaz", "Bekele", "+251911223344", "parent@daycare.local");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_guardian, _guardianId);

        _dateTimeProvider.LocalToday.Returns(_today);
        _dateTimeProvider.UtcNow.Returns(_nowUtc);
        _dateTimeProvider.ToLocal(Arg.Any<DateTime>()).Returns(callInfo => callInfo.Arg<DateTime>());

        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.FullName.Returns("Staff One");
        _currentUser.Role.Returns("Staff");
    }

    [Fact]
    public async Task CreateIncident_WhenValid_ShouldCreateIncidentAndReturnDto()
    {
        // Arrange
        var childrenSet = MockDbSetHelper.CreateMockDbSet([_child]);
        _dbContext.Children.Returns(childrenSet);

        var classroomsSet = MockDbSetHelper.CreateMockDbSet([_classroom]);
        _dbContext.Classrooms.Returns(classroomsSet);

        var incidentsList = new List<Incident>();
        var incidentsSet = MockDbSetHelper.CreateMockDbSet(incidentsList);
        _dbContext.Incidents.Returns(incidentsSet);

        var validator = new CreateIncidentValidator();
        var useCase = new CreateIncidentUseCase(_dbContext, _currentUser, _accessGuard, validator);

        var request = new CreateIncidentRequest(
            _childId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor, "Scraped knee",
            ActionTaken: "Applied bandage");

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.Description.ShouldBe("Scraped knee");
        result.Severity.ShouldBe(IncidentSeverity.Minor);
        result.IsSerious.ShouldBeFalse();
        result.ReportedByName.ShouldBe("Staff One");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateIncident_StaffOnSameDay_ShouldSucceed()
    {
        // Arrange
        var incident = new Incident(
            _childId, _classroomId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor,
            "Bumped head", Guid.NewGuid(), "Staff One");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        _currentUser.Role.Returns("Staff");

        var validator = new UpdateIncidentValidator();
        var useCase = new UpdateIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new UpdateIncidentRequest(
            incident.Id, _nowUtc, IncidentType.Injury, IncidentSeverity.Moderate, "Bumped head with swelling",
            ActionTaken: "Applied ice pack");

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.Severity.ShouldBe(IncidentSeverity.Moderate);
        result.Description.ShouldBe("Bumped head with swelling");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateIncident_StaffOnPastDay_ShouldThrowEntryLocked()
    {
        // Arrange
        var yesterdayUtc = _nowUtc.AddDays(-1);
        _dateTimeProvider.ToLocal(yesterdayUtc).Returns(yesterdayUtc);

        var incident = new Incident(
            _childId, _classroomId, yesterdayUtc, IncidentType.Injury, IncidentSeverity.Minor,
            "Bumped head", Guid.NewGuid(), "Staff One");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        _currentUser.Role.Returns("Staff");

        var validator = new UpdateIncidentValidator();
        var useCase = new UpdateIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new UpdateIncidentRequest(
            incident.Id, yesterdayUtc, IncidentType.Injury, IncidentSeverity.Moderate, "Bumped head with swelling");

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Code.ShouldBe("ENTRY_LOCKED");
    }

    [Fact]
    public async Task UpdateIncident_AdminOnPastDay_ShouldSucceed()
    {
        // Arrange
        var yesterdayUtc = _nowUtc.AddDays(-1);
        _dateTimeProvider.ToLocal(yesterdayUtc).Returns(yesterdayUtc);

        var incident = new Incident(
            _childId, _classroomId, yesterdayUtc, IncidentType.Injury, IncidentSeverity.Minor,
            "Bumped head", Guid.NewGuid(), "Staff One");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        _currentUser.Role.Returns("Admin");

        var validator = new UpdateIncidentValidator();
        var useCase = new UpdateIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        var request = new UpdateIncidentRequest(
            incident.Id, yesterdayUtc, IncidentType.Injury, IncidentSeverity.Moderate, "Admin updated note");

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.Description.ShouldBe("Admin updated note");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyParent_ShouldSetTimestampAndNotifierName()
    {
        // Arrange
        var incident = new Incident(
            _childId, _classroomId, _nowUtc, IncidentType.Illness, IncidentSeverity.Moderate,
            "Fever 38.5C", Guid.NewGuid(), "Staff One");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        var validator = new NotifyParentIncidentValidator();
        var useCase = new NotifyParentIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, _accessGuard, validator);

        // Act
        var result = await useCase.ExecuteAsync(new NotifyParentIncidentRequest(incident.Id), CancellationToken.None);

        // Assert
        result.ParentNotifiedAt.ShouldBe(_nowUtc);
        result.ParentNotifiedByName.ShouldBe("Staff One");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcknowledgeIncident_WhenLinkedParent_ShouldSetAcknowledged()
    {
        // Arrange
        var incident = new Incident(
            _childId, _classroomId, _nowUtc, IncidentType.Illness, IncidentSeverity.Moderate,
            "Fever 38.5C", Guid.NewGuid(), "Staff One");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        var link = new ChildGuardian(_childId, _guardianId, Relationship.Mother, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        var linksSet = MockDbSetHelper.CreateMockDbSet([link]);
        _dbContext.ChildGuardians.Returns(linksSet);

        var guardianSet = MockDbSetHelper.CreateMockDbSet([_guardian]);
        _dbContext.Guardians.Returns(guardianSet);

        _currentUser.GuardianId.Returns(_guardianId);
        _currentUser.Role.Returns("Parent");

        var validator = new AcknowledgeIncidentValidator();
        var useCase = new AcknowledgeIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new AcknowledgeIncidentRequest(incident.Id), CancellationToken.None);

        // Assert
        result.AcknowledgedAt.ShouldBe(_nowUtc);
        result.AcknowledgedByGuardianId.ShouldBe(_guardianId);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcknowledgeIncident_WhenNotLinkedParent_ShouldThrowForbidden()
    {
        // Arrange
        var incident = new Incident(
            _childId, _classroomId, _nowUtc, IncidentType.Illness, IncidentSeverity.Moderate,
            "Fever", Guid.NewGuid(), "Staff One");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        // No links for this child and guardian
        var linksSet = MockDbSetHelper.CreateMockDbSet<ChildGuardian>([]);
        _dbContext.ChildGuardians.Returns(linksSet);

        _currentUser.GuardianId.Returns(_guardianId);
        _currentUser.Role.Returns("Parent");

        var validator = new AcknowledgeIncidentValidator();
        var useCase = new AcknowledgeIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new AcknowledgeIncidentRequest(incident.Id), CancellationToken.None));
    }

    [Fact]
    public async Task AcknowledgeIncident_WhenAlreadyAcknowledged_ShouldThrowAlreadyAcknowledged()
    {
        // Arrange
        var incident = new Incident(
            _childId, _classroomId, _nowUtc, IncidentType.Illness, IncidentSeverity.Moderate,
            "Fever", Guid.NewGuid(), "Staff One");
        incident.Acknowledge(_guardianId, _nowUtc.AddMinutes(-10));
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(incident, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(incident, _classroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        var link = new ChildGuardian(_childId, _guardianId, Relationship.Mother, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        var linksSet = MockDbSetHelper.CreateMockDbSet([link]);
        _dbContext.ChildGuardians.Returns(linksSet);

        _currentUser.GuardianId.Returns(_guardianId);
        _currentUser.Role.Returns("Parent");

        var validator = new AcknowledgeIncidentValidator();
        var useCase = new AcknowledgeIncidentUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(new AcknowledgeIncidentRequest(incident.Id), CancellationToken.None));

        ex.Code.ShouldBe("ALREADY_ACKNOWLEDGED");
    }

    [Fact]
    public async Task GetIncidents_StaffScoping_ShouldOnlySeeAssignedClassrooms()
    {
        // Arrange
        var inc1 = new Incident(_childId, _classroomId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor, "Inc 1", Guid.NewGuid(), "Staff");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(inc1, _child);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(inc1, _classroom);

        var inc2 = new Incident(_otherChildId, _otherClassroomId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor, "Inc 2", Guid.NewGuid(), "Staff");
        typeof(Incident).GetProperty(nameof(Incident.Child))!.SetValue(inc2, _otherChild);
        typeof(Incident).GetProperty(nameof(Incident.Classroom))!.SetValue(inc2, _otherClassroom);

        var incidentSet = MockDbSetHelper.CreateMockDbSet([inc1, inc2]);
        _dbContext.Incidents.Returns(incidentSet);

        var staffId = Guid.NewGuid();
        _currentUser.StaffId.Returns(staffId);
        _currentUser.Role.Returns("Staff");

        var staffClassroom = new StaffClassroom(staffId, _classroomId, _nowUtc);
        var scSet = MockDbSetHelper.CreateMockDbSet([staffClassroom]);
        _dbContext.StaffClassrooms.Returns(scSet);

        var validator = new GetIncidentsValidator();
        var useCase = new GetIncidentsUseCase(_dbContext, _currentUser, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetIncidentsRequest(), CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Id.ShouldBe(inc1.Id);
    }

    [Fact]
    public async Task GetIncidentById_WhenCallerUnauthorized_ShouldThrowForbidden()
    {
        // Arrange
        var incident = new Incident(_childId, _classroomId, _nowUtc, IncidentType.Injury, IncidentSeverity.Minor, "Inc 1", Guid.NewGuid(), "Staff");
        var incidentSet = MockDbSetHelper.CreateMockDbSet([incident]);
        _dbContext.Incidents.Returns(incidentSet);

        _accessGuard.EnsureCanViewChildAsync(_childId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Parents may only view children linked to their guardian record.")));

        var validator = new GetIncidentByIdValidator();
        var useCase = new GetIncidentByIdUseCase(_dbContext, _accessGuard, validator);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetIncidentByIdRequest(incident.Id), CancellationToken.None));
    }
}
