using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Test.Infrastructure.Persistence;

namespace Test.Component.Tests.Fixtures;

internal static class TestDbContextFactory
{
    public static TestDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestDbContext(options);
    }
}
