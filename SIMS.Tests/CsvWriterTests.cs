using System.Text;
using FluentAssertions;
using SIMS.Application.Common;
using Xunit;

namespace SIMS.Tests;

public class CsvWriterTests
{
    [Fact]
    public void EscapeField_WhenNullOrEmpty_ReturnsEmptyString()
    {
        CsvWriter.EscapeField(null).Should().BeEmpty();
        CsvWriter.EscapeField(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void EscapeField_WhenPlainText_ReturnsUnchanged()
    {
        CsvWriter.EscapeField("Software Engineering").Should().Be("Software Engineering");
        CsvWriter.EscapeField("SE12345").Should().Be("SE12345");
    }

    [Fact]
    public void EscapeField_WhenContainsComma_WrapsInQuotes()
    {
        CsvWriter.EscapeField("Nguyen, Van A").Should().Be("\"Nguyen, Van A\"");
    }

    [Fact]
    public void EscapeField_WhenContainsDoubleQuotes_DoublesQuotesAndWraps()
    {
        CsvWriter.EscapeField("Intro to \"OOP\"").Should().Be("\"Intro to \"\"OOP\"\"\"");
    }

    [Fact]
    public void EscapeField_WhenContainsNewline_WrapsInQuotes()
    {
        CsvWriter.EscapeField("Line1\nLine2").Should().Be("\"Line1\nLine2\"");
        CsvWriter.EscapeField("Line1\r\nLine2").Should().Be("\"Line1\r\nLine2\"");
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-25", "'-25")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\tcalc", "'\tcalc")]
    public void EscapeField_WhenFormulaPrefix_PrependsSingleQuote(string input, string expected)
    {
        CsvWriter.EscapeField(input).Should().Be(expected);
    }

    [Fact]
    public void EscapeField_WhenFormulaPrefixContainsComma_PrependsSingleQuoteAndQuotes()
    {
        // =SUM(1, 2) starts with = so gets ' prefix, and contains comma so gets wrapped in quotes
        CsvWriter.EscapeField("=SUM(1, 2)").Should().Be("\"'=SUM(1, 2)\"");
    }

    [Fact]
    public void BuildCsv_WithEmptyDataRows_ReturnsHeadersRowOnly()
    {
        var headers = new[] { "Code", "Title", "Credits" };
        var rows = Enumerable.Empty<IEnumerable<string?>>();

        var csv = CsvWriter.BuildCsv(headers, rows);

        csv.Should().Be("Code,Title,Credits\r\n");
    }

    [Fact]
    public void BuildCsv_WithMultipleRows_FormatsRFC4180()
    {
        var headers = new[] { "ID", "Name" };
        var rows = new List<string?[]>
        {
            new[] { "1", "Alice, M." },
            new[] { "2", "Bob \"The Builder\"" }
        };

        var csv = CsvWriter.BuildCsv(headers, rows);

        csv.Should().Be("ID,Name\r\n1,\"Alice, M.\"\r\n2,\"Bob \"\"The Builder\"\"\"\r\n");
    }

    [Fact]
    public void ToUtf8BytesWithBom_IncludesBomAndEncodesVietnameseCorrectly()
    {
        var text = "Mã SV,Họ và Tên\r\nSE101,Nguyễn Văn An\r\n";
        var bytes = CsvWriter.ToUtf8BytesWithBom(text);

        // UTF-8 BOM is 0xEF, 0xBB, 0xBF
        bytes.Should().HaveCountGreaterThan(3);
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        // Decode back without the 3 BOM bytes should match the original text
        var decoded = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        decoded.Should().Be(text);
    }
}

