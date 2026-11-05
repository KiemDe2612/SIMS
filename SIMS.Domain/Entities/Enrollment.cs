namespace SIMS.Domain.Entities;

public class Enrollment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public double? Grade { get; set; }
    
    // Navigation properties
    public virtual Student? Student { get; set; }
    public virtual Course? Course { get; set; }
    public virtual Semester? Semester { get; set; }
}
