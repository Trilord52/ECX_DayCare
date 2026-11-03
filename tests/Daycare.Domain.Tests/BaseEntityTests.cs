using Daycare.Domain.Common;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity { }

    [Fact]
    public void BaseEntity_ShouldInitializeWithGuidId()
    {
        var entity = new TestEntity();
        entity.Id.ShouldNotBe(Guid.Empty);
    }
}
