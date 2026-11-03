namespace SIMS.Application.Interfaces;

public interface ITranscriptService
{
    IGpaCalculationStrategy CurrentStrategy { get; }
    void SetStrategy(IGpaCalculationStrategy strategy);
    GpaCalculationResult Calculate(Guid semesterId, IEnumerable<GradeItem> allGrades);
    Task<GpaCalculationResult> CalculateStudentTranscriptAsync(Guid studentId, Guid? currentSemesterId = null);
}

