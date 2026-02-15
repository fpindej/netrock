using MyProject.Domain.Entities;

namespace MyProject.Unit.Tests.Domain;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity;

    [Fact]
    public void NewEntity_ShouldNotBeDeleted()
    {
        var entity = new TestEntity();

        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedTrue()
    {
        var entity = new TestEntity();

        entity.SoftDelete();

        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void SoftDelete_CalledTwice_ShouldBeIdempotent()
    {
        var entity = new TestEntity();

        entity.SoftDelete();
        entity.SoftDelete();

        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void Restore_ShouldSetIsDeletedFalse()
    {
        var entity = new TestEntity();
        entity.SoftDelete();

        entity.Restore();

        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Restore_ShouldClearDeletedAt()
    {
        var entity = new TestEntity();
        entity.SoftDelete();

        entity.Restore();

        Assert.Null(entity.DeletedAt);
    }

    [Fact]
    public void Restore_ShouldClearDeletedBy()
    {
        var entity = new TestEntity();
        entity.SoftDelete();

        entity.Restore();

        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeIdempotent()
    {
        var entity = new TestEntity();

        entity.Restore();

        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void SoftDelete_ThenRestore_ShouldRoundTrip()
    {
        var entity = new TestEntity();

        entity.SoftDelete();
        Assert.True(entity.IsDeleted);

        entity.Restore();
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void NewEntity_ShouldHaveDefaultId()
    {
        var entity = new TestEntity();

        Assert.Equal(Guid.Empty, entity.Id);
    }

    [Fact]
    public void NewEntity_ShouldHaveDefaultAuditFields()
    {
        var entity = new TestEntity();

        Assert.Equal(default, entity.CreatedAt);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }
}
