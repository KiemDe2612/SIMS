namespace SIMS.Application.DTOs;

public enum ExportFormat
{
    Csv,
    Excel,
    Pdf
}

public class ExportResult
{
    public byte[] FileContents { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

