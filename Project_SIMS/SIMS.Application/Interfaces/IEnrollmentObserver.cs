namespace SIMS.Application.Interfaces;

public record EnrollmentCreatedEvent(
    Guid EnrollmentId,
    Guid StudentId,
    string StudentCode,
    Guid CourseId,
    string CourseCode,
    Guid SemesterId,
    DateTime EnrollmentDate
);

public interface IEnrollmentObserver
{
    Task OnEnrollmentCreatedAsync(EnrollmentCreatedEvent eventData);
}

public interface IEnrollmentSubject
{
    void Attach(IEnrollmentObserver observer);
    void Detach(IEnrollmentObserver observer);
    Task NotifyAsync(EnrollmentCreatedEvent eventData);
}

