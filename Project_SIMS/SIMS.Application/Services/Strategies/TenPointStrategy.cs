using SIMS.Application.Interfaces;

namespace SIMS.Application.Services.Strategies;

public class TenPointStrategy : IGpaCalculationStrategy
{
    public string ScaleName => "Thang điểm 10 (10-Point Scale)";

    public double ConvertGrade(double gradeOn10Scale)
    {
        return Math.Round(Math.Clamp(gradeOn10Scale, 0.0, 10.0), 2);
    }

    public double CalculateGpa(IEnumerable<GradeItem> semesterGrades)
    {
        var validGrades = semesterGrades.Where(g => g.Grade.HasValue && g.Credits > 0).ToList();
        if (!validGrades.Any()) return 0.0;

        double totalWeightedPoints = validGrades.Sum(g => ConvertGrade(g.Grade!.Value) * g.Credits);
        int totalCredits = validGrades.Sum(g => g.Credits);

        return totalCredits > 0 ? Math.Round(totalWeightedPoints / totalCredits, 2) : 0.0;
    }

    public double CalculateCpa(IEnumerable<GradeItem> allGrades)
    {
        return CalculateGpa(allGrades);
    }

    public string DetermineAcademicStatus(double cpa)
    {
        return cpa switch
        {
            >= 9.0 => "Xuất sắc (Excellent)",
            >= 8.0 => "Giỏi (Good)",
            >= 7.0 => "Khá (Fairly Good)",
            >= 5.0 => "Trung bình (Average)",
            _ => "Yếu (Poor/Probation)"
        };
    }
}

