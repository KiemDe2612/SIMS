using FluentAssertions;
using SIMS.Infrastructure.Exporters;

namespace SIMS.Tests;

public class CsvInjectionTests
{
    [Theory]
    [InlineData("=1+2", "'=1+2")]
    [InlineData("=SUM(A1:A10)", "'=SUM(A1:A10)")]
    [InlineData("+cmd|' /C calc'!A0", "'+cmd|' /C calc'!A0")]
    [InlineData("-100+50", "'-100+50")]
    [InlineData("@SUM(B1:B5)", "'@SUM(B1:B5)")]
    public void EscapeCell_ShouldPrependSingleQuote_WhenFormulaCharactersPresent(string input, string expected)
    {
        // Act
        var result = CsvExporter.EscapeCell(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EscapeCell_ShouldHandleCommas_ByWrappingInQuotes()
    {
        // Arrange
        var input = "Hà Nội, Việt Nam";

        // Act
        var result = CsvExporter.EscapeCell(input);

        // Assert
        result.Should().Be("\"Hà Nội, Việt Nam\"");
    }

    [Fact]
    public void EscapeCell_ShouldHandleDoubleQuotes_ByDoublingQuotesAndWrapping()
    {
        // Arrange
        var input = "John \"The Expert\" Doe";

        // Act
        var result = CsvExporter.EscapeCell(input);

        // Assert
        result.Should().Be("\"John \"\"The Expert\"\" Doe\"");
    }

    [Fact]
    public void EscapeCell_ShouldHandleBothFormulaAndComma()
    {
        // Arrange
        var input = "=1+2,3";

        // Act
        var result = CsvExporter.EscapeCell(input);

        // Assert: First prepended with ' -> '=1+2,3, then wrapped in quotes due to comma -> "'=1+2,3"
        result.Should().Be("\"'=1+2,3\"");
    }

    [Fact]
    public void EscapeCell_ShouldReturnEmpty_WhenNullOrEmpty()
    {
        CsvExporter.EscapeCell(null).Should().Be(string.Empty);
        CsvExporter.EscapeCell("").Should().Be(string.Empty);
    }

    [Fact]
    public void EscapeCell_ShouldNotModify_NormalText()
    {
        var input = "Software Engineering";
        var result = CsvExporter.EscapeCell(input);
        result.Should().Be("Software Engineering");
    }
}
