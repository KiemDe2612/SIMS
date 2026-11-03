namespace SIMS.Application.Interfaces;

public record GradeItem(string CourseCode, string CourseTitle, int Credits, Guid SemesterId, double? Grade);

public record GpaCalculationResult(
    double Gpa, 
    double Cpa, 
    int SemesterCredits, 
    int TotalCumulativeCredits, 
    string AcademicStatus, 
    string ScaleName
);

public interface IGpaCalculationStrategy
{
    string ScaleName { get; }
    
    /// <summary>
    /// Chuyển đổi điểm số thang 10 sang thang điểm của chiến lược (ví dụ thang 10 hoặc thang 4).
    /// </summary>
    double ConvertGrade(double gradeOn10Scale);

    /// <summary>
    /// Tính GPA cho một học kỳ cụ thể.
    /// </summary>
    double CalculateGpa(IEnumerable<GradeItem> semesterGrades);

    /// <summary>
    /// Tính CPA tích lũy cho toàn bộ các môn đã học.
    /// </summary>
    double CalculateCpa(IEnumerable<GradeItem> allGrades);

    /// <summary>
    /// Xếp loại học lực dựa trên CPA.
    /// </summary>
    string DetermineAcademicStatus(double cpa);
}

