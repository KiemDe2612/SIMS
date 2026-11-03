using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.DTOs;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using Xunit;

namespace SIMS.Tests;

public class FacultyServiceAndPortalAdditionalTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Faculty>> _mockFacultyRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<AppUser>> _mockUserRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<Semester>> _mockSemesterRepo;

    public FacultyServiceAndPortalAdditionalTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockFacultyRepo = new Mock<IGenericRepository<Faculty>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();
        _mockStudentRepo = new Mock<IGenericRepository<Student>>();
        _mockUserRepo = new Mock<IGenericRepository<AppUser>>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockSemesterRepo = new Mock<IGenericRepository<Semester>>();

        _mockUow.Setup(u => u.Repository<Faculty>()).Returns(_mockFacultyRepo.Object);
        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.Repository<Student>()).Returns(_mockStudentRepo.Object);
        _mockUow.Setup(u => u.Repository<AppUser>()).Returns(_mockUserRepo.Object);
        _mockUow.Setup(u => u.Repository<Department>()).Returns(_mockDeptRepo.Object);
        _mockUow.Setup(u => u.Repository<Semester>()).Returns(_mockSemesterRepo.Object);
    }

    #region FacultyService Tests

    [Fact]
    public async Task FacultyService_GetAssignedCourses_WhenFacultyNotFound_ReturnsEmpty()
    {
        _mockFacultyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Faculty, bool>>>()))
            .ReturnsAsync(new List<Faculty>());

        var service = new FacultyService(_mockUow.Object);
        var result = await service.GetAssignedCoursesAsync("non-existent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FacultyService_GetAssignedCourses_WhenFacultyExists_ReturnsAssignedCourses()
    {
        var facultyId = Guid.NewGuid();
        var faculty = new Faculty { Id = facultyId, UserId = "fac-1", FacultyId = "F001" };
        var courses = new List<Course>
        {
            new Course { Id = Guid.NewGuid(), Code = "CS101", Title = "Intro CS", AssignedFacultyId = facultyId }
        };

        _mockFacultyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Faculty, bool>>>()))
            .ReturnsAsync(new List<Faculty> { faculty });
        _mockCourseRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Course, bool>>>()))
            .ReturnsAsync(courses);

        var service = new FacultyService(_mockUow.Object);
        var result = await service.GetAssignedCoursesAsync("fac-1");

        result.Should().HaveCount(1);
        result.First().Code.Should().Be("CS101");
    }

    [Fact]
    public async Task FacultyService_GetCourseByIdAsync_ReturnsCourse()
    {
        var courseId = Guid.NewGuid();
        var course = new Course { Id = courseId, Code = "CS101", Title = "Intro CS" };
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);

        var service = new FacultyService(_mockUow.Object);
        var result = await service.GetCourseByIdAsync(courseId);

        result.Should().NotBeNull();
        result!.Code.Should().Be("CS101");
    }

    [Fact]
    public async Task FacultyService_GetCourseGradesAsync_MapsStudentAndUserInformation()
    {
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var userId = "user-s1";

        var enrollment = new Enrollment { Id = Guid.NewGuid(), CourseId = courseId, StudentId = studentId, Grade = 8.5 };
        var student = new Student { Id = studentId, StudentId = "S12345", UserId = userId };
        var appUser = new AppUser { Id = userId, FirstName = "Nguyen", LastName = "An" };

        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });
        _mockStudentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Student> { student });
        _mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AppUser> { appUser });

        var service = new FacultyService(_mockUow.Object);
        var result = (await service.GetCourseGradesAsync(courseId)).ToList();

        result.Should().HaveCount(1);
        result[0].StudentCode.Should().Be("S12345");
        result[0].StudentName.Should().Be("Nguyen An");
        result[0].Grade.Should().Be(8.5);
    }

    [Fact]
    public async Task FacultyService_UpdateGradesAsync_UpdatesExistingEnrollments()
    {
        var enrollmentId = Guid.NewGuid();
        var existingEnrollment = new Enrollment { Id = enrollmentId, Grade = 5.0 };

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId)).ReturnsAsync(existingEnrollment);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new FacultyService(_mockUow.Object);
        await service.UpdateGradesAsync(new[]
        {
            new CourseGradeUpdateDto { EnrollmentId = enrollmentId, Grade = 9.0 }
        });

        existingEnrollment.Grade.Should().Be(9.0);
        _mockEnrollmentRepo.Verify(r => r.Update(existingEnrollment), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    #endregion

    #region StudentPortal Additional Tests

    [Fact]
    public async Task StudentPortalService_GetMyProfileAsync_WhenStudentExists_ReturnsProfileWithRealName()
    {
        var studentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var userId = "usr-stu";

        var student = new Student
        {
            Id = studentId,
            StudentId = "SV001",
            UserId = userId,
            DepartmentId = deptId,
            DateOfBirth = new DateTime(2002, 1, 1),
            Address = "123 Le Loi"
        };
        var dept = new Department { Id = deptId, Name = "Computer Science" };
        var user = new AppUser { Id = userId, FirstName = "Tran", LastName = "Binh", Email = "binh@sims.com" };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockDeptRepo.Setup(r => r.GetByIdAsync(deptId)).ReturnsAsync(dept);
        _mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppUser, bool>>>()))
            .ReturnsAsync(new List<AppUser> { user });

        var service = new StudentPortalService(_mockUow.Object);
        var profile = await service.GetMyProfileAsync(userId);

        profile.Should().NotBeNull();
        profile!.FullName.Should().Be("Tran Binh");
        profile.Email.Should().Be("binh@sims.com");
        profile.DepartmentName.Should().Be("Computer Science");
        profile.StudentId.Should().Be("SV001");
    }

    [Fact]
    public async Task StudentPortalService_GetMyProfileAsync_WhenStudentNotFound_ReturnsNull()
    {
        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student>());

        var service = new StudentPortalService(_mockUow.Object);
        var profile = await service.GetMyProfileAsync("unknown-user");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task StudentPortalService_GetAvailableCoursesAsync_FlagsEnrolledCourses()
    {
        var studentId = Guid.NewGuid();
        var semId = Guid.NewGuid();
        var course1Id = Guid.NewGuid();
        var course2Id = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = "user-1", StudentId = "SV1" };
        var activeSem = new Semester { Id = semId, Name = "Fall 2026", IsActive = true };
        var courses = new List<Course>
        {
            new Course { Id = course1Id, Code = "CS1", Title = "Course 1", Credits = 3 },
            new Course { Id = course2Id, Code = "CS2", Title = "Course 2", Credits = 3 }
        };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockCourseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { activeSem });
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>
            {
                new Enrollment { Id = Guid.NewGuid(), CourseId = course1Id, StudentId = studentId, SemesterId = semId }
            });

        var service = new StudentPortalService(_mockUow.Object);
        var result = (await service.GetAvailableCoursesAsync("user-1")).ToList();

        result.Should().HaveCount(2);
        result.First(c => c.CourseId == course1Id).IsEnrolled.Should().BeTrue();
        result.First(c => c.CourseId == course2Id).IsEnrolled.Should().BeFalse();
    }

    [Fact]
    public async Task StudentPortalService_DropCourseAsync_WhenEnrolled_DropsSuccessfully()
    {
        var studentId = Guid.NewGuid();
        var semId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollment = new Enrollment { Id = Guid.NewGuid(), StudentId = studentId, CourseId = courseId, SemesterId = semId };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { new Student { Id = studentId, UserId = "user-1" } });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { new Semester { Id = semId, IsActive = true } });
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var service = new StudentPortalService(_mockUow.Object);
        var success = await service.DropCourseAsync("user-1", courseId);

        success.Should().BeTrue();
        _mockEnrollmentRepo.Verify(r => r.Remove(enrollment), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task StudentPortalService_DropCourseAsync_WhenEnrollmentNotFound_ReturnsFalse()
    {
        var studentId = Guid.NewGuid();
        var semId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { new Student { Id = studentId, UserId = "user-1" } });
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { new Semester { Id = semId, IsActive = true } });
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>());

        var service = new StudentPortalService(_mockUow.Object);
        var success = await service.DropCourseAsync("user-1", courseId);

        success.Should().BeFalse();
    }

    [Fact]
    public async Task StudentPortalService_GetMyGradesAsync_ReturnsStudentGradesWithDetails()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semId = Guid.NewGuid();

        var student = new Student { Id = studentId, UserId = "user-1" };
        var course = new Course { Id = courseId, Code = "CS101", Title = "Programming", Credits = 4 };
        var semester = new Semester { Id = semId, Name = "Fall 2026" };
        var enrollment = new Enrollment { Id = Guid.NewGuid(), StudentId = studentId, CourseId = courseId, SemesterId = semId, Grade = 8.5 };

        _mockStudentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Student, bool>>>()))
            .ReturnsAsync(new List<Student> { student });
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });
        _mockCourseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { course });
        _mockSemesterRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Semester> { semester });

        var service = new StudentPortalService(_mockUow.Object);
        var grades = (await service.GetMyGradesAsync("user-1")).ToList();

        grades.Should().HaveCount(1);
        grades[0].CourseCode.Should().Be("CS101");
        grades[0].Credits.Should().Be(4);
        grades[0].Grade.Should().Be(8.5);
        grades[0].SemesterName.Should().Be("Fall 2026");
    }

    #endregion
}
