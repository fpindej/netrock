using MyProject.Infrastructure.Features.Authentication.Models;
using NetArchTest.Rules;

namespace MyProject.Architecture.Tests;

public class AccessModifierTests
{
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(ApplicationUser).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.Identity.Constants.AppRoles).Assembly;

    [Fact]
    public void InfrastructureServices_ShouldNotBePublic()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreClasses()
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void ApplicationInterfaces_ShouldBePublic()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void DomainEntities_ShouldBePublic()
    {
        var result = Types.InAssembly(typeof(Domain.Entities.BaseEntity).Assembly)
            .That()
            .ResideInNamespace("MyProject.Domain.Entities")
            .Should()
            .BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.IsSuccessful) return string.Empty;
        var violators = result.FailingTypeNames ?? [];
        return $"Access modifier violated by: {string.Join(", ", violators)}";
    }
}
