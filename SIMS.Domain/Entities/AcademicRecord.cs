namespace SIMS.Domain.Entities;

public class AcademicRecord : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public double GPA { get; set; }
    public double CPA { get; set; }
    public string AcademicStatus { get; set; } = string.Empty; // e.g. Good, Warning, Probation
    
    public virtual Student? Student { get; set; }
    public virtual Semester? Semester { get; set; }
}
