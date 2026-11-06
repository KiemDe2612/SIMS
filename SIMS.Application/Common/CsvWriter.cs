using System.Text;

namespace SIMS.Application.Common;

/// <summary>
/// Helper class for generating CSV output following RFC 4180 rules,
/// with UTF-8 BOM encoding and CSV formula injection mitigation.
/// </summary>
public static class CsvWriter
{
    private static readonly char[] InjectionPrefixes = { '=', '+', '-', '@', '\t', '\r' };

    /// <summary>
    /// Escapes a single CSV cell according to RFC 4180 and prepends a single quote
    /// if the field begins with potentially dangerous formula injection characters.
    /// </summary>
    public static string EscapeField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // CSV injection defense: prefix dangerous leading characters with a single quote
        if (value.Length > 0 && InjectionPrefixes.Contains(value[0]))
        {
            value = "'" + value;
        }

        // RFC 4180: quote field if it contains comma, double-quote, CR or LF
        if (value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Writes a single CSV row (comma-separated, without trailing newline).
    /// </summary>
    public static string WriteRow(IEnumerable<string?> fields)
    {
        return string.Join(",", fields.Select(EscapeField));
    }

    /// <summary>
    /// Builds full CSV content with header row and data rows separated by CRLF (\r\n).
    /// </summary>
    public static string BuildCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string?>> rows)
    {
        var sb = new StringBuilder();
        sb.Append(WriteRow(headers));
        sb.Append("\r\n");

        foreach (var row in rows)
        {
            sb.Append(WriteRow(row));
            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Encodes CSV string to byte array prefixed with UTF-8 BOM (0xEF, 0xBB, 0xBF).
    /// </summary>
    public static byte[] ToUtf8BytesWithBom(string csvContent)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);

        var result = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);

        return result;
    }
}

