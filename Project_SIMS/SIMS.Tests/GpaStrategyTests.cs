using FluentAssertions;
using Moq;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Application.Services.Strategies;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using System.Linq.Expressions;

namespace SIMS.Tests;

public class GpaStrategyTests
{
    [Fact]
    public void TenPointStrategy_ShouldCalculateWeightedGpa_Correctly()
    {
        // Arrange
        var strategy = new TenPointStrategy();
        var semesterId = Guid.NewGuid();
        var grades = new List<GradeItem>
        {
            new GradeItem("PRN211", "C#", 3, semesterId, 8.0),
            new GradeItem("SWT301", "Testing", 3, semesterId, 6.0),
            new GradeItem("PRJ301", "Java Web", 4, semesterId, 7.5)
        };

        // Act
        // Weighted = (8.0*3 + 6.0*3 + 7.5*4) = 24 + 18 + 30 = 72. Total credits = 10. GPA = 7.2
        double gpa = strategy.CalculateGpa(grades);
        string status = strategy.DetermineAcademicStatus(gpa);

        // Assert
        gpa.Should().Be(7.2);
        status.Should().Contain("Khá");
    }

    [Fact]
    public void FourPointStrategy_ShouldConvertAndCalculateGpa_Correctly()
    {
        // Arrange
        var strategy = new FourPointStrategy();
        var semesterId = Guid.NewGuid();
        var grades = new List<GradeItem>
        {
            // 8.5 -> 4.0 (A)
            new GradeItem("PRN211", "C#", 3, semesterId, 8.5),
            // 7.0 -> 3.0 (B)
            new GradeItem("SWT301", "Testing", 3, semesterId, 7.0),
            // 5.5 -> 2.0 (C)
            new GradeItem("PRJ301", "Java Web", 4, semesterId, 5.5)
        };

        // Act
        // Weighted = (4.0*3 + 3.0*3 + 2.0*4) = 12 + 9 + 8 = 29. Total credits = 10. GPA = 2.9
        double gpa = strategy.CalculateGpa(grades);
        string status = strategy.DetermineAcademicStatus(gpa);

        // Assert
        gpa.Should().Be(2.9);
        status.Should().Contain("Khá");
    }

    [Fact]
    public void TranscriptService_CanSwitchStrategyAtRuntime()
    {
        // Arrange
        var mockUow = new Mock<IUnitOfWork>();
        var tenPointStrategy = new TenPointStrategy();
        var fourPointStrategy = new FourPointStrategy();

        var service = new TranscriptService(tenPointStrategy, mockUow.Object);
        service.CurrentStrategy.Should().BeOfType<TenPointStrategy>();

        var semesterId = Guid.NewGuid();
        var grades = new List<GradeItem>
        {
            new GradeItem("PRN211", "C#", 3, semesterId, 9.0)
        };

        var result10 = service.Calculate(semesterId, grades);
        result10.Gpa.Should().Be(9.0);
        result10.ScaleName.Should().Contain("10");

        // Act: Switch strategy
        service.SetStrategy(fourPointStrategy);
        var result4 = service.Calculate(semesterId, grades);

        // Assert
        result4.Gpa.Should().Be(4.0);
        result4.ScaleName.Should().Contain("4");
    }
}

