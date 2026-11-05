using System.ComponentModel.DataAnnotations;

namespace SIMS.Application.DTOs;

public class CourseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string AssignedFacultyName { get; set; } = string.Empty;
}

public class CreateCourseDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required, Range(1, 10)]
    public int Credits { get; set; }
    [Required]
    public Guid DepartmentId { get; set; }
    public Guid? AssignedFacultyId { get; set; }
    public Guid? PrerequisiteCourseId { get; set; }
}

public class UpdateCourseDto
{
    public Guid Id { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required, Range(1, 10)]
    public int Credits { get; set; }
    [Required]
    public Guid DepartmentId { get; set; }
    public Guid? AssignedFacultyId { get; set; }
    public Guid? PrerequisiteCourseId { get; set; }
}
