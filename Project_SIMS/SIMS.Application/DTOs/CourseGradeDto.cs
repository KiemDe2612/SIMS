namespace SIMS.Application.DTOs;

public class CourseGradeDto
{
    public Guid EnrollmentId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public double? Grade { get; set; }
}

public class CourseGradeUpdateDto
{
    public Guid EnrollmentId { get; set; }
    public double? Grade { get; set; }
}
