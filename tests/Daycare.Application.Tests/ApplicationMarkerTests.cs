using Daycare.Application.Common;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests;

public class ApplicationMarkerTests
{
    [Fact]
    public void ApplicationAssembly_ShouldLoadSuccessfully()
    {
        typeof(ApplicationAssemblyMarker).Assembly.ShouldNotBeNull();
    }
}
