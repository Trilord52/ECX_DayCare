namespace Daycare.Application.Features.Billing.FeePlans.DTOs;

public record FeePlanDto(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyAmount,
    bool IsActive
);

