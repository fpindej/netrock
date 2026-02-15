using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Component.Tests.Fixtures;

internal static class TestDbContextFactory
{
    public static MyProjectDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<MyProjectDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new MyProjectDbContext(options);
    }
}
