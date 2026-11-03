using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SIMS.Infrastructure.Exporters;

public record StudentTranscriptItem(string CourseCode, string CourseTitle, int Credits, string Semester, double? Grade);

public static class PdfExporter
{
    static PdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerateStudentTranscript(
        string studentId,
        string fullName,
        string department,
        List<StudentTranscriptItem> items)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("SIMS - STUDENT INFORMATION MANAGEMENT SYSTEM")
                                .Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            titleCol.Item().Text("BẢNG ĐIỂM HỌC TẬP SINH VIÊN (OFFICIAL TRANSCRIPT)")
                                .Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Mã sinh viên: {studentId}").Bold();
                            c.Item().Text($"Họ và tên: {fullName}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Chuyên ngành: {department}").Bold();
                            c.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}");
                        });
                    });

                    col.Item().Height(15);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // STT
                        columns.ConstantColumn(80);  // Mã môn
                        columns.RelativeColumn(3);   // Tên môn
                        columns.ConstantColumn(60);  // Tín chỉ
                        columns.RelativeColumn(2);   // Học kỳ
                        columns.ConstantColumn(60);  // Điểm
                        columns.ConstantColumn(80);  // Kết quả
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("STT").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Mã môn").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Tên môn học").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Tín chỉ").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Học kỳ").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Điểm").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Kết quả").Bold().FontColor(Colors.White);
                    });

                    int stt = 1;
                    foreach (var item in items)
                    {
                        var bgColor = stt % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                        var resultText = !item.Grade.HasValue ? "Đang học" : (item.Grade.Value >= 5.0 ? "Đạt (Pass)" : "Không đạt");
                        var resultColor = !item.Grade.HasValue ? Colors.Grey.Darken2 : (item.Grade.Value >= 5.0 ? Colors.Green.Darken2 : Colors.Red.Darken2);

                        table.Cell().Background(bgColor).Padding(5).Text(stt.ToString());
                        table.Cell().Background(bgColor).Padding(5).Text(item.CourseCode).Bold();
                        table.Cell().Background(bgColor).Padding(5).Text(item.CourseTitle);
                        table.Cell().Background(bgColor).Padding(5).Text(item.Credits.ToString());
                        table.Cell().Background(bgColor).Padding(5).Text(item.Semester);
                        table.Cell().Background(bgColor).Padding(5).Text(item.Grade.HasValue ? item.Grade.Value.ToString("0.0") : "-");
                        table.Cell().Background(bgColor).Padding(5).Text(resultText).FontColor(resultColor).Bold();
                        stt++;
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Hệ thống Quản lý Sinh viên SIMS").FontSize(9).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}

