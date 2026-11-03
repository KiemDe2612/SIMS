using FluentAssertions;
using SIMS.Domain.Entities;
using Xunit;

namespace SIMS.Tests;

public class DomainEntitiesTests
{
    [Fact]
    public void AcademicRecord_PropertiesAndNavigation_WorkProperly()
    {
        var studentId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var record = new AcademicRecord
        {
            StudentId = studentId,
            SemesterId = semesterId,
            GPA = 3.65,
            CPA = 3.50,
            AcademicStatus = "Good",
            Student = new Student { Id = studentId },
            Semester = new Semester { Id = semesterId }
        };

        record.StudentId.Should().Be(studentId);
        record.SemesterId.Should().Be(semesterId);
        record.GPA.Should().Be(3.65);
        record.CPA.Should().Be(3.50);
        record.AcademicStatus.Should().Be("Good");
        record.Student.Should().NotBeNull();
        record.Semester.Should().NotBeNull();
    }

    [Fact]
    public void Faculty_PropertiesAndNavigation_WorkProperly()
    {
        var faculty = new Faculty
        {
            UserId = "user-f1",
            FacultyId = "FAC01",
            Title = "Associate Professor",
            HireDate = new DateTime(2020, 1, 1),
            DepartmentId = Guid.NewGuid(),
            User = new AppUser { Id = "user-f1", FirstName = "Nguyen", LastName = "Van A" },
            Department = new Department { Name = "IT" }
        };

        faculty.UserId.Should().Be("user-f1");
        faculty.FacultyId.Should().Be("FAC01");
        faculty.Title.Should().Be("Associate Professor");
        faculty.HireDate.Should().Be(new DateTime(2020, 1, 1));
        faculty.User.Should().NotBeNull();
        faculty.Department.Should().NotBeNull();
        faculty.AssignedCourses.Should().NotBeNull();
    }

    [Fact]
    public void Semester_PropertiesAndNavigation_WorkProperly()
    {
        var sem = new Semester
        {
            Name = "Fall 2026",
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2027, 1, 15),
            IsActive = true
        };

        sem.Name.Should().Be("Fall 2026");
        sem.StartDate.Should().Be(new DateTime(2026, 9, 1));
        sem.EndDate.Should().Be(new DateTime(2027, 1, 15));
        sem.IsActive.Should().BeTrue();
        sem.Enrollments.Should().NotBeNull();
    }

    [Fact]
    public void AuditLog_Properties_WorkProperly()
    {
        var now = DateTime.UtcNow;
        var log = new AuditLog
        {
            UserId = "user-admin",
            Action = "Export",
            EntityName = "Student",
            EntityId = "ALL",
            Changes = "{\"format\":\"CSV\"}",
            CreatedAt = now
        };

        log.UserId.Should().Be("user-admin");
        log.Action.Should().Be("Export");
        log.EntityName.Should().Be("Student");
        log.EntityId.Should().Be("ALL");
        log.Changes.Should().Be("{\"format\":\"CSV\"}");
        log.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Student_PropertiesAndNavigations_WorkProperly()
    {
        var dept = new Department { Id = Guid.NewGuid(), Name = "Software Engineering" };
        var user = new AppUser { Id = "usr1", FirstName = "Nam", LastName = "Le" };
        var enrollment = new Enrollment { Id = Guid.NewGuid() };

        var student = new Student
        {
            UserId = "usr1",
            StudentId = "SE1001",
            DateOfBirth = new DateTime(2003, 5, 20),
            Address = "Hanoi",
            EnrollmentDate = new DateTime(2021, 9, 1),
            DepartmentId = dept.Id,
            Department = dept,
            User = user,
            Enrollments = new List<Enrollment> { enrollment }
        };

        student.UserId.Should().Be("usr1");
        student.StudentId.Should().Be("SE1001");
        student.Department.Should().Be(dept);
        student.User.Should().Be(user);
        student.Enrollments.Should().Contain(enrollment);
    }

    [Fact]
    public void Course_PropertiesAndNavigations_WorkProperly()
    {
        var prereq = new Course { Id = Guid.NewGuid(), Code = "PRN101" };
        var faculty = new Faculty { Id = Guid.NewGuid() };
        var dept = new Department { Id = Guid.NewGuid() };

        var course = new Course
        {
            Code = "PRN211",
            Title = "C# Programming",
            Description = "Learn C# and .NET",
            Credits = 3,
            DepartmentId = dept.Id,
            Department = dept,
            AssignedFacultyId = faculty.Id,
            AssignedFaculty = faculty,
            PrerequisiteCourseId = prereq.Id,
            PrerequisiteCourse = prereq,
            Enrollments = new List<Enrollment>()
        };

        course.Code.Should().Be("PRN211");
        course.Title.Should().Be("C# Programming");
        course.Description.Should().Be("Learn C# and .NET");
        course.Credits.Should().Be(3);
        course.Department.Should().Be(dept);
        course.AssignedFaculty.Should().Be(faculty);
        course.PrerequisiteCourse.Should().Be(prereq);
        course.Enrollments.Should().BeEmpty();
    }

    [Fact]
    public void Enrollment_PropertiesAndNavigations_WorkProperly()
    {
        var student = new Student { Id = Guid.NewGuid() };
        var course = new Course { Id = Guid.NewGuid() };
        var semester = new Semester { Id = Guid.NewGuid() };
        var enrollDate = DateTime.UtcNow;

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            SemesterId = semester.Id,
            EnrollmentDate = enrollDate,
            Grade = 9.5,
            Student = student,
            Course = course,
            Semester = semester
        };

        enrollment.StudentId.Should().Be(student.Id);
        enrollment.CourseId.Should().Be(course.Id);
        enrollment.SemesterId.Should().Be(semester.Id);
        enrollment.EnrollmentDate.Should().Be(enrollDate);
        enrollment.Grade.Should().Be(9.5);
        enrollment.Student.Should().Be(student);
        enrollment.Course.Should().Be(course);
        enrollment.Semester.Should().Be(semester);
    }
}
