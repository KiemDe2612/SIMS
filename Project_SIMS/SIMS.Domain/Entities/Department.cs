namespace SIMS.Domain.Entities;

public class Department : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. SE, AI, IB
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
