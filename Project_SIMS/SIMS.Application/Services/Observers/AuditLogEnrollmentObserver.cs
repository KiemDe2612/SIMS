using System.Text.Json;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services.Observers;

public class AuditLogEnrollmentObserver : IEnrollmentObserver
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogEnrollmentObserver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task OnEnrollmentCreatedAsync(EnrollmentCreatedEvent eventData)
    {
        var audit = new AuditLog
        {
            UserId = eventData.StudentCode,
            Action = "Create",
            EntityName = "Enrollment",
            EntityId = eventData.EnrollmentId.ToString(),
            Changes = JsonSerializer.Serialize(new
            {
                eventData.StudentCode,
                eventData.CourseCode,
                eventData.SemesterId,
                eventData.EnrollmentDate
            }),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<AuditLog>().AddAsync(audit);
        await _unitOfWork.CompleteAsync();
    }
}

