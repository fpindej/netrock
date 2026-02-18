using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using MyProject.Application.Features.Audit;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Audit.Models;
using MyProject.Infrastructure.Features.Audit.Services;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Component.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly MyProjectDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditService _sut;

    private static readonly DateTime FixedUtcNow = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    public AuditServiceTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(FixedUtcNow, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<AuditService>>();
        _sut = new AuditService(_dbContext, _timeProvider, _logger);
    }

    public void Dispose() => _dbContext.Dispose();

    #region LogAsync

    [Fact]
    public async Task LogAsync_ValidAction_PersistsAuditEvent()
    {
        var userId = Guid.NewGuid();

        await _sut.LogAsync(AuditActions.LoginSuccess, userId: userId);

        var stored = Assert.Single(_dbContext.AuditEvents);
        Assert.Equal(AuditActions.LoginSuccess, stored.Action);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal(FixedUtcNow, stored.CreatedAt);
    }

    [Fact]
    public async Task LogAsync_WithAllFields_PersistsCompletely()
    {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        const string metadata = """{"role":"Admin"}""";

        await _sut.LogAsync(
            AuditActions.AdminAssignRole,
            userId: userId,
            targetEntityType: "User",
            targetEntityId: targetId,
            metadata: metadata);

        var stored = Assert.Single(_dbContext.AuditEvents);
        Assert.Equal(AuditActions.AdminAssignRole, stored.Action);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal("User", stored.TargetEntityType);
        Assert.Equal(targetId, stored.TargetEntityId);
        Assert.Equal(metadata, stored.Metadata);
    }

    [Fact]
    public async Task LogAsync_AnonymousEvent_PersistsWithNullUserId()
    {
        await _sut.LogAsync(AuditActions.LoginFailure, metadata: """{"attemptedEmail":"x@y.com"}""");

        var stored = Assert.Single(_dbContext.AuditEvents);
        Assert.Null(stored.UserId);
        Assert.Equal(AuditActions.LoginFailure, stored.Action);
    }

    [Fact]
    public async Task LogAsync_DbFailure_DoesNotThrow()
    {
        // Dispose context to force a DB failure
        _dbContext.Dispose();

        var exception = await Record.ExceptionAsync(() =>
            _sut.LogAsync(AuditActions.LoginSuccess, userId: Guid.NewGuid()));

        Assert.Null(exception);
    }

    #endregion

    #region GetUserAuditEventsAsync

    [Fact]
    public async Task GetUserAuditEventsAsync_ReturnsPaginatedResults()
    {
        var userId = Guid.NewGuid();
        await SeedEventsForUser(userId, 5);

        var result = await _sut.GetUserAuditEventsAsync(userId, pageNumber: 1, pageSize: 3);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Events.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    public async Task GetUserAuditEventsAsync_OrdersByCreatedAtDescending()
    {
        var userId = Guid.NewGuid();
        var baseTime = new DateTimeOffset(FixedUtcNow, TimeSpan.Zero);

        for (var i = 0; i < 3; i++)
        {
            _timeProvider.SetUtcNow(baseTime.AddMinutes(i));
            await _sut.LogAsync($"Action_{i}", userId: userId);
        }

        var result = await _sut.GetUserAuditEventsAsync(userId, pageNumber: 1, pageSize: 10);

        Assert.Equal("Action_2", result.Events[0].Action);
        Assert.Equal("Action_1", result.Events[1].Action);
        Assert.Equal("Action_0", result.Events[2].Action);
    }

    [Fact]
    public async Task GetUserAuditEventsAsync_IncludesEventsWhereUserIsTarget()
    {
        var actorId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        // Event performed BY the target user
        await _sut.LogAsync(AuditActions.LoginSuccess, userId: targetUserId);

        // Event targeting the user (performed by admin)
        await _sut.LogAsync(AuditActions.AdminLockUser, userId: actorId,
            targetEntityType: "User", targetEntityId: targetUserId);

        var result = await _sut.GetUserAuditEventsAsync(targetUserId, pageNumber: 1, pageSize: 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Events, e => e.Action == AuditActions.LoginSuccess);
        Assert.Contains(result.Events, e => e.Action == AuditActions.AdminLockUser);
    }

    [Fact]
    public async Task GetUserAuditEventsAsync_ExcludesUnrelatedEvents()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await _sut.LogAsync(AuditActions.LoginSuccess, userId: userId);
        await _sut.LogAsync(AuditActions.LoginSuccess, userId: otherUserId);

        var result = await _sut.GetUserAuditEventsAsync(userId, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task GetUserAuditEventsAsync_EmptyForUnknownUser()
    {
        var result = await _sut.GetUserAuditEventsAsync(Guid.NewGuid(), pageNumber: 1, pageSize: 10);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task GetUserAuditEventsAsync_SecondPage_ReturnsCorrectSlice()
    {
        var userId = Guid.NewGuid();
        await SeedEventsForUser(userId, 5);

        var result = await _sut.GetUserAuditEventsAsync(userId, pageNumber: 2, pageSize: 3);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Events.Count);
    }

    #endregion

    private async Task SeedEventsForUser(Guid userId, int count)
    {
        var baseTime = new DateTimeOffset(FixedUtcNow, TimeSpan.Zero);
        for (var i = 0; i < count; i++)
        {
            _dbContext.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"Action_{i}",
                CreatedAt = baseTime.AddMinutes(i).UtcDateTime
            });
        }

        await _dbContext.SaveChangesAsync();
    }
}
