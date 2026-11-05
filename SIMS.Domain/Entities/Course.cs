namespace SIMS.Domain.Entities;

public class Course : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. PRN211
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    
    public Guid DepartmentId { get; set; }
    public Guid? AssignedFacultyId { get; set; }
    public Guid? PrerequisiteCourseId { get; set; }
    
    // Navigation properties
    public virtual Department? Department { get; set; }
    public virtual Faculty? AssignedFaculty { get; set; }
    public virtual Course? PrerequisiteCourse { get; set; }
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
