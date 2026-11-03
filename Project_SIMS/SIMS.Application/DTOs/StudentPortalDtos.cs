namespace SIMS.Application.DTOs;

public class MyProfileDto
{
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
}

public class AvailableCourseDto
{
    public Guid CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsEnrolled { get; set; }
}

public class MyGradeDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public double? Grade { get; set; }
}
