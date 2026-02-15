namespace MyProject.Component.Tests.Persistence;

/// <summary>
/// Tests for <see cref="MyProject.Infrastructure.Persistence.BaseEntityRepository{TEntity}"/>.
/// <para>
/// These tests require a real database entity registered in the DbContext.
/// Currently the template has no domain entities extending BaseEntity.
/// Once a concrete entity is added (or Testcontainers is available per issue #174),
/// replace the Skip attributes and implement against real data.
/// </para>
/// </summary>
public class BaseEntityRepositoryTests
{
    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task GetById_Existing_ReturnsEntity()
    {
        // Arrange: Add entity via DbContext, then retrieve via repository
        // Assert: entity is returned with matching Id
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task GetById_NonExistent_ReturnsNull()
    {
        // Arrange: empty database
        // Assert: null returned
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task GetById_SoftDeleted_ReturnsNull()
    {
        // Arrange: Add entity, soft-delete it
        // Assert: GetById returns null (global query filter)
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task Add_Valid_ReturnsSuccess()
    {
        // Arrange: Create entity
        // Act: AddAsync + SaveChanges
        // Assert: Result.IsSuccess, entity persisted
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task SoftDelete_Existing_SetsIsDeleted()
    {
        // Arrange: Add entity
        // Act: SoftDeleteAsync + SaveChanges
        // Assert: IsDeleted = true, entity not returned by normal queries
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task Restore_SoftDeleted_ClearsIsDeleted()
    {
        // Arrange: Add + SoftDelete + SaveChanges
        // Act: RestoreAsync + SaveChanges
        // Assert: IsDeleted = false, entity returned by normal queries
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        // Arrange: Add 15 entities
        // Act: GetAllAsync(page: 2, pageSize: 5)
        // Assert: Returns 5 items, ordered by CreatedAt desc
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task Exists_MatchingPredicate_ReturnsTrue()
    {
        // Arrange: Add entity
        // Assert: ExistsAsync with matching predicate returns true
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires a domain entity registered in DbContext — implement with Testcontainers (issue #174)")]
    public Task Exists_NoMatch_ReturnsFalse()
    {
        // Arrange: empty database
        // Assert: ExistsAsync returns false
        return Task.CompletedTask;
    }
}
