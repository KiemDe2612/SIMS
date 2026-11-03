using System.Text.Json;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;
using SIMS.Infrastructure.Exporters;

namespace SIMS.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private async Task LogAuditAsync(string userId, string entityName, object filterParams)
    {
        try
        {
            var audit = new AuditLog
            {
                UserId = string.IsNullOrWhiteSpace(userId) ? "Anonymous" : userId,
                Action = "Export",
                EntityName = entityName,
                EntityId = string.Empty,
                Changes = JsonSerializer.Serialize(filterParams),
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(audit);
            await _unitOfWork.CompleteAsync();
        }
        catch
        {
            // Do not fail export if audit logging encounters a secondary non-critical error
        }
    }

    private static (string ContentType, string Extension) GetMeta(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Excel => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
            ExportFormat.Pdf => ("application/pdf", ".pdf"),
            _ => ("text/csv; charset=utf-8", ".csv")
        };
    }

    // ==========================================
    // 1. ADMINISTRATOR EXPORTS
    // ==========================================

    public async Task<ExportResult> ExportStudentsAsync(string userId, ExportFormat format)
    {
        await LogAuditAsync(userId, "Student", new { action = "ExportStudents", format = format.ToString() });

        var headers = new[] { "Mã sinh viên", "Họ tên", "Email", "Ngày sinh", "Địa chỉ", "Khoa / Ngành", "Ngày nhập học" };
        var meta = GetMeta(format);

        // Pre-cache lookups for performance
        var departments = (await _unitOfWork.Repository<Department>().GetAllAsync()).ToDictionary(d => d.Id, d => d.Name);
        var users = (await _unitOfWork.Repository<AppUser>().GetAllAsync()).ToDictionary(u => u.Id, u => u);

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            int page = 1;
            int pageSize = 200;
            while (true)
            {
                var (items, total) = await _unitOfWork.Repository<Student>().GetPagedAsync(page, pageSize);
                var list = items.ToList();
                if (!list.Any()) break;

                foreach (var s in list)
                {
                    users.TryGetValue(s.UserId, out var u);
                    departments.TryGetValue(s.DepartmentId, out var deptName);

                    yield return new object?[]
                    {
                        s.StudentId,
                        u != null ? $"{u.FirstName} {u.LastName}".Trim() : "N/A",
                        u?.Email ?? "N/A",
                        s.DateOfBirth.ToString("dd/MM/yyyy"),
                        s.Address,
                        deptName ?? "N/A",
                        s.EnrollmentDate.ToString("dd/MM/yyyy")
                    };
                }

                if (page * pageSize >= total) break;
                page++;
            }
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("SinhVien", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"DanhSach_SinhVien_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }

    public async Task<ExportResult> ExportCoursesAsync(string userId, ExportFormat format)
    {
        await LogAuditAsync(userId, "Course", new { action = "ExportCourses", format = format.ToString() });

        var headers = new[] { "Mã môn học", "Tên môn học", "Mô tả", "Số tín chỉ", "Khoa phụ trách", "Giảng viên phụ trách", "Môn tiên quyết" };
        var meta = GetMeta(format);

        var departments = (await _unitOfWork.Repository<Department>().GetAllAsync()).ToDictionary(d => d.Id, d => d.Name);
        var faculties = (await _unitOfWork.Repository<Faculty>().GetAllAsync()).ToDictionary(f => f.Id, f => f.FacultyId);
        var allCourses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id, c => c.Code);

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            int page = 1;
            int pageSize = 200;
            while (true)
            {
                var (items, total) = await _unitOfWork.Repository<Course>().GetPagedAsync(page, pageSize);
                var list = items.ToList();
                if (!list.Any()) break;

                foreach (var c in list)
                {
                    departments.TryGetValue(c.DepartmentId, out var deptName);
                    string facultyName = c.AssignedFacultyId.HasValue && faculties.TryGetValue(c.AssignedFacultyId.Value, out var facCode)
                        ? facCode
                        : "Chưa phân công";

                    string prereq = c.PrerequisiteCourseId.HasValue && allCourses.TryGetValue(c.PrerequisiteCourseId.Value, out var pCode)
                        ? pCode
                        : "Không có";

                    yield return new object?[]
                    {
                        c.Code,
                        c.Title,
                        c.Description,
                        c.Credits,
                        deptName ?? "N/A",
                        facultyName,
                        prereq
                    };
                }

                if (page * pageSize >= total) break;
                page++;
            }
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("MonHoc", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"DanhSach_MonHoc_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }

    public async Task<ExportResult> ExportEnrollmentsBySemesterAsync(string userId, Guid? semesterId, ExportFormat format)
    {
        await LogAuditAsync(userId, "Enrollment", new { action = "ExportEnrollmentsBySemester", semesterId, format = format.ToString() });

        var headers = new[] { "Mã SV", "Mã môn học", "Tên môn học", "Số tín chỉ", "Học kỳ", "Ngày đăng ký", "Điểm số" };
        var meta = GetMeta(format);

        var students = (await _unitOfWork.Repository<Student>().GetAllAsync()).ToDictionary(s => s.Id, s => s.StudentId);
        var courses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id, c => c);
        var semesters = (await _unitOfWork.Repository<Semester>().GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);

        string semesterName = "TatCa";
        if (semesterId.HasValue && semesters.TryGetValue(semesterId.Value, out var semName))
        {
            semesterName = semName.Replace(" ", "_");
        }

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            int page = 1;
            int pageSize = 200;
            while (true)
            {
                var (items, total) = semesterId.HasValue
                    ? await _unitOfWork.Repository<Enrollment>().GetPagedAsync(page, pageSize, e => e.SemesterId == semesterId.Value)
                    : await _unitOfWork.Repository<Enrollment>().GetPagedAsync(page, pageSize);

                var list = items.ToList();
                if (!list.Any()) break;

                foreach (var e in list)
                {
                    students.TryGetValue(e.StudentId, out var sCode);
                    courses.TryGetValue(e.CourseId, out var course);
                    semesters.TryGetValue(e.SemesterId, out var sName);

                    yield return new object?[]
                    {
                        sCode ?? "N/A",
                        course?.Code ?? "N/A",
                        course?.Title ?? "N/A",
                        course?.Credits ?? 0,
                        sName ?? "N/A",
                        e.EnrollmentDate.ToString("dd/MM/yyyy"),
                        e.Grade.HasValue ? e.Grade.Value.ToString("0.0") : "Chưa có điểm"
                    };
                }

                if (page * pageSize >= total) break;
                page++;
            }
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("DangKyHocPhan", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"DanhSach_DangKy_{semesterName}_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }

    public async Task<ExportResult> ExportCourseGradesAsync(string userId, Guid courseId, ExportFormat format)
    {
        await LogAuditAsync(userId, "CourseGrade", new { action = "ExportCourseGrades", courseId, format = format.ToString() });

        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(courseId);
        if (course == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy môn học với ID {courseId}");
        }

        var headers = new[] { "Mã môn", "Tên môn học", "Học kỳ", "Mã SV", "Họ tên sinh viên", "Điểm số", "Đánh giá" };
        var meta = GetMeta(format);

        var students = (await _unitOfWork.Repository<Student>().GetAllAsync()).ToDictionary(s => s.Id, s => s);
        var users = (await _unitOfWork.Repository<AppUser>().GetAllAsync()).ToDictionary(u => u.Id, u => u);
        var semesters = (await _unitOfWork.Repository<Semester>().GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            int page = 1;
            int pageSize = 200;
            while (true)
            {
                var (items, total) = await _unitOfWork.Repository<Enrollment>().GetPagedAsync(page, pageSize, e => e.CourseId == courseId);
                var list = items.ToList();
                if (!list.Any()) break;

                foreach (var e in list)
                {
                    students.TryGetValue(e.StudentId, out var student);
                    AppUser? user = null;
                    if (student != null)
                    {
                        users.TryGetValue(student.UserId, out user);
                    }

                    semesters.TryGetValue(e.SemesterId, out var sName);

                    string eval = !e.Grade.HasValue ? "Chưa có điểm" : (e.Grade.Value >= 5.0 ? "Đạt (Pass)" : "Không đạt");

                    yield return new object?[]
                    {
                        course.Code,
                        course.Title,
                        sName ?? "N/A",
                        student?.StudentId ?? "N/A",
                        user != null ? $"{user.FirstName} {user.LastName}".Trim() : (student != null ? $"SV {student.StudentId}" : "N/A"),
                        e.Grade.HasValue ? e.Grade.Value.ToString("0.0") : "Chưa có",
                        eval
                    };
                }

                if (page * pageSize >= total) break;
                page++;
            }
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("BangDiem", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"BangDiem_{course.Code}_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }

    // ==========================================
    // 2. FACULTY EXPORTS (PERMISSION ENFORCED)
    // ==========================================

    public async Task<ExportResult> ExportFacultyCourseGradesAsync(string facultyUserId, Guid courseId, ExportFormat format)
    {
        // Permission Check: Verify Faculty identity
        var faculties = await _unitOfWork.Repository<Faculty>().FindAsync(f => f.UserId == facultyUserId);
        var faculty = faculties.FirstOrDefault();
        if (faculty == null)
        {
            throw new UnauthorizedAccessException("Tài khoản không phải là Giảng viên hoặc không có quyền truy cập.");
        }

        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(courseId);
        if (course == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy môn học với ID {courseId}");
        }

        // Strict Check: Course AssignedFacultyId must match current faculty.Id
        if (course.AssignedFacultyId != faculty.Id)
        {
            throw new UnauthorizedAccessException($"Bạn không có quyền xuất điểm cho môn {course.Code} vì không được phân công giảng dạy môn này.");
        }

        await LogAuditAsync(facultyUserId, "FacultyCourseGrade", new { facultyUserId, courseId, courseCode = course.Code, format = format.ToString() });

        var headers = new[] { "Mã môn", "Tên môn học", "Học kỳ", "Mã SV", "Họ tên sinh viên", "Điểm số", "Đánh giá" };
        var meta = GetMeta(format);

        var students = (await _unitOfWork.Repository<Student>().GetAllAsync()).ToDictionary(s => s.Id, s => s);
        var users = (await _unitOfWork.Repository<AppUser>().GetAllAsync()).ToDictionary(u => u.Id, u => u);
        var semesters = (await _unitOfWork.Repository<Semester>().GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            int page = 1;
            int pageSize = 200;
            while (true)
            {
                var (items, total) = await _unitOfWork.Repository<Enrollment>().GetPagedAsync(page, pageSize, e => e.CourseId == courseId);
                var list = items.ToList();
                if (!list.Any()) break;

                foreach (var e in list)
                {
                    students.TryGetValue(e.StudentId, out var student);
                    AppUser? user = null;
                    if (student != null)
                    {
                        users.TryGetValue(student.UserId, out user);
                    }
                    semesters.TryGetValue(e.SemesterId, out var sName);

                    string eval = !e.Grade.HasValue ? "Chưa có điểm" : (e.Grade.Value >= 5.0 ? "Đạt (Pass)" : "Không đạt");

                    yield return new object?[]
                    {
                        course.Code,
                        course.Title,
                        sName ?? "N/A",
                        student?.StudentId ?? "N/A",
                        user != null ? $"{user.FirstName} {user.LastName}".Trim() : (student != null ? $"SV {student.StudentId}" : "N/A"),
                        e.Grade.HasValue ? e.Grade.Value.ToString("0.0") : "Chưa có",
                        eval
                    };
                }

                if (page * pageSize >= total) break;
                page++;
            }
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("BangDiemLopHoc", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"BangDiem_GiangVien_{course.Code}_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }

    // ==========================================
    // 3. STUDENT EXPORTS (DATA OWNERSHIP ENFORCED)
    // ==========================================

    public async Task<ExportResult> ExportStudentGradesAsync(string studentUserId, ExportFormat format)
    {
        // Permission Check: Verify student identity
        var students = await _unitOfWork.Repository<Student>().FindAsync(s => s.UserId == studentUserId);
        var student = students.FirstOrDefault();
        if (student == null)
        {
            throw new UnauthorizedAccessException("Tài khoản không phải là Sinh viên hoặc không tìm thấy hồ sơ sinh viên tương ứng.");
        }

        await LogAuditAsync(studentUserId, "StudentGrade", new { studentUserId, studentId = student.StudentId, format = format.ToString() });

        var enrollments = (await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == student.Id)).ToList();
        var courses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id, c => c);
        var semesters = (await _unitOfWork.Repository<Semester>().GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);
        var dept = await _unitOfWork.Repository<Department>().GetByIdAsync(student.DepartmentId);
        var user = (await _unitOfWork.Repository<AppUser>().FindAsync(u => u.Id == studentUserId)).FirstOrDefault();

        string studentName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : $"Sinh viên {student.StudentId}";
        string deptName = dept?.Name ?? "Chung";

        // If PDF requested
        if (format == ExportFormat.Pdf)
        {
            var pdfItems = enrollments.Select(e =>
            {
                courses.TryGetValue(e.CourseId, out var c);
                semesters.TryGetValue(e.SemesterId, out var sName);
                return new StudentTranscriptItem(
                    c?.Code ?? "N/A",
                    c?.Title ?? "N/A",
                    c?.Credits ?? 0,
                    sName ?? "N/A",
                    e.Grade
                );
            }).ToList();

            byte[] pdfBytes = PdfExporter.GenerateStudentTranscript(student.StudentId, studentName, deptName, pdfItems);

            return new ExportResult
            {
                FileContents = pdfBytes,
                ContentType = "application/pdf",
                FileName = $"BangDiem_{student.StudentId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };
        }

        // CSV or Excel
        var headers = new[] { "Mã môn", "Tên môn học", "Số tín chỉ", "Học kỳ", "Điểm số", "Kết quả" };
        var meta = GetMeta(format);

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            foreach (var e in enrollments)
            {
                courses.TryGetValue(e.CourseId, out var c);
                semesters.TryGetValue(e.SemesterId, out var sName);

                string eval = !e.Grade.HasValue ? "Đang học" : (e.Grade.Value >= 5.0 ? "Đạt (Pass)" : "Không đạt");

                yield return new object?[]
                {
                    c?.Code ?? "N/A",
                    c?.Title ?? "N/A",
                    c?.Credits ?? 0,
                    sName ?? "N/A",
                    e.Grade.HasValue ? e.Grade.Value.ToString("0.0") : "Chưa có",
                    eval
                };
            }
            await Task.CompletedTask;
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("BangDiemCaNhan", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"BangDiem_{student.StudentId}_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }

    public async Task<ExportResult> ExportStudentEnrollmentsAsync(string studentUserId, ExportFormat format)
    {
        var students = await _unitOfWork.Repository<Student>().FindAsync(s => s.UserId == studentUserId);
        var student = students.FirstOrDefault();
        if (student == null)
        {
            throw new UnauthorizedAccessException("Tài khoản không phải là Sinh viên hoặc không tìm thấy hồ sơ sinh viên tương ứng.");
        }

        await LogAuditAsync(studentUserId, "StudentEnrollment", new { studentUserId, studentId = student.StudentId, format = format.ToString() });

        var enrollments = (await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == student.Id)).ToList();
        var courses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id, c => c);
        var semesters = (await _unitOfWork.Repository<Semester>().GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);

        var headers = new[] { "Mã môn học", "Tên môn học", "Số tín chỉ", "Học kỳ", "Ngày đăng ký" };
        var meta = GetMeta(format);

        async IAsyncEnumerable<IEnumerable<object?>> GetRowsAsync()
        {
            foreach (var e in enrollments)
            {
                courses.TryGetValue(e.CourseId, out var c);
                semesters.TryGetValue(e.SemesterId, out var sName);

                yield return new object?[]
                {
                    c?.Code ?? "N/A",
                    c?.Title ?? "N/A",
                    c?.Credits ?? 0,
                    sName ?? "N/A",
                    e.EnrollmentDate.ToString("dd/MM/yyyy")
                };
            }
            await Task.CompletedTask;
        }

        byte[] bytes = format == ExportFormat.Excel
            ? await ExcelExporter.ExportAsync("MonHocDaDangKy", headers, GetRowsAsync())
            : await CsvExporter.ExportAsync(headers, GetRowsAsync());

        return new ExportResult
        {
            FileContents = bytes,
            ContentType = meta.ContentType,
            FileName = $"MonDaDangKy_{student.StudentId}_{DateTime.Now:yyyyMMdd_HHmmss}{meta.Extension}"
        };
    }
}

