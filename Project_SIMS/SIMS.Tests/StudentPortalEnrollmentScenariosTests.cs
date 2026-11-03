using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class StudentPortalEnrollmentScenariosTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Semester>> _mockSemesterRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly Mock<IEnrollmentSubject> _mockSubject;
    private readonly StudentPortalService _service;

    public StudentPortalEnrollmentScenariosTests()
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
    public async Task Scenario1_Enrollment_Success_WhenAllConditionsMet()
    {
        // Arrange
        var userId = "student-user-1";
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = userId, StudentId = "SE12345" };
        var activeSemester = new Semester { Id = semesterId, Name = "Fall 2026", IsActive = true };
        var course = new Course { Id = courseId, Code = "PRN211", Title = "C# .NET", Credits = 3, PrerequisiteCourseId = null };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSemester });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(course);
        _mockCourseRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Course> { course });

        // Currently no existing enrollments for this course or semester
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _service.EnrollInCourseAsync(userId, courseId);

        // Assert
        result.Should().BeTrue();
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.Is<Enrollment>(
            e => e.StudentId == studentId && e.CourseId == courseId && e.SemesterId == semesterId)), Times.Once);
        _mockSubject.Verify(s => s.NotifyAsync(It.Is<EnrollmentCreatedEvent>(
            e => e.StudentCode == "SE12345" && e.CourseCode == "PRN211")), Times.Once);
    }

    [Fact]
    public async Task Scenario2_Enrollment_Fails_WhenAlreadyEnrolledInActiveSemester()
    {
        // Arrange
        var userId = "student-user-1";
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = userId, StudentId = "SE12345" };
        var activeSemester = new Semester { Id = semesterId, Name = "Fall 2026", IsActive = true };
        var course = new Course { Id = courseId, Code = "PRN211", Credits = 3 };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSemester });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(course);

        // Mock already enrolled: find returns an existing enrollment record
        var existingEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            SemesterId = semesterId,
            EnrollmentDate = DateTime.UtcNow.AddDays(-5)
        };

        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.EnrollInCourseAsync(userId, courseId);

        // Assert: rejected because student is already enrolled
        result.Should().BeFalse();
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
        _mockSubject.Verify(s => s.NotifyAsync(It.IsAny<EnrollmentCreatedEvent>()), Times.Never);
    }

    [Fact]
    public async Task Scenario3_Enrollment_Fails_WhenExceedsMaxCreditLimit()
    {
        // Arrange
        var userId = "student-user-1";
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = userId, StudentId = "SE12345" };
        var activeSemester = new Semester { Id = semesterId, Name = "Fall 2026", IsActive = true };

        // Target course has 4 credits
        var targetCourse = new Course { Id = targetCourseId, Code = "PRJ301", Credits = 4, PrerequisiteCourseId = null };

        // Existing courses in semester already total 22 credits (limit is 24, 22 + 4 = 26 > 24!)
        var c1 = new Course { Id = Guid.NewGuid(), Code = "C1", Credits = 10 };
        var c2 = new Course { Id = Guid.NewGuid(), Code = "C2", Credits = 12 };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSemester });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(targetCourseId))
            .ReturnsAsync(targetCourse);
        _mockCourseRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Course> { targetCourse, c1, c2 });

        // Existing enrollments setup
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Enrollment, bool>>>(
            expr => expr.Compile()(new Enrollment { StudentId = studentId, CourseId = targetCourseId, SemesterId = semesterId }))))
            .ReturnsAsync(new List<Enrollment>()); // Not enrolled in target course yet

        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Enrollment, bool>>>(
            expr => expr.Compile()(new Enrollment { StudentId = studentId, SemesterId = semesterId }))))
            .ReturnsAsync(new List<Enrollment>
            {
                new Enrollment { StudentId = studentId, CourseId = c1.Id, SemesterId = semesterId },
                new Enrollment { StudentId = studentId, CourseId = c2.Id, SemesterId = semesterId }
            });

        // Act
        var result = await _service.EnrollInCourseAsync(userId, targetCourseId);

        // Assert: 22 + 4 = 26 > 24 max limit -> rejected!
        result.Should().BeFalse();
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public async Task Scenario4_Enrollment_Fails_WhenPrerequisiteNotCompletedOrFailed()
    {
        // Arrange
        var userId = "student-user-1";
        var studentId = Guid.NewGuid();
        var prereqCourseId = Guid.NewGuid();
        var advancedCourseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = userId, StudentId = "SE12345" };
        var activeSemester = new Semester { Id = semesterId, Name = "Fall 2026", IsActive = true };

        // Advanced course requires prerequisite course
        var advancedCourse = new Course
        {
            Id = advancedCourseId,
            Code = "SWT301",
            Credits = 3,
            PrerequisiteCourseId = prereqCourseId
        };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSemester });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(advancedCourseId))
            .ReturnsAsync(advancedCourse);

        // Return empty for passed prerequisite (student got grade < 5.0 or never enrolled)
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Enrollment, bool>>>(
            expr => expr.Compile()(new Enrollment { StudentId = studentId, CourseId = prereqCourseId, Grade = 4.0 }))))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _service.EnrollInCourseAsync(userId, advancedCourseId);

        // Assert: rejected because prerequisite grade >= 5.0 is missing
        result.Should().BeFalse();
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public async Task Scenario5_Enrollment_Fails_WhenNoActiveSemesterFound()
    {
        // Arrange
        var userId = "student-user-1";
        var courseId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), UserId = userId, StudentId = "SE12345" };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });

        // No active semester
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester>());

        // Act
        var result = await _service.EnrollInCourseAsync(userId, courseId);

        // Assert
        result.Should().BeFalse();
        _mockCourseRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>()), Times.Never);
    }
}

