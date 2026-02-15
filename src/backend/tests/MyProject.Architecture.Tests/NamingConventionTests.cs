using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace MyProject.Architecture.Tests;

public class NamingConventionTests
{
    private static readonly System.Reflection.Assembly WebApiAssembly = typeof(Program).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.Identity.Constants.AppRoles).Assembly;

    [Fact]
    public void Controllers_ShouldEndWithController()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .That()
            .Inherit(typeof(ControllerBase))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Controllers_ShouldInheritFromControllerBase()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .Inherit(typeof(ControllerBase))
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Validators_ShouldEndWithValidator()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Interfaces_InApplication_ShouldStartWithI()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.IsSuccessful) return string.Empty;
        var violators = result.FailingTypeNames ?? [];
        return $"Naming convention violated by: {string.Join(", ", violators)}";
    }
}
