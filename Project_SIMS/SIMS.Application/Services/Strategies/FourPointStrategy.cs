using SIMS.Application.Interfaces;

namespace SIMS.Application.Services.Strategies;

public class FourPointStrategy : IGpaCalculationStrategy
{
    public string ScaleName => "Thang điểm 4 (4.0 Scale)";

    public double ConvertGrade(double gradeOn10Scale)
    {
        return gradeOn10Scale switch
        {
            >= 8.5 => 4.0, // A
            >= 7.0 => 3.0, // B
            >= 5.5 => 2.0, // C
            >= 4.0 => 1.0, // D
            _ => 0.0       // F
        };
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
            >= 3.6 => "Xuất sắc (Excellent)",
            >= 3.2 => "Giỏi (Good)",
            >= 2.5 => "Khá (Fairly Good)",
            >= 2.0 => "Trung bình (Average)",
            _ => "Yếu (Poor/Probation)"
        };
    }
}

