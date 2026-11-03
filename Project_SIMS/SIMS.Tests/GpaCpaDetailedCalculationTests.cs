using FluentAssertions;
using Moq;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Application.Services.Strategies;
using SIMS.Domain.Interfaces;

namespace SIMS.Tests;

public class GpaCpaDetailedCalculationTests
{
    [Fact]
    public void TenPointStrategy_HandCalculated_MultiSemester_Verification()
    {
        // =========================================================================
        // TÍNH TOÁN BẰNG TAY (HANDWRITTEN VERIFICATION FOR 10-POINT SCALE):
        // Học kỳ 1 (Fall 2026):
        // - PRN211: 3 tín chỉ, điểm 8.0 => Trọng số = 3 * 8.0 = 24.0
        // - PRJ301: 4 tín chỉ, điểm 9.0 => Trọng số = 4 * 9.0 = 36.0
        // - SWT301: 3 tín chỉ, điểm 6.5 => Trọng số = 3 * 6.5 = 19.5
        // -> Tổng tín chỉ Kỳ 1 = 3 + 4 + 3 = 10
        // -> Tổng điểm trọng số Kỳ 1 = 24.0 + 36.0 + 19.5 = 79.5
        // -> GPA Kỳ 1 = 79.5 / 10 = 7.95
        //
        // Học kỳ 2 (Spring 2026):
        // - PRO192: 3 tín chỉ, điểm 8.5 => Trọng số = 3 * 8.5 = 25.5
        // - DBI202: 3 tín chỉ, điểm 7.0 => Trọng số = 3 * 7.0 = 21.0
        // -> Tổng tín chỉ Kỳ 2 = 3 + 3 = 6
        // -> Tổng điểm trọng số Kỳ 2 = 25.5 + 21.0 = 46.5
        // -> GPA Kỳ 2 = 46.5 / 6 = 7.75
        //
        // Tích lũy toàn khóa (CPA cả 2 kỳ):
        // -> Tổng tín chỉ tích lũy = 10 + 6 = 16
        // -> Tổng điểm tích lũy = 79.5 + 46.5 = 126.0
        // -> CPA = 126.0 / 16 = 7.875 => Làm tròn 2 chữ số: 7.88
        // -> Xếp loại học lực với CPA 7.88: "Khá (Fairly Good)"
        // =========================================================================

        var strategy = new TenPointStrategy();
        var semester1Id = Guid.NewGuid();
        var semester2Id = Guid.NewGuid();

        var sem1Grades = new List<GradeItem>
        {
            new GradeItem("PRN211", "C# Programming", 3, semester1Id, 8.0),
            new GradeItem("PRJ301", "Java Web Development", 4, semester1Id, 9.0),
            new GradeItem("SWT301", "Software Testing", 3, semester1Id, 6.5)
        };

        var sem2Grades = new List<GradeItem>
        {
            new GradeItem("PRO192", "Object-Oriented Programming", 3, semester2Id, 8.5),
            new GradeItem("DBI202", "Introduction to Databases", 3, semester2Id, 7.0)
        };

        var allGrades = sem1Grades.Concat(sem2Grades).ToList();

        // Act
        double gpaSem1 = strategy.CalculateGpa(sem1Grades);
        double gpaSem2 = strategy.CalculateGpa(sem2Grades);
        double cpaTotal = strategy.CalculateCpa(allGrades);
        string status = strategy.DetermineAcademicStatus(cpaTotal);

        // Assert
        gpaSem1.Should().Be(7.95, "GPA kỳ 1 phải đúng 7.95 theo tính tay");
        gpaSem2.Should().Be(7.75, "GPA kỳ 2 phải đúng 7.75 theo tính tay");
        cpaTotal.Should().Be(7.88, "CPA tích lũy 126.0 / 16 làm tròn phải là 7.88");
        status.Should().Be("Khá (Fairly Good)", "CPA trong khoảng [7.0, 8.0) xếp loại Khá");
    }

