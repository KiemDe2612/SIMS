using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class SoftDeleteAndValidationTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly StudentService _studentService;
    private readonly CourseService _courseService;

    public SoftDeleteAndValidationTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockStudentRepo = new Mock<IGenericRepository<Student>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();

        _mockUow.Setup(u => u.Repository<Student>()).Returns(_mockStudentRepo.Object);
        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _studentService = new StudentService(_mockUow.Object);
        _courseService = new CourseService(_mockUow.Object);
    }

    [Fact]
    public async Task DeleteStudentAsync_ShouldThrowException_WhenStudentHasActiveEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, StudentId = "SE12345" };

        _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment> { new Enrollment { StudentId = studentId } });

        // Act & Assert
        var act = async () => await _studentService.DeleteStudentAsync(studentId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không thể xóa*lịch sử đăng ký*");
        _mockStudentRepo.Verify(r => r.Remove(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public async Task DeleteStudentAsync_ShouldSoftDelete_WhenNoEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, StudentId = "SE12345", IsDeleted = false };

        _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>()); // No enrollments

        // Act
        var result = await _studentService.DeleteStudentAsync(studentId);

        // Assert
        result.Should().BeTrue();
        student.IsDeleted.Should().BeTrue();
        student.UpdatedAt.Should().NotBeNull();
        _mockStudentRepo.Verify(r => r.Update(student), Times.Once);
        _mockStudentRepo.Verify(r => r.Remove(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCourseAsync_ShouldThrowException_WhenCourseHasEnrollments()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = new Course { Id = courseId, Code = "PRN211" };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment> { new Enrollment { CourseId = courseId } });

        // Act & Assert
        var act = async () => await _courseService.DeleteCourseAsync(courseId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không thể xóa*đăng ký học phần*");
        _mockCourseRepo.Verify(r => r.Remove(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCourseAsync_ShouldSoftDelete_WhenNoEnrollments()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = new Course { Id = courseId, Code = "PRN211", IsDeleted = false };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);
        _mockEnrollmentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Enrollment, bool>>>()))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _courseService.DeleteCourseAsync(courseId);

        // Assert
        result.Should().BeTrue();
        course.IsDeleted.Should().BeTrue();
        course.UpdatedAt.Should().NotBeNull();
        _mockCourseRepo.Verify(r => r.Update(course), Times.Once);
        _mockCourseRepo.Verify(r => r.Remove(It.IsAny<Course>()), Times.Never);
    }
}

