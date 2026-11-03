namespace SIMS.Domain.Entities;

public class Semester : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g. Fall 2024
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
