using Daycare.Domain.Enums;

namespace Daycare.Application.Features.Billing.Invoices.DTOs;

public record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    Guid ChildId,
    string ChildName,
    Guid GuardianId,
    string GuardianName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    string Currency,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    bool IsOverdue
);

public record InvoiceLineDto(
    Guid Id,
    InvoiceLineType LineType,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder
);

public record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTime PaidAt,
    string? Notes,
    Guid RecordedByUserId,
    string RecordedByName,
    DateTime? VoidedAt,
    string? VoidReason,
    string? VoidedByName
);

public record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid ChildId,
    string ChildName,
    Guid GuardianId,
    string GuardianName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    string Currency,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    bool IsOverdue,
    string? VoidReason,
    DateTime? VoidedAt,
    List<InvoiceLineDto> Lines,
    List<PaymentDto> Payments
);

public record SkippedChildDto(
    Guid ChildId,
    string ChildName,
    string Code
);

public record GenerateInvoicesResultDto(
    int Created,
    List<Guid> InvoiceIds,
    List<SkippedChildDto> Skipped
);

