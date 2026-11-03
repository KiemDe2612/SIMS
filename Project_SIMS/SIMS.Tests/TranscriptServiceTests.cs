using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.Services;
using SIMS.Application.Services.Strategies;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using Xunit;

namespace SIMS.Tests;

public class TranscriptServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Semester>> _mockSemesterRepo;

    public TranscriptServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockSemesterRepo = new Mock<IGenericRepository<Semester>>();

        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Semester>()).Returns(_mockSemesterRepo.Object);
    }

    [Fact]
    public async Task CalculateStudentTranscriptAsync_WithExplicitSemester_CalculatesGpaAndCpa()
    {
        var studentId = Guid.NewGuid();
        var semester1Id = Guid.NewGuid();
        var course1Id = Guid.NewGuid();
        var course2Id = Guid.NewGuid();

        var enrollments = new List<Enrollment>
        {
            new Enrollment { StudentId = studentId, CourseId = course1Id, SemesterId = semester1Id, Grade = 8.0 },
            new Enrollment { StudentId = studentId, CourseId = course2Id, SemesterId = semester1Id, Grade = 7.0 }
        };
        var courses = new List<Course>
        {
            new Course { Id = course1Id, Code = "CS101", Title = "Intro", Credits = 3 },
            new Course { Id = course2Id, Code = "CS102", Title = "Data Struct", Credits = 4 }
        };

        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(enrollments);
        _mockCourseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);

        var strategy = new TenPointStrategy();
        var transcriptService = new TranscriptService(strategy, _mockUow.Object);

        var result = await transcriptService.CalculateStudentTranscriptAsync(studentId, semester1Id);

        result.Should().NotBeNull();
        result.ScaleName.Should().Be(strategy.ScaleName);
        result.Gpa.Should().BeApproximately(7.43, 0.01);
        result.TotalCumulativeCredits.Should().Be(7);
        result.SemesterCredits.Should().Be(7);
    }

    [Fact]
    public async Task CalculateStudentTranscriptAsync_WithoutSemester_UsesActiveSemester()
    {
        var studentId = Guid.NewGuid();
        var activeSemId = Guid.NewGuid();
        var course1Id = Guid.NewGuid();

        var enrollments = new List<Enrollment>
        {
            new Enrollment { StudentId = studentId, CourseId = course1Id, SemesterId = activeSemId, Grade = 9.0 }
        };
        var courses = new List<Course>
        {
            new Course { Id = course1Id, Code = "CS101", Title = "Intro", Credits = 3 }
        };

        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(enrollments);
        _mockCourseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);
        _mockSemesterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Semester, bool>>>()))
            .ReturnsAsync(new List<Semester> { new Semester { Id = activeSemId, IsActive = true } });

        var fourPointStrategy = new FourPointStrategy();
        var transcriptService = new TranscriptService(new TenPointStrategy(), _mockUow.Object);
        transcriptService.SetStrategy(fourPointStrategy);

        transcriptService.CurrentStrategy.Should().Be(fourPointStrategy);

        var result = await transcriptService.CalculateStudentTranscriptAsync(studentId, null);

        result.Should().NotBeNull();
        result.ScaleName.Should().Be(fourPointStrategy.ScaleName);
        result.Gpa.Should().Be(4.0);
    }
}
