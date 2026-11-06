using System.Linq.Expressions;
using System.Text;
using FluentAssertions;
using Moq;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using Xunit;

namespace SIMS.Tests;

public class CsvExportServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStudentService> _studentServiceMock = new();
    private readonly Mock<ICourseService> _courseServiceMock = new();
    private readonly Mock<IFacultyService> _facultyServiceMock = new();
    private readonly Mock<IStudentPortalService> _studentPortalServiceMock = new();
    private readonly Mock<IGenericRepository<Student>> _studentRepoMock = new();
    private readonly Mock<IGenericRepository<Course>> _courseRepoMock = new();
    private readonly Mock<IGenericRepository<Department>> _deptRepoMock = new();
    private readonly Mock<IGenericRepository<Enrollment>> _enrollmentRepoMock = new();
    private readonly Mock<IGenericRepository<Semester>> _semesterRepoMock = new();

    public CsvExportServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Student>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Course>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Department>()).Returns(_deptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Enrollment>()).Returns(_enrollmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Semester>()).Returns(_semesterRepoMock.Object);
    }

    private CsvExportService CreateService()
    {
        return new CsvExportService(
            _unitOfWorkMock.Object,
            _studentServiceMock.Object,
            _courseServiceMock.Object,
            _facultyServiceMock.Object,
            _studentPortalServiceMock.Object);
    }

    private static string DecodeCsvWithoutBom(byte[] bytes)
    {
        // First 3 bytes are UTF-8 BOM
        bytes.Should().HaveCountGreaterThanOrEqualTo(3);
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    [Fact]
    public async Task ExportStudentsAsync_WhenEmptyList_ReturnsHeaderRowOnly()
    {
        _studentServiceMock.Setup(s => s.GetAllStudentsAsync())
            .ReturnsAsync(new List<StudentDto>());

        var sut = CreateService();
        var bytes = await sut.ExportStudentsAsync();

        var text = DecodeCsvWithoutBom(bytes);
        text.Should().Be("Student ID,First Name,Last Name,Email,Department,Date of Birth,Address\r\n");
    }

    [Fact]
    public async Task ExportStudentsAsync_WithData_ExportsFormattedRows()
    {
        var students = new List<StudentDto>
        {
            new()
            {
                StudentId = "SE1001",
                FirstName = "An",
                LastName = "Nguyen, Van",
                Email = "an.nguyen@sims.com",
                DepartmentName = "Software Engineering",
                DateOfBirth = new DateTime(2003, 5, 20),
                Address = "123 Main St, Hanoi"
            }
        };

        var sut = CreateService();
        var bytes = await sut.ExportStudentsAsync(students);

        var text = DecodeCsvWithoutBom(bytes);
        text.Should().Contain("Student ID,First Name,Last Name,Email,Department,Date of Birth,Address\r\n");
        text.Should().Contain("SE1001,An,\"Nguyen, Van\",an.nguyen@sims.com,Software Engineering,2003-05-20,\"123 Main St, Hanoi\"\r\n");
    }

    [Fact]
    public async Task ExportCoursesAsync_WhenEmptyList_ReturnsHeaderRowOnly()
    {
        _courseServiceMock.Setup(c => c.GetAllCoursesAsync())
            .ReturnsAsync(new List<CourseDto>());

        var sut = CreateService();
        var bytes = await sut.ExportCoursesAsync();

        var text = DecodeCsvWithoutBom(bytes);
        text.Should().Be("Course Code,Course Title,Credits,Department,Faculty Assignment\r\n");
    }

    [Fact]
    public async Task ExportCoursesAsync_WithData_ExportsFormattedRows()
    {
        var courses = new List<CourseDto>
        {
            new()
            {
                Code = "PRN211",
                Title = "Basic Cross-Platform App with .NET",
                Credits = 3,
                DepartmentName = "Software Engineering",
                AssignedFacultyName = "Assigned"
            }
        };

        var sut = CreateService();
        var bytes = await sut.ExportCoursesAsync(courses);

        var text = DecodeCsvWithoutBom(bytes);
        text.Should().Contain("PRN211,Basic Cross-Platform App with .NET,3,Software Engineering,Assigned\r\n");
    }

    [Fact]
    public async Task ExportMyTranscriptAsync_OnlyContainsDataForSpecifiedStudent()
    {
        var targetStudentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var courseId1 = Guid.NewGuid();
        var courseId2 = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var targetEnrollments = new List<Enrollment>
        {
            new() { Id = Guid.NewGuid(), StudentId = targetStudentId, CourseId = courseId1, SemesterId = semesterId, Grade = 8.5 },
            new() { Id = Guid.NewGuid(), StudentId = targetStudentId, CourseId = courseId2, SemesterId = semesterId, Grade = 4.0 }
        };

        _enrollmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(targetEnrollments);

        _courseRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Course>
            {
                new() { Id = courseId1, Code = "PRN211", Title = "C# .NET", Credits = 3 },
                new() { Id = courseId2, Code = "CSD201", Title = "Data Structures", Credits = 4 }
            });

        _semesterRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Semester>
            {
                new() { Id = semesterId, Name = "Fall 2026" }
            });

        var sut = CreateService();
        var bytes = await sut.ExportMyTranscriptAsync(targetStudentId);

        var text = DecodeCsvWithoutBom(bytes);

        // Header check
        text.Should().StartWith("Semester,Course Code,Course Title,Credits,Grade,Status\r\n");

        // Rows check
        text.Should().Contain("Fall 2026,PRN211,C# .NET,3,8.5,Passed\r\n");
        text.Should().Contain("Fall 2026,CSD201,Data Structures,4,4.0,Failed\r\n");

        // Verify read-only: No write or complete calls on unit of work
        _enrollmentRepoMock.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _enrollmentRepoMock.Verify(r => r.Update(It.IsAny<Enrollment>()), Times.Never);
        _enrollmentRepoMock.Verify(r => r.Remove(It.IsAny<Enrollment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task ExportCourseGradesAsync_WhenEmptyList_ReturnsHeaderRowOnly()
    {
        var courseId = Guid.NewGuid();
        _facultyServiceMock.Setup(f => f.GetCourseGradesAsync(courseId))
            .ReturnsAsync(new List<CourseGradeDto>());

        var sut = CreateService();
        var bytes = await sut.ExportCourseGradesAsync(courseId);

        var text = DecodeCsvWithoutBom(bytes);
        text.Should().Be("Student ID,Student Name,Grade,Status\r\n");
    }

    [Fact]
    public async Task ExportCourseGradesAsync_WithGrades_FormatsGradesAndStatus()
    {
        var courseId = Guid.NewGuid();
        var grades = new List<CourseGradeDto>
        {
            new() { StudentCode = "SE1001", StudentName = "Alice", Grade = 9.0 },
            new() { StudentCode = "SE1002", StudentName = "Bob", Grade = null }
        };

        _facultyServiceMock.Setup(f => f.GetCourseGradesAsync(courseId))
            .ReturnsAsync(grades);

        var sut = CreateService();
        var bytes = await sut.ExportCourseGradesAsync(courseId);

        var text = DecodeCsvWithoutBom(bytes);
        text.Should().Contain("SE1001,Alice,9.0,Passed\r\n");
        text.Should().Contain("SE1002,Bob,,Not Graded\r\n");
    }

    [Fact]
    public async Task ExportMethods_NeverCallAddUpdateRemoveOrComplete()
    {
        _studentServiceMock.Setup(s => s.GetAllStudentsAsync()).ReturnsAsync(new List<StudentDto>());
        _courseServiceMock.Setup(c => c.GetAllCoursesAsync()).ReturnsAsync(new List<CourseDto>());
        _facultyServiceMock.Setup(f => f.GetCourseGradesAsync(It.IsAny<Guid>())).ReturnsAsync(new List<CourseGradeDto>());

        var sut = CreateService();

        await sut.ExportStudentsAsync();
        await sut.ExportCoursesAsync();
        await sut.ExportCourseGradesAsync(Guid.NewGuid());

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        _studentRepoMock.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Never);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
        _studentRepoMock.Verify(r => r.Remove(It.IsAny<Student>()), Times.Never);
    }
}
