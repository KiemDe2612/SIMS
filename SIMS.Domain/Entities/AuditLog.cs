namespace SIMS.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty; // JSON representation of changes
}
