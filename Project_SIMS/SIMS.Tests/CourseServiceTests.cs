using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.DTOs;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class CourseServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<Faculty>> _mockFacultyRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly CourseService _courseService;

    public CourseServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockFacultyRepo = new Mock<IGenericRepository<Faculty>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();

        _mockUow.Setup(u => u.Repository<Course>()).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.Repository<Department>()).Returns(_mockDeptRepo.Object);
        _mockUow.Setup(u => u.Repository<Faculty>()).Returns(_mockFacultyRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _courseService = new CourseService(_mockUow.Object);
    }

    [Fact]
    public async Task GetAllCoursesAsync_ShouldReturnCourses_WithCorrectAssignedStatus()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var departments = new List<Department> { new Department { Id = deptId, Name = "SE" } };
        var courses = new List<Course>
        {
            new Course { Id = Guid.NewGuid(), Code = "PRN211", Title = "C#", Credits = 3, DepartmentId = deptId, AssignedFacultyId = Guid.NewGuid() },
            new Course { Id = Guid.NewGuid(), Code = "PRJ301", Title = "Java", Credits = 3, DepartmentId = deptId, AssignedFacultyId = null }
        };

        _mockCourseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(departments);

        // Act
        var result = (await _courseService.GetAllCoursesAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].AssignedFacultyName.Should().Be("Assigned");
        result[1].AssignedFacultyName.Should().Be("Unassigned");
    }

    [Fact]
    public async Task GetCourseByIdAsync_ShouldReturnCourse_WhenExists()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = new Course { Id = courseId, Code = "PRN211", Title = "C#" };
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);

        // Act
        var result = await _courseService.GetCourseByIdAsync(courseId);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("PRN211");
    }

    [Fact]
    public async Task CreateCourseAsync_ShouldAddCourseAndSave()
    {
        // Arrange
        var dto = new CreateCourseDto
        {
            Code = "SWE201",
            Title = "Software Engineering Intro",
            Description = "Overview",
            Credits = 3,
            DepartmentId = Guid.NewGuid()
        };

        // Act
        var result = await _courseService.CreateCourseAsync(dto);

        // Assert
        result.Should().BeTrue();
        _mockCourseRepo.Verify(r => r.AddAsync(It.Is<Course>(c => c.Code == "SWE201" && c.Credits == 3)), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_ShouldUpdateFields_WhenCourseFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = new Course { Id = courseId, Title = "Old Title", Credits = 2 };
        var dto = new UpdateCourseDto
        {
            Id = courseId,
            Title = "Updated Title",
            Description = "Updated Desc",
            Credits = 4,
            DepartmentId = Guid.NewGuid()
        };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(existingCourse);

        // Act
        var result = await _courseService.UpdateCourseAsync(dto);

        // Assert
        result.Should().BeTrue();
        existingCourse.Title.Should().Be("Updated Title");
        existingCourse.Credits.Should().Be(4);
        _mockCourseRepo.Verify(r => r.Update(existingCourse), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_ShouldReturnFalse_WhenCourseNotFound()
    {
        // Arrange
        _mockCourseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);

        // Act
        var result = await _courseService.UpdateCourseAsync(new UpdateCourseDto { Id = Guid.NewGuid() });

        // Assert
        result.Should().BeFalse();
        _mockCourseRepo.Verify(r => r.Update(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task GetDepartmentsAsync_ShouldReturnDepartments()
    {
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department> { new Department { Name = "CS" } });
        var result = await _courseService.GetDepartmentsAsync();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFacultiesAsync_ShouldReturnFaculties()
    {
        _mockFacultyRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Faculty> { new Faculty { FacultyId = "F1" } });
        var result = await _courseService.GetFacultiesAsync();
        result.Should().HaveCount(1);
    }
}
