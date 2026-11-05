using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class StudentPortalServiceEnrollmentTests
{
    private const string UserId = "user-1";

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGenericRepository<Student>> _students = new();
    private readonly Mock<IGenericRepository<Semester>> _semesters = new();
    private readonly Mock<IGenericRepository<Enrollment>> _enrollments = new();

    public StudentPortalServiceEnrollmentTests()
    {
        _uow.Setup(u => u.Repository<Student>()).Returns(_students.Object);
        _uow.Setup(u => u.Repository<Semester>()).Returns(_semesters.Object);
        _uow.Setup(u => u.Repository<Enrollment>()).Returns(_enrollments.Object);
        _uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
    }

    // Applies the real predicate to an in-memory list so the service's filters are exercised.
    private static void SetupFind<T>(Mock<IGenericRepository<T>> repo, params T[] data) where T : class =>
        repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<T, bool>>>()))
            .Returns((Expression<Func<T, bool>> p) =>
                Task.FromResult<IEnumerable<T>>(data.Where(p.Compile()).ToList()));


    [Fact]
    public async Task Enroll_ReturnsFalse_WhenStudentNotFound()
    {
        SetupFind<Student>(_students);
        SetupFind(_semesters, new Semester { IsActive = true });
        SetupFind<Enrollment>(_enrollments);

        var result = await new StudentPortalService(_uow.Object).EnrollInCourseAsync(UserId, Guid.NewGuid());

        result.Should().BeFalse();
        _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _uow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Enroll_ReturnsFalse_WhenNoActiveSemester()
    {
        SetupFind(_students, new Student { UserId = UserId });
        SetupFind(_semesters, new Semester { IsActive = false });
        SetupFind<Enrollment>(_enrollments);

        var result = await new StudentPortalService(_uow.Object).EnrollInCourseAsync(UserId, Guid.NewGuid());

        result.Should().BeFalse();
        _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _uow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Enroll_ReturnsFalse_WhenAlreadyEnrolledInSameSemester()
    {
        var student = new Student { UserId = UserId };
        var semester = new Semester { IsActive = true };
        var courseId = Guid.NewGuid();
        SetupFind(_students, student);
        SetupFind(_semesters, semester);
        SetupFind(_enrollments, new Enrollment { StudentId = student.Id, CourseId = courseId, SemesterId = semester.Id });

        var result = await new StudentPortalService(_uow.Object).EnrollInCourseAsync(UserId, courseId);

        result.Should().BeFalse();
        _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _uow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Enroll_Succeeds_AndPersistsEnrollment()
    {
        var student = new Student { UserId = UserId };
        var semester = new Semester { IsActive = true };
        var courseId = Guid.NewGuid();
        SetupFind(_students, student);
        SetupFind(_semesters, semester);
        SetupFind<Enrollment>(_enrollments);

        var result = await new StudentPortalService(_uow.Object).EnrollInCourseAsync(UserId, courseId);

        result.Should().BeTrue();
        _enrollments.Verify(r => r.AddAsync(It.Is<Enrollment>(e =>
            e.StudentId == student.Id && e.CourseId == courseId && e.SemesterId == semester.Id)), Times.Once);
        _uow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task Enroll_Succeeds_WhenEnrolledInSameCourseOnlyInOtherSemester()
    {
        var student = new Student { UserId = UserId };
        var semester = new Semester { IsActive = true };
        var courseId = Guid.NewGuid();
        SetupFind(_students, student);
        SetupFind(_semesters, semester);
        SetupFind(_enrollments, new Enrollment { StudentId = student.Id, CourseId = courseId, SemesterId = Guid.NewGuid() });

        var result = await new StudentPortalService(_uow.Object).EnrollInCourseAsync(UserId, courseId);

        result.Should().BeTrue();
    }
}
