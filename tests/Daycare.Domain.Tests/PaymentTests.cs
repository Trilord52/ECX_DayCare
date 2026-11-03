using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class PaymentTests
{
    private readonly Guid _invoiceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        var paidAt = DateTime.UtcNow;
        var payment = new Payment(
            _invoiceId,
            1500m,
            PaymentMethod.Telebirr,
            paidAt,
            _userId,
            "Admin User",
            "TX123456",
            "Mobile transfer");

        payment.Amount.ShouldBe(1500m);
        payment.Method.ShouldBe(PaymentMethod.Telebirr);
        payment.Reference.ShouldBe("TX123456");
        payment.PaidAt.ShouldBe(paidAt);
        payment.RecordedByUserId.ShouldBe(_userId);
        payment.RecordedByName.ShouldBe("Admin User");
        payment.VoidedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_NonPositiveAmount_ShouldThrowValidationFailed()
    {
        var ex = Should.Throw<DomainRuleException>(() =>
            new Payment(_invoiceId, 0m, PaymentMethod.Cash, DateTime.UtcNow, _userId, "Admin"));

        ex.Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    public void Void_WhenActive_ShouldSetVoidDetails()
    {
        var payment = new Payment(
            _invoiceId, 1000m, PaymentMethod.Cash, DateTime.UtcNow, _userId, "Admin");
        var voidedAt = DateTime.UtcNow;

        payment.Void("Duplicate receipt entry", _userId, "Admin User", voidedAt);

        payment.VoidedAt.ShouldBe(voidedAt);
        payment.VoidReason.ShouldBe("Duplicate receipt entry");
        payment.VoidedByUserId.ShouldBe(_userId);
        payment.VoidedByName.ShouldBe("Admin User");
    }

    [Fact]
    public void Void_WhenAlreadyVoided_ShouldThrowPaymentAlreadyVoided()
    {
        var payment = new Payment(
            _invoiceId, 1000m, PaymentMethod.Cash, DateTime.UtcNow, _userId, "Admin");
        payment.Void("Initial void", _userId, "Admin User", DateTime.UtcNow);

        var ex = Should.Throw<DomainRuleException>(() =>
            payment.Void("Second void", _userId, "Admin User", DateTime.UtcNow));

        ex.Code.ShouldBe("PAYMENT_ALREADY_VOIDED");
    }
}
