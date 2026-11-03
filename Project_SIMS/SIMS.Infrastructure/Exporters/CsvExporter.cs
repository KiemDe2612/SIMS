using System.Text;

namespace SIMS.Infrastructure.Exporters;

public static class CsvExporter
{
    /// <summary>
    /// Escapes cell value to prevent CSV Injection (formula injection) and handles RFC 4180 delimiter escaping.
    /// </summary>
    public static string EscapeCell(object? val)
    {
        if (val == null) return string.Empty;
        var str = val.ToString() ?? string.Empty;

        // Escape CSV injection: if starts with =, +, -, @, prepend single quote '
        if (str.StartsWith('=') || str.StartsWith('+') || str.StartsWith('-') || str.StartsWith('@'))
        {
            str = "'" + str;
        }

        // Standard CSV RFC 4180 escaping: if contains comma, quote, or newline
        if (str.Contains(',') || str.Contains('"') || str.Contains('\r') || str.Contains('\n'))
        {
            str = "\"" + str.Replace("\"", "\"\"") + "\"";
        }

        return str;
    }

    public static string FormatRow(IEnumerable<object?> values)
    {
        return string.Join(",", values.Select(EscapeCell));
    }

    /// <summary>
    /// Writes CSV bytes with UTF-8 BOM, streaming rows asynchronously to keep memory usage minimal.
    /// </summary>
    public static async Task<byte[]> ExportAsync(string[] headers, IAsyncEnumerable<IEnumerable<object?>> rows)
    {
        using var ms = new MemoryStream();
        // UTF-8 with BOM: true
        using (var writer = new StreamWriter(ms, new UTF8Encoding(true), bufferSize: 4096, leaveOpen: true))
        {
            // Header row
            await writer.WriteLineAsync(FormatRow(headers));

            // Stream rows
            await foreach (var row in rows)
            {
                await writer.WriteLineAsync(FormatRow(row));
            }

            await writer.FlushAsync();
        }

        return ms.ToArray();
    }
}
