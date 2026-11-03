using System.Text;
using FluentAssertions;
using Moq;
using SIMS.Application.DTOs;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Exporters;
using SIMS.Infrastructure.Services;
using System.Linq.Expressions;

namespace SIMS.Tests;

public class ExportFormatAndContentTypeTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<AppUser>> _mockUserRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Semester>> _mockSemesterRepo;
    private readonly Mock<IGenericRepository<AuditLog>> _mockAuditRepo;
    private readonly ExportService _exportService;

    public ExportFormatAndContentTypeTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockStudentRepo = new Mock<IGenericRepository<Student>>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockUserRepo = new Mock<IGenericRepository<AppUser>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockSemesterRepo = new Mock<IGenericRepository<Semester>>();
        _mockAuditRepo = new Mock<IGenericRepository<AuditLog>>();

        _mockUow.Setup(u => u.Repository<Student>()).Returns(_mockStudentRepo.Object);
        _mockUow.Setup(u => u.Repository<Department>()).Returns(_mockDeptRepo.Object);
        _mockUow.Setup(u => u.Repository<AppUser>()).Returns(_mockUserRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Semester>()).Returns(_mockSemesterRepo.Object);
        _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(_mockAuditRepo.Object);

        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _exportService = new ExportService(_mockUow.Object);
    }

    [Fact]
    public async Task CsvExport_ShouldStartWith_Utf8Bom()
    {
        // Arrange
        var headers = new[] { "Col1", "Col2" };
        async IAsyncEnumerable<IEnumerable<object?>> GetRows()
        {
            yield return new object[] { "Val1", "Val2" };
            await Task.CompletedTask;
        }

        // Act
        var bytes = await CsvExporter.ExportAsync(headers, GetRows());

        // Assert: UTF-8 BOM is 0xEF, 0xBB, 0xBF
        bytes.Length.Should().BeGreaterThan(3);
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        // Content verification
        var text = Encoding.UTF8.GetString(bytes);
        text.Should().Contain("Col1,Col2");
        text.Should().Contain("Val1,Val2");
    }

    [Fact]
    public async Task ExportStudents_AsCsv_ShouldHaveCorrectContentTypeAndExtension()
    {
        // Arrange
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());
        _mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AppUser>());
        _mockStudentRepo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), null))
            .ReturnsAsync((new List<Student>(), 0));

        // Act
        var result = await _exportService.ExportStudentsAsync("admin-user", ExportFormat.Csv);

        // Assert
        result.ContentType.Should().Be("text/csv; charset=utf-8");
        result.FileName.Should().EndWith(".csv");
        result.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportStudents_AsExcel_ShouldHaveCorrectContentTypeAndExtension()
    {
        // Arrange
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());
        _mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AppUser>());
        _mockStudentRepo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), null))
            .ReturnsAsync((new List<Student>(), 0));

        // Act
        var result = await _exportService.ExportStudentsAsync("admin-user", ExportFormat.Excel);

        // Assert
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileName.Should().EndWith(".xlsx");
        result.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportStudentGrades_AsPdf_ShouldHaveCorrectContentTypeAndExtension()
    {
        // Arrange
        var studentUserId = "student-123";
        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            StudentId = "SE99999",
            DepartmentId = Guid.NewGuid()
        };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>());
        _mockCourseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course>());
        _mockSemesterRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Semester>());
        _mockDeptRepo.Setup(r => r.GetByIdAsync(student.DepartmentId))
            .ReturnsAsync(new Department { Name = "Software Engineering" });
        _mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
            .ReturnsAsync(new List<AppUser> { new AppUser { FirstName = "An", LastName = "Tran" } });

        // Act
        var result = await _exportService.ExportStudentGradesAsync(studentUserId, ExportFormat.Pdf);

        // Assert
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.FileName.Should().Contain("SE99999");
        result.FileContents.Should().NotBeEmpty();
        // PDF magic header %PDF
        Encoding.ASCII.GetString(result.FileContents.Take(4).ToArray()).Should().Be("%PDF");
    }
}

