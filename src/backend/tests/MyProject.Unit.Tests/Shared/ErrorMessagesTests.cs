using System.Reflection;
using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class ErrorMessagesTests
{
    private static readonly string[] ExpectedNestedClasses =
        ["Auth", "User", "Admin", "Roles", "Pagination", "Server", "Jobs", "Security", "Entity"];

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
    public void AllConstStringFields_ShouldBeNonNullAndNonEmpty()
    {
        var nestedTypes = typeof(ErrorMessages)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = (string?)field.GetRawConstantValue();
                Assert.False(
                    string.IsNullOrEmpty(value),
                    $"ErrorMessages.{type.Name}.{field.Name} must not be null or empty.");
            }
        }
    }

    [Fact]
    public void EachNestedClass_ShouldHaveAtLeastOneConstant()
    {
        var nestedTypes = typeof(ErrorMessages)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .ToList();

            Assert.True(
                fields.Count > 0,
                $"ErrorMessages.{type.Name} should have at least one const string field.");
        }
    }

    [Fact]
    public void ErrorMessages_WithinEachClass_ShouldBeUnique()
    {
        var nestedTypes = typeof(ErrorMessages)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var seen = new Dictionary<string, string>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = (string?)field.GetRawConstantValue();
                if (value is null) continue;

                var qualifiedName = $"ErrorMessages.{type.Name}.{field.Name}";
                Assert.False(
                    seen.ContainsKey(value),
                    $"Duplicate error message value \"{value}\" found in {qualifiedName} and {seen.GetValueOrDefault(value)}.");
                seen[value] = qualifiedName;
            }
        }
    }
}