    [Fact]
    public void FourPointStrategy_HandCalculated_MultiSemester_Verification()
    {
        // =========================================================================
        // TÍNH TOÁN BẰNG TAY (HANDWRITTEN VERIFICATION FOR 4.0 SCALE):
        // Quy đổi thang điểm chuẩn:
        // - 8.0 (PRN211) -> 3.0 (Điểm B)
        // - 9.0 (PRJ301) -> 4.0 (Điểm A)
        // - 6.5 (SWT301) -> 2.0 (Điểm C)
        // - 8.5 (PRO192) -> 4.0 (Điểm A)
        // - 7.0 (DBI202) -> 3.0 (Điểm B)
        //
        // Học kỳ 1 (Fall 2026):
        // - PRN211: 3 TC * 3.0 = 9.0
        // - PRJ301: 4 TC * 4.0 = 16.0
        // - SWT301: 3 TC * 2.0 = 6.0
        // -> Tổng tín chỉ Kỳ 1 = 10
        // -> Điểm trọng số Kỳ 1 = 9.0 + 16.0 + 6.0 = 31.0
        // -> GPA Kỳ 1 (thang 4) = 31.0 / 10 = 3.10
        //
        // Học kỳ 2 (Spring 2026):
        // - PRO192: 3 TC * 4.0 = 12.0
        // - DBI202: 3 TC * 3.0 = 9.0
        // -> Tổng tín chỉ Kỳ 2 = 6
        // -> Điểm trọng số Kỳ 2 = 12.0 + 9.0 = 21.0
        // -> GPA Kỳ 2 (thang 4) = 21.0 / 6 = 3.50
        //
        // Tích lũy toàn khóa (CPA cả 2 kỳ thang 4):
        // -> Tổng tín chỉ tích lũy = 10 + 6 = 16
        // -> Tổng điểm trọng số tích lũy = 31.0 + 21.0 = 52.0
        // -> CPA (thang 4) = 52.0 / 16 = 3.25
        // -> Xếp loại học lực với CPA 3.25: "Giỏi (Good)" (>= 3.2 và < 3.6)
        // =========================================================================

        var strategy = new FourPointStrategy();
        var semester1Id = Guid.NewGuid();
        var semester2Id = Guid.NewGuid();

        var sem1Grades = new List<GradeItem>
        {
            new GradeItem("PRN211", "C# Programming", 3, semester1Id, 8.0),
            new GradeItem("PRJ301", "Java Web Development", 4, semester1Id, 9.0),
            new GradeItem("SWT301", "Software Testing", 3, semester1Id, 6.5)
        };

        var sem2Grades = new List<GradeItem>
        {
            new GradeItem("PRO192", "Object-Oriented Programming", 3, semester2Id, 8.5),
            new GradeItem("DBI202", "Introduction to Databases", 3, semester2Id, 7.0)
        };

        var allGrades = sem1Grades.Concat(sem2Grades).ToList();

        // Act
        double gpaSem1 = strategy.CalculateGpa(sem1Grades);
        double gpaSem2 = strategy.CalculateGpa(sem2Grades);
        double cpaTotal = strategy.CalculateCpa(allGrades);
        string status = strategy.DetermineAcademicStatus(cpaTotal);

        // Assert
        gpaSem1.Should().Be(3.10, "GPA kỳ 1 thang 4 = 31.0 / 10 = 3.10");
        gpaSem2.Should().Be(3.50, "GPA kỳ 2 thang 4 = 21.0 / 6 = 3.50");
        cpaTotal.Should().Be(3.25, "CPA tích lũy thang 4 = 52.0 / 16 = 3.25");
        status.Should().Be("Giỏi (Good)", "CPA 3.25 trên thang 4 thuộc diện Giỏi");
    }

    [Fact]
    public void TranscriptService_ShouldIntegrateStrategy_AndReturnStructuredTranscript()
    {
        // Arrange
        var mockUow = new Mock<IUnitOfWork>();
        var strategy = new TenPointStrategy();
        var transcriptService = new TranscriptService(strategy, mockUow.Object);

        var semesterId = Guid.NewGuid();
        var grades = new List<GradeItem>
        {
            new GradeItem("PRN211", "C#", 3, semesterId, 9.0),
            new GradeItem("PRJ301", "Java", 4, semesterId, 4.0) // Failed course (Grade < 5.0)
        };

        // Act
        var transcript = transcriptService.Calculate(semesterId, grades);

        // Assert:
        // GPA = (9.0*3 + 4.0*4) / 7 = (27 + 16) / 7 = 43 / 7 = 6.14
        transcript.Gpa.Should().Be(6.14);
        transcript.Cpa.Should().Be(6.14);
        // Only passed courses count towards completed credits: 3 (PRN211 passed, PRJ301 failed)
        transcript.SemesterCredits.Should().Be(3);
        transcript.TotalCumulativeCredits.Should().Be(3);
        transcript.ScaleName.Should().Contain("10");
        transcript.AcademicStatus.Should().Contain("Trung bình");
    }
}

