using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class TranscriptService : ITranscriptService
{
    private IGpaCalculationStrategy _strategy;
    private readonly IUnitOfWork _unitOfWork;

    public TranscriptService(IGpaCalculationStrategy strategy, IUnitOfWork unitOfWork)
    {
        _strategy = strategy;
        _unitOfWork = unitOfWork;
    }

    public IGpaCalculationStrategy CurrentStrategy => _strategy;

    public void SetStrategy(IGpaCalculationStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public GpaCalculationResult Calculate(Guid semesterId, IEnumerable<GradeItem> allGrades)
    {
        var gradeList = allGrades.ToList();
        var semesterGrades = gradeList.Where(g => g.SemesterId == semesterId).ToList();

        double gpa = _strategy.CalculateGpa(semesterGrades);
        double cpa = _strategy.CalculateCpa(gradeList);

        int semesterCredits = semesterGrades.Where(g => g.Grade.HasValue && g.Grade >= 5.0).Sum(g => g.Credits);
        int totalCredits = gradeList.Where(g => g.Grade.HasValue && g.Grade >= 5.0).Sum(g => g.Credits);
        string status = _strategy.DetermineAcademicStatus(cpa);

        return new GpaCalculationResult(gpa, cpa, semesterCredits, totalCredits, status, _strategy.ScaleName);
    }

    public async Task<GpaCalculationResult> CalculateStudentTranscriptAsync(Guid studentId, Guid? currentSemesterId = null)
    {
        var enrollments = (await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == studentId)).ToList();
        var courses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id, c => c);

        var gradeItems = enrollments.Select(e =>
        {
            courses.TryGetValue(e.CourseId, out var c);
            return new GradeItem(
                c?.Code ?? "N/A",
                c?.Title ?? "N/A",
                c?.Credits ?? 0,
                e.SemesterId,
                e.Grade
            );
        }).ToList();

        // If current semester not specified, pick active semester
        Guid targetSemesterId = currentSemesterId ?? Guid.Empty;
        if (targetSemesterId == Guid.Empty)
        {
            var activeSemesters = await _unitOfWork.Repository<Semester>().FindAsync(s => s.IsActive);
            targetSemesterId = activeSemesters.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        return Calculate(targetSemesterId, gradeItems);
    }
}

