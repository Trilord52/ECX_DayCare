using Daycare.Application.Common;
using Daycare.Domain.Common;
using Daycare.Infrastructure.Common;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Daycare.Architecture.Tests;

public class ArchitectureTests
{
    private const string DomainNamespace = "Daycare.Domain";
    private const string ApplicationNamespace = "Daycare.Application";
    private const string InfrastructureNamespace = "Daycare.Infrastructure";
    private const string ApiNamespace = "Daycare.Api";

    [Fact]
    public void Domain_ShouldNotDependOn_ApplicationInfrastructureApiOrFrameworks()
    {
        var result = Types.InAssembly(typeof(BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureApiNpgsqlOrAspNetCore()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace,
                "Npgsql",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(InfrastructureAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void UseCases_ShouldResideInApplicationAndInheritFromUseCaseBase()
    {
        var applicationAssembly = typeof(ApplicationAssemblyMarker).Assembly;
        var useCaseTypes = applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("UseCase"))
            .ToList();

        useCaseTypes.ShouldNotBeEmpty();

        foreach (var type in useCaseTypes)
        {
            var baseType = type.BaseType;
            var inheritsFromUseCase = false;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(UseCase<,>))
                {
                    inheritsFromUseCase = true;
                    break;
                }
                baseType = baseType.BaseType;
            }

            inheritsFromUseCase.ShouldBeTrue($"{type.FullName} does not derive from UseCase<TRequest, TResponse>.");
        }
    }

    [Fact]
    public void Controllers_ShouldNotReference_DbContextTypes()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace("Daycare.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore.DbContext",
                "Daycare.Infrastructure.Persistence.ApplicationDbContext",
                "Daycare.Application.Abstractions.IApplicationDbContext")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ImagingLibrary_ShouldOnlyBeReferencedBy_Infrastructure()
    {
        var applicationResult = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SkiaSharp")
            .GetResult();

        applicationResult.IsSuccessful.ShouldBeTrue(string.Join(", ", applicationResult.FailingTypeNames ?? []));

        var apiResult = Types.InAssembly(typeof(Program).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SkiaSharp")
            .GetResult();

        apiResult.IsSuccessful.ShouldBeTrue(string.Join(", ", apiResult.FailingTypeNames ?? []));

        var domainResult = Types.InAssembly(typeof(BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SkiaSharp")
            .GetResult();

        domainResult.IsSuccessful.ShouldBeTrue(string.Join(", ", domainResult.FailingTypeNames ?? []));
    }
}
