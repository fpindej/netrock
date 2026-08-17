using System.Reflection;
using System.Text.RegularExpressions;
using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public partial class ErrorMessagesTests
{
    private static readonly string[] ExpectedNestedClasses =
        ["Auth", "User", "Admin", "Roles", "Pagination", "Server", "Jobs", "Security", "Entity"];

    [GeneratedRegex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$")]
    private static partial Regex SnakeCaseRegex();

    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex PascalBoundaryRegex();

    private static IEnumerable<(Type Type, FieldInfo Field, Error Error)> AllErrors()
    {
        var nestedTypes = typeof(ErrorMessages)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsInitOnly && f.FieldType == typeof(Error));

            foreach (var field in fields)
            {
                var error = (Error?)field.GetValue(null);
                Assert.NotNull(error);
                yield return (type, field, error);
            }
        }
    }

    private static string ToSnakeCase(string pascalCase) =>
        PascalBoundaryRegex().Replace(pascalCase, "_").ToLowerInvariant();

    [Fact]
    public void AllNestedClasses_ShouldExist()
    {
        var nestedTypes = typeof(ErrorMessages)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .Select(t => t.Name)
            .ToHashSet();

        foreach (var expected in ExpectedNestedClasses)
        {
            Assert.Contains(expected, nestedTypes);
        }
    }

    [Fact]
    public void AllErrors_ShouldHaveNonEmptyMessage()
    {
        foreach (var (type, field, error) in AllErrors())
        {
            Assert.False(
                string.IsNullOrWhiteSpace(error.Message),
                $"ErrorMessages.{type.Name}.{field.Name} must have a non-empty message.");
        }
    }

    [Fact]
    public void EachNestedClass_ShouldHaveAtLeastOneError()
    {
        var nestedTypes = typeof(ErrorMessages)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsInitOnly && f.FieldType == typeof(Error))
                .ToList();

            Assert.True(
                fields.Count > 0,
                $"ErrorMessages.{type.Name} should have at least one Error field.");
        }
    }

    [Fact]
    public void ErrorMessages_WithinEachClass_ShouldBeUnique()
    {
        var seen = new Dictionary<(Type Type, string Message), string>();

        foreach (var (type, field, error) in AllErrors())
        {
            var qualifiedName = $"ErrorMessages.{type.Name}.{field.Name}";
            Assert.False(
                seen.ContainsKey((type, error.Message)),
                $"Duplicate error message \"{error.Message}\" found in {qualifiedName} and {seen.GetValueOrDefault((type, error.Message))}.");
            seen[(type, error.Message)] = qualifiedName;
        }
    }

    [Fact]
    public void ErrorCodes_ShouldBeSnakeCase()
    {
        foreach (var (type, field, error) in AllErrors())
        {
            Assert.True(
                SnakeCaseRegex().IsMatch(error.Code),
                $"ErrorMessages.{type.Name}.{field.Name} code \"{error.Code}\" must be snake_case.");
        }
    }

    [Fact]
    public void ErrorCodes_ShouldBeDerivedFromDeclaringClassAndFieldName()
    {
        foreach (var (type, field, error) in AllErrors())
        {
            var expectedCode = $"{ToSnakeCase(type.Name)}_{ToSnakeCase(field.Name)}";
            Assert.True(
                expectedCode == error.Code,
                $"ErrorMessages.{type.Name}.{field.Name} code must be \"{expectedCode}\" but was \"{error.Code}\".");
        }
    }

    [Fact]
    public void ErrorCodes_ShouldBeGloballyUnique()
    {
        var seen = new Dictionary<string, string>();

        foreach (var (type, field, error) in AllErrors())
        {
            var qualifiedName = $"ErrorMessages.{type.Name}.{field.Name}";
            Assert.False(
                seen.ContainsKey(error.Code),
                $"Duplicate error code \"{error.Code}\" found in {qualifiedName} and {seen.GetValueOrDefault(error.Code)}.");
            seen[error.Code] = qualifiedName;
        }
    }
}
