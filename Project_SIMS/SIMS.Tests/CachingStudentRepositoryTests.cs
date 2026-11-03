using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Repositories;

namespace SIMS.Tests;

public class CachingStudentRepositoryTests
{
    private readonly Mock<IGenericRepository<Student>> _mockInner;
    private readonly IMemoryCache _memoryCache;
    private readonly CachingStudentRepository _decorator;

    public CachingStudentRepositoryTests()
    {
        _mockInner = new Mock<IGenericRepository<Student>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _decorator = new CachingStudentRepository(_mockInner.Object, _memoryCache);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldCacheResult_OnSubsequentCalls()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student
        {
            Id = studentId,
            StudentId = "SE12345",
            Address = "Hanoi"
        };

        _mockInner.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);

        // Act 1: First call -> fetches from inner repo
        var result1 = await _decorator.GetByIdAsync(studentId);

        // Act 2: Second call -> fetches from cache
        var result2 = await _decorator.GetByIdAsync(studentId);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.Id.Should().Be(studentId);
        result2!.Id.Should().Be(studentId);

        // Verify inner repository was called only ONCE!
        _mockInner.Verify(r => r.GetByIdAsync(studentId), Times.Once);
    }

    [Fact]
    public void Update_ShouldInvalidateCache()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, StudentId = "SE12345" };
        _memoryCache.Set($"student_{studentId}", student);

        // Pre-condition: cache has item
        _memoryCache.TryGetValue($"student_{studentId}", out Student? preCheck).Should().BeTrue();

        // Act
        _decorator.Update(student);

        // Assert: cache invalidated
        _memoryCache.TryGetValue($"student_{studentId}", out Student? postCheck).Should().BeFalse();
        _mockInner.Verify(r => r.Update(student), Times.Once);
    }

    [Fact]
    public void Remove_ShouldInvalidateCache()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, StudentId = "SE12345" };
        _memoryCache.Set($"student_{studentId}", student);

        // Act
        _decorator.Remove(student);

        // Assert
        _memoryCache.TryGetValue($"student_{studentId}", out Student? postCheck).Should().BeFalse();
        _mockInner.Verify(r => r.Remove(student), Times.Once);
    }
}

