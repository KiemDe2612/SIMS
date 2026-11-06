using SIMS.Application.DTOs;

namespace SIMS.Application.Interfaces;

public interface ICsvExportService
{
    /// <summary>
    /// Exports all students to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportStudentsAsync();

    /// <summary>
    /// Exports a specific collection of students to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportStudentsAsync(IEnumerable<StudentDto> students);

    /// <summary>
    /// Exports all courses to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportCoursesAsync();

    /// <summary>
    /// Exports a specific collection of courses to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportCoursesAsync(IEnumerable<CourseDto> courses);

    /// <summary>
    /// Exports academic transcript for a specific student entity ID to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportMyTranscriptAsync(Guid studentId);

    /// <summary>
    /// Exports academic transcript for a student identified by Identity userId to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportMyTranscriptAsync(string userId);

    /// <summary>
    /// Exports grades of all students enrolled in a specific course to a CSV byte array with UTF-8 BOM.
    /// </summary>
    Task<byte[]> ExportCourseGradesAsync(Guid courseId);
}

