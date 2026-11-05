namespace SIMS.Domain.Entities;

public class Faculty : BaseEntity
{
    public string UserId { get; set; } = string.Empty; // FK to AppUser
    public string FacultyId { get; set; } = string.Empty; // e.g. F123
    public string Title { get; set; } = string.Empty; // e.g. Dr, Prof
    public DateTime HireDate { get; set; }
    public Guid DepartmentId { get; set; }
    
    // Navigation properties
    public virtual AppUser? User { get; set; }
    public virtual Department? Department { get; set; }
    public virtual ICollection<Course> AssignedCourses { get; set; } = new List<Course>();
}
