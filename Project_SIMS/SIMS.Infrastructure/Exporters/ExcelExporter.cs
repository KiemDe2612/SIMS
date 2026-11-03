using ClosedXML.Excel;

namespace SIMS.Infrastructure.Exporters;

public static class ExcelExporter
{
    public static async Task<byte[]> ExportAsync(string sheetName, string[] headers, IAsyncEnumerable<IEnumerable<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);

        // Header style
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Write data rows
        int rowIdx = 2;
        await foreach (var row in rows)
        {
            int colIdx = 1;
            foreach (var item in row)
            {
                var cell = worksheet.Cell(rowIdx, colIdx);
                if (item == null)
                {
                    cell.Value = string.Empty;
                }
                else if (item is int intVal)
                {
                    cell.Value = intVal;
                }
                else if (item is double dblVal)
                {
                    cell.Value = dblVal;
                }
                else if (item is decimal decVal)
                {
                    cell.Value = decVal;
                }
                else if (item is DateTime dtVal)
                {
                    cell.Value = dtVal.ToString("yyyy-MM-dd");
                }
                else
                {
                    cell.Value = item.ToString();
                }
                colIdx++;
            }
            rowIdx++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}

