using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Application.Services.Observers;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class EnrollmentObserverAndRulesTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Semester>> _mockSemesterRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly Mock<IEnrollmentSubject> _mockSubject;
    private readonly StudentPortalService _service;

    public EnrollmentObserverAndRulesTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockStudentRepo = new Mock<IGenericRepository<Student>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockSemesterRepo = new Mock<IGenericRepository<Semester>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();
        _mockSubject = new Mock<IEnrollmentSubject>();

        _mockUow.Setup(u => u.Repository<Student>()).Returns(_mockStudentRepo.Object);
        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Semester>()).Returns(_mockSemesterRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _service = new StudentPortalService(_mockUow.Object, _mockSubject.Object);
    }

    [Fact]
    public async Task EnrollmentSubject_ShouldNotifyAllAttachedObservers()
    {
        // Arrange
        var observer1 = new Mock<IEnrollmentObserver>();
        var observer2 = new Mock<IEnrollmentObserver>();

        var subject = new EnrollmentSubject(new[] { observer1.Object, observer2.Object });
        var eventData = new EnrollmentCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SE11111", Guid.NewGuid(), "PRN211", Guid.NewGuid(), DateTime.UtcNow);

        // Act
        await subject.NotifyAsync(eventData);

        // Assert
        observer1.Verify(o => o.OnEnrollmentCreatedAsync(eventData), Times.Once);
        observer2.Verify(o => o.OnEnrollmentCreatedAsync(eventData), Times.Once);
    }

    [Fact]
    public async Task EnrollInCourseAsync_ShouldFail_WhenPrerequisiteNotCompleted()
    {
        // Arrange
        var userId = "user-123";
        var studentId = Guid.NewGuid();
        var prereqCourseId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = userId, StudentId = "SE123" };
        var activeSemester = new Semester { Id = semesterId, IsActive = true };
        var course = new Course
        {
            Id = targetCourseId,
            Code = "PRJ301",
            Credits = 3,
            PrerequisiteCourseId = prereqCourseId // Requires PRN211!
        };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSemester });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(targetCourseId))
            .ReturnsAsync(course);

        // Not already enrolled
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Enrollment, bool>>>(
            expr => expr.Compile()(new Enrollment { StudentId = studentId, CourseId = targetCourseId, SemesterId = semesterId }))))
            .ReturnsAsync(new List<Enrollment>());

        // Student has NOT passed prerequisite course (returns empty)
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Enrollment, bool>>>(
            expr => expr.Compile()(new Enrollment { StudentId = studentId, CourseId = prereqCourseId, Grade = 3.0 }))))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _service.EnrollInCourseAsync(userId, targetCourseId);

        // Assert: Enrollment failed because prerequisite is not met
        result.Should().BeFalse();
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _mockSubject.Verify(s => s.NotifyAsync(It.IsAny<EnrollmentCreatedEvent>()), Times.Never);
    }

    [Fact]
    public async Task EnrollInCourseAsync_ShouldNotifyObserver_WhenEnrollmentSucceeds()
    {
        // Arrange
        var userId = "user-123";
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = userId, StudentId = "SE123" };
        var activeSemester = new Semester { Id = semesterId, IsActive = true };
        var course = new Course { Id = courseId, Code = "PRN211", Credits = 3, PrerequisiteCourseId = null };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSemester });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(course);
        _mockCourseRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Course> { course });

        // Not already enrolled
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _service.EnrollInCourseAsync(userId, courseId);

        // Assert
        result.Should().BeTrue();
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Once);
        _mockSubject.Verify(s => s.NotifyAsync(It.Is<EnrollmentCreatedEvent>(
            e => e.StudentCode == "SE123" && e.CourseCode == "PRN211")), Times.Once);
    }

    [Fact]
    public async Task AuditLogEnrollmentObserver_ShouldPersistAuditLog()
    {
        var mockAuditRepo = new Mock<IGenericRepository<AuditLog>>();
        _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(mockAuditRepo.Object);

        var observer = new AuditLogEnrollmentObserver(_mockUow.Object);
        var eventData = new EnrollmentCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SE999", Guid.NewGuid(), "CS101", Guid.NewGuid(), DateTime.UtcNow);

        await observer.OnEnrollmentCreatedAsync(eventData);

        mockAuditRepo.Verify(r => r.AddAsync(It.Is<AuditLog>(a => 
            a.UserId == "SE999" && a.Action == "Create" && a.EntityName == "Enrollment")), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotificationEnrollmentObserver_ShouldCompleteWithoutThrowing()
    {
        var observer = new NotificationEnrollmentObserver(null);
        var eventData = new EnrollmentCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SE999", Guid.NewGuid(), "CS101", Guid.NewGuid(), DateTime.UtcNow);

        var act = () => observer.OnEnrollmentCreatedAsync(eventData);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void EnrollmentSubject_AttachAndDetach_WorksAsExpected()
    {
        var observerMock = new Mock<IEnrollmentObserver>();
        var subject = new EnrollmentSubject();

        subject.Attach(observerMock.Object);
        subject.Detach(observerMock.Object);
    }
}

