using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIMS.Domain.Entities;
using SIMS.Persistence.Data;

namespace SIMS.Tests;

public class ApplicationDbContextIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextIntegrationTests()
    {
        // Set up SQLite In-Memory Database connection
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        // Create full database schema in-memory
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanInsertAndRetrieve_StudentWithDepartment()
    {
        // Arrange
        var user = new AppUser { Id = "user-1", UserName = "se123@sims.com", Email = "se123@sims.com", FirstName = "A", LastName = "B" };
        await _context.Users.AddAsync(user);

        var dept = new Department
        {
            Code = "SE",
            Name = "Software Engineering",
            Description = "SE Department"
        };
        await _context.Departments.AddAsync(dept);
        await _context.SaveChangesAsync();

        var student = new Student
        {
            StudentId = "SE12345",
            Address = "123 Cau Giay, Hanoi",
            DateOfBirth = new DateTime(2003, 5, 15),
            DepartmentId = dept.Id,
            UserId = user.Id
        };
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == "SE12345");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.StudentId.Should().Be("SE12345");
        retrieved.DepartmentId.Should().Be(dept.Id);
        retrieved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteFilter_ShouldExcludeDeletedEntities_UnlessIgnored()
    {
        // Arrange
        var user = new AppUser { Id = "user-2", UserName = "se999@sims.com", Email = "se999@sims.com", FirstName = "X", LastName = "Y" };
        await _context.Users.AddAsync(user);

        var dept = new Department { Code = "IT", Name = "Information Technology" };
        await _context.Departments.AddAsync(dept);
        await _context.SaveChangesAsync();

        var student = new Student
        {
            StudentId = "SE99999",
            DepartmentId = dept.Id,
            UserId = user.Id,
            IsDeleted = false
        };
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();

        // Verify entity exists
        (await _context.Students.CountAsync()).Should().Be(1);

        // Act: Soft delete student
        student.IsDeleted = true;
        student.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert 1: Standard query ignores soft-deleted student (HasQueryFilter: !e.IsDeleted)
        var countStandard = await _context.Students.CountAsync();
        countStandard.Should().Be(0, "Query filter phải tự động loại bỏ các entity có IsDeleted = true");

        // Assert 2: IgnoreQueryFilters still retrieves the record from SQLite
        var countIgnored = await _context.Students.IgnoreQueryFilters().CountAsync();
        countIgnored.Should().Be(1, "Dữ liệu vẫn còn lưu trữ vật lý trong cơ sở dữ liệu");
    }

    [Fact]
    public async Task CanPersistAndQuery_AuditLogs()
    {
        // Arrange
        var audit = new AuditLog
        {
            UserId = "user-admin",
            Action = "Export",
            EntityName = "Student",
            EntityId = "ALL",
            Changes = "{\"format\":\"Excel\"}"
        };

        // Act
        await _context.AuditLogs.AddAsync(audit);
        await _context.SaveChangesAsync();

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "Export");
        savedLog.Should().NotBeNull();
        savedLog!.UserId.Should().Be("user-admin");
        savedLog.Changes.Should().Contain("Excel");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
