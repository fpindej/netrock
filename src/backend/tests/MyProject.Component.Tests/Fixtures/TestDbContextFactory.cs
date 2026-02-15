using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Component.Tests.Fixtures;

internal static class TestDbContextFactory
{
    public static MyProjectDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<MyProjectDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new MyProjectDbContext(options);
    }
}
