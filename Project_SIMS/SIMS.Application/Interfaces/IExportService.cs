using SIMS.Application.DTOs;

namespace SIMS.Application.Interfaces;

public interface IExportService
{
    // Administrator
    Task<ExportResult> ExportStudentsAsync(string userId, ExportFormat format);
    Task<ExportResult> ExportCoursesAsync(string userId, ExportFormat format);
    Task<ExportResult> ExportEnrollmentsBySemesterAsync(string userId, Guid? semesterId, ExportFormat format);
    Task<ExportResult> ExportCourseGradesAsync(string userId, Guid courseId, ExportFormat format);

    // Faculty (validates assigned faculty)
    Task<ExportResult> ExportFacultyCourseGradesAsync(string facultyUserId, Guid courseId, ExportFormat format);

    // Student (validates student identity)
    Task<ExportResult> ExportStudentGradesAsync(string studentUserId, ExportFormat format);
    Task<ExportResult> ExportStudentEnrollmentsAsync(string studentUserId, ExportFormat format);
}

