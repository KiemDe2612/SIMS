using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.DTOs;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Services;

namespace SIMS.Tests;

public class ExportSecurityTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Faculty>> _mockFacultyRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<Semester>> _mockSemesterRepo;
    private readonly Mock<IGenericRepository<AppUser>> _mockUserRepo;
    private readonly Mock<IGenericRepository<AuditLog>> _mockAuditRepo;
    private readonly ExportService _exportService;

    public ExportSecurityTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockFacultyRepo = new Mock<IGenericRepository<Faculty>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockStudentRepo = new Mock<IGenericRepository<Student>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockSemesterRepo = new Mock<IGenericRepository<Semester>>();
        _mockUserRepo = new Mock<IGenericRepository<AppUser>>();
        _mockAuditRepo = new Mock<IGenericRepository<AuditLog>>();

        _mockUow.Setup(u => u.Repository<Faculty>()).Returns(_mockFacultyRepo.Object);
        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Student>()).Returns(_mockStudentRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.Repository<Department>()).Returns(_mockDeptRepo.Object);
        _mockUow.Setup(u => u.Repository<Semester>()).Returns(_mockSemesterRepo.Object);
        _mockUow.Setup(u => u.Repository<AppUser>()).Returns(_mockUserRepo.Object);
        _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(_mockAuditRepo.Object);

        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _exportService = new ExportService(_mockUow.Object);
    }

    [Fact]
    public async Task Faculty_CannotExportCourseGrades_OfAnotherFaculty()
    {
        // Arrange
        var loggedInFacultyUserId = "faculty-user-1";
        var loggedInFacultyId = Guid.NewGuid();
        var anotherFacultyId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var faculty = new Faculty
        {
            Id = loggedInFacultyId,
            UserId = loggedInFacultyUserId,
            FacultyId = "FAC001"
        };

        var courseOfAnotherFaculty = new Course
        {
            Id = courseId,
            Code = "PRN211",
            Title = "C# Programming",
            AssignedFacultyId = anotherFacultyId // Belongs to someone else!
        };

        _mockFacultyRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Faculty, bool>>>()))
            .ReturnsAsync(new List<Faculty> { faculty });

        _mockCourseRepo
            .Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(courseOfAnotherFaculty);

        // Act & Assert
        var act = async () => await _exportService.ExportFacultyCourseGradesAsync(loggedInFacultyUserId, courseId, ExportFormat.Csv);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không có quyền xuất điểm*");
    }

    [Fact]
    public async Task Faculty_CannotExportGrades_WhenFacultyRecordNotFound()
    {
        // Arrange
        var invalidUserId = "unknown-user";
        var courseId = Guid.NewGuid();

        _mockFacultyRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Faculty, bool>>>()))
            .ReturnsAsync(new List<Faculty>()); // Not found

        // Act & Assert
        var act = async () => await _exportService.ExportFacultyCourseGradesAsync(invalidUserId, courseId, ExportFormat.Csv);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không phải là Giảng viên*");
    }

    [Fact]
    public async Task Faculty_CanExportCourseGrades_WhenAssignedToCourse()
    {
        // Arrange
        var loggedInFacultyUserId = "faculty-user-1";
        var facultyId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var faculty = new Faculty
        {
            Id = facultyId,
            UserId = loggedInFacultyUserId,
            FacultyId = "FAC001"
        };

        var myCourse = new Course
        {
            Id = courseId,
            Code = "PRN211",
            Title = "C# Programming",
            AssignedFacultyId = facultyId // Matches!
        };

        _mockFacultyRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Faculty, bool>>>()))
            .ReturnsAsync(new List<Faculty> { faculty });

        _mockCourseRepo
            .Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(myCourse);

        _mockCourseRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Course> { myCourse });

        _mockStudentRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Student>());

        _mockUserRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<AppUser>());

        _mockSemesterRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Semester>());

        _mockEnrollmentRepo
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync((new List<Enrollment>(), 0));

        // Act
        var result = await _exportService.ExportFacultyCourseGradesAsync(loggedInFacultyUserId, courseId, ExportFormat.Csv);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("text/csv; charset=utf-8");
        result.FileName.Should().Contain("BangDiem_GiangVien_PRN211");
    }

    [Fact]
    public async Task Student_CannotExportGrades_WhenStudentRecordNotFound()
    {
        // Arrange
        var nonExistentStudentUserId = "ghost-student-id";

        _mockStudentRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student>()); // No student record for this user

        // Act & Assert
        var act = async () => await _exportService.ExportStudentGradesAsync(nonExistentStudentUserId, ExportFormat.Csv);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không phải là Sinh viên*");
    }

    [Fact]
    public async Task Student_CannotExportEnrollments_WhenStudentRecordNotFound()
    {
        // Arrange
        var nonExistentStudentUserId = "ghost-student-id";

        _mockStudentRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student>());

        // Act & Assert
        var act = async () => await _exportService.ExportStudentEnrollmentsAsync(nonExistentStudentUserId, ExportFormat.Csv);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không phải là Sinh viên*");
    }

    [Fact]
    public async Task Student_CanExportTheirOwnGrades_Successfully()
    {
        // Arrange
        var studentUserId = "student-user-1";
        var studentId = Guid.NewGuid();

        var student = new Student
        {
            Id = studentId,
            UserId = studentUserId,
            StudentId = "SE12345"
        };

        _mockStudentRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });

        _mockEnrollmentRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>());

        _mockCourseRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Course>());

        _mockSemesterRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Semester>());

        _mockUserRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
            .ReturnsAsync(new List<AppUser> { new AppUser { FirstName = "Nam", LastName = "Nguyen" } });

        // Act
        var result = await _exportService.ExportStudentGradesAsync(studentUserId, ExportFormat.Csv);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("text/csv; charset=utf-8");
        result.FileName.Should().Contain("BangDiem_SE12345");
    }
}

