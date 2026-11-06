using System.Globalization;
using SIMS.Application.Common;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class CsvExportService : ICsvExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly IFacultyService _facultyService;
    private readonly IStudentPortalService _studentPortalService;

    public CsvExportService(
        IUnitOfWork unitOfWork,
        IStudentService studentService,
        ICourseService courseService,
        IFacultyService facultyService,
        IStudentPortalService studentPortalService)
    {
        _unitOfWork = unitOfWork;
        _studentService = studentService;
        _courseService = courseService;
        _facultyService = facultyService;
        _studentPortalService = studentPortalService;
    }

    public async Task<byte[]> ExportStudentsAsync()
    {
        var students = await _studentService.GetAllStudentsAsync();
        return await ExportStudentsAsync(students);
    }

    public Task<byte[]> ExportStudentsAsync(IEnumerable<StudentDto> students)
    {
        var headers = new[]
        {
            "Student ID", "First Name", "Last Name", "Email", "Department", "Date of Birth", "Address"
        };

        var rows = (students ?? Enumerable.Empty<StudentDto>()).Select(s => new string?[]
        {
            s.StudentId,
            s.FirstName,
            s.LastName,
            s.Email,
            s.DepartmentName,
            s.DateOfBirth.ToString("yyyy-MM-dd"),
            s.Address
        });

        var csv = CsvWriter.BuildCsv(headers, rows);
        return Task.FromResult(CsvWriter.ToUtf8BytesWithBom(csv));
    }

    public async Task<byte[]> ExportCoursesAsync()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return await ExportCoursesAsync(courses);
    }

    public Task<byte[]> ExportCoursesAsync(IEnumerable<CourseDto> courses)
    {
        var headers = new[]
        {
            "Course Code", "Course Title", "Credits", "Department", "Faculty Assignment"
        };

        var rows = (courses ?? Enumerable.Empty<CourseDto>()).Select(c => new string?[]
        {
            c.Code,
            c.Title,
            c.Credits.ToString(),
            c.DepartmentName,
            c.AssignedFacultyName
        });

        var csv = CsvWriter.BuildCsv(headers, rows);
        return Task.FromResult(CsvWriter.ToUtf8BytesWithBom(csv));
    }

    public async Task<byte[]> ExportMyTranscriptAsync(Guid studentId)
    {
        var enrollments = (await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == studentId)).ToList();
        var courses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id);
        var semesters = (await _unitOfWork.Repository<Semester>().GetAllAsync()).ToDictionary(s => s.Id);

        var headers = new[]
        {
            "Semester", "Course Code", "Course Title", "Credits", "Grade", "Status"
        };

        var rows = enrollments.Select(e =>
        {
            courses.TryGetValue(e.CourseId, out var course);
            semesters.TryGetValue(e.SemesterId, out var sem);

            var gradeText = e.Grade.HasValue ? e.Grade.Value.ToString("F1", CultureInfo.InvariantCulture) : string.Empty;
            var status = !e.Grade.HasValue ? "In Progress" : (e.Grade.Value >= 5.0 ? "Passed" : "Failed");

            return new string?[]
            {
                sem?.Name ?? "N/A",
                course?.Code ?? "N/A",
                course?.Title ?? "N/A",
                (course?.Credits ?? 0).ToString(),
                gradeText,
                status
            };
        });

        var csv = CsvWriter.BuildCsv(headers, rows);
        return CsvWriter.ToUtf8BytesWithBom(csv);
    }

    public async Task<byte[]> ExportMyTranscriptAsync(string userId)
    {
        var student = await _studentPortalService.GetStudentEntityByUserIdAsync(userId);
        if (student == null)
        {
            var headers = new[] { "Semester", "Course Code", "Course Title", "Credits", "Grade", "Status" };
            var emptyCsv = CsvWriter.BuildCsv(headers, Enumerable.Empty<string?[]>());
            return CsvWriter.ToUtf8BytesWithBom(emptyCsv);
        }

        return await ExportMyTranscriptAsync(student.Id);
    }

    public async Task<byte[]> ExportCourseGradesAsync(Guid courseId)
    {
        var grades = await _facultyService.GetCourseGradesAsync(courseId);

        var headers = new[]
        {
            "Student ID", "Student Name", "Grade", "Status"
        };

        var rows = grades.Select(g =>
        {
            var gradeText = g.Grade.HasValue ? g.Grade.Value.ToString("F1", CultureInfo.InvariantCulture) : string.Empty;
            var status = !g.Grade.HasValue ? "Not Graded" : (g.Grade.Value >= 5.0 ? "Passed" : "Failed");

            return new string?[]
            {
                g.StudentCode,
                g.StudentName,
                gradeText,
                status
            };
        });

        var csv = CsvWriter.BuildCsv(headers, rows);
        return CsvWriter.ToUtf8BytesWithBom(csv);
    }
}

