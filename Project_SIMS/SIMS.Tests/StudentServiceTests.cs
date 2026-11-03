using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SIMS.Application.DTOs;
using SIMS.Application.Services;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class StudentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<Enrollment>> _mockEnrollmentRepo;
    private readonly StudentService _studentService;

    public StudentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockStudentRepo = new Mock<IGenericRepository<Student>>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<Enrollment>>();

        _mockUow.Setup(u => u.Repository<Student>()).Returns(_mockStudentRepo.Object);
        _mockUow.Setup(u => u.Repository<Department>()).Returns(_mockDeptRepo.Object);
        _mockUow.Setup(u => u.Repository<Enrollment>()).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _studentService = new StudentService(_mockUow.Object);
    }

    [Fact]
    public async Task GetAllStudentsAsync_ShouldReturnStudents_WithMappedDepartmentNames()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var departments = new List<Department>
        {
            new Department { Id = deptId, Name = "Software Engineering" }
        };

        var students = new List<Student>
        {
            new Student { Id = Guid.NewGuid(), StudentId = "SE001", DepartmentId = deptId, Address = "HN" },
            new Student { Id = Guid.NewGuid(), StudentId = "SE002", DepartmentId = Guid.NewGuid(), Address = "HCM" }
        };

        _mockStudentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(students);
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(departments);

        // Act
        var result = (await _studentService.GetAllStudentsAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].DepartmentName.Should().Be("Software Engineering");
        result[1].DepartmentName.Should().Be("N/A"); // Unmapped dept defaults to N/A
    }

    [Fact]
    public async Task GetStudentByIdAsync_ShouldReturnStudent_WhenExists()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student
        {
            Id = studentId,
            StudentId = "SE12345",
            Address = "Da Nang",
            DateOfBirth = new DateTime(2002, 5, 20),
            DepartmentId = Guid.NewGuid()
        };

        _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);

        // Act
        var result = await _studentService.GetStudentByIdAsync(studentId);

        // Assert
        result.Should().NotBeNull();
        result!.StudentId.Should().Be("SE12345");
        result.Address.Should().Be("Da Nang");
    }

    [Fact]
    public async Task GetStudentByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _mockStudentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Student?)null);

        // Act
        var result = await _studentService.GetStudentByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateStudentAsync_ShouldAddEntityAndSave()
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            StudentId = "SE999",
            FirstName = "Van",
            LastName = "Le",
            Email = "le@sims.com",
            Address = "Can Tho",
            DateOfBirth = new DateTime(2003, 1, 1),
            DepartmentId = Guid.NewGuid(),
            UserId = "user-guid-999"
        };

        // Act
        var result = await _studentService.CreateStudentAsync(dto);

        // Assert
        result.Should().BeTrue();
        _mockStudentRepo.Verify(r => r.AddAsync(It.Is<Student>(s => s.StudentId == "SE999" && s.UserId == "user-guid-999")), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateStudentAsync_ShouldUpdateProperties_WhenStudentExists()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var existingStudent = new Student
        {
            Id = studentId,
            StudentId = "SE111",
            Address = "Old Address"
        };

        var updateDto = new UpdateStudentDto
        {
            Id = studentId,
            Address = "New Address 123",
            DateOfBirth = new DateTime(2002, 10, 10),
            DepartmentId = Guid.NewGuid()
        };

        _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(existingStudent);

        // Act
        var result = await _studentService.UpdateStudentAsync(updateDto);

        // Assert
        result.Should().BeTrue();
        existingStudent.Address.Should().Be("New Address 123");
        _mockStudentRepo.Verify(r => r.Update(existingStudent), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateStudentAsync_ShouldReturnFalse_WhenStudentNotFound()
    {
        // Arrange
        _mockStudentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Student?)null);

        // Act
        var result = await _studentService.UpdateStudentAsync(new UpdateStudentDto { Id = Guid.NewGuid() });

        // Assert
        result.Should().BeFalse();
        _mockStudentRepo.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public async Task GetDepartmentsAsync_ShouldReturnAllDepartments()
    {
        var departments = new List<Department>
        {
            new Department { Id = Guid.NewGuid(), Name = "IT" },
            new Department { Id = Guid.NewGuid(), Name = "Business" }
        };
        _mockDeptRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(departments);

        var result = await _studentService.GetDepartmentsAsync();

        result.Should().HaveCount(2);
    }
}
