using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class StudentPortalService : IStudentPortalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnrollmentSubject? _enrollmentSubject;

    public StudentPortalService(IUnitOfWork unitOfWork, IEnrollmentSubject? enrollmentSubject = null)
    {
        _unitOfWork = unitOfWork;
        _enrollmentSubject = enrollmentSubject;
    }

    public async Task<Student?> GetStudentEntityByUserIdAsync(string userId)
    {
        var students = await _unitOfWork.Repository<Student>().FindAsync(s => s.UserId == userId);
        return students.FirstOrDefault();
    }

    public async Task<MyProfileDto?> GetMyProfileAsync(string userId)
    {
        var student = await GetStudentEntityByUserIdAsync(userId);
        if (student == null) return null;

        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(student.DepartmentId);
        var appUsers = await _unitOfWork.Repository<AppUser>().FindAsync(u => u.Id == userId);
        var appUser = appUsers.FirstOrDefault();

        string realName = appUser != null ? $"{appUser.FirstName} {appUser.LastName}".Trim() : $"Sinh viên {student.StudentId}";
        string realEmail = appUser?.Email ?? $"{student.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}";

        return new MyProfileDto
        {
            StudentId = student.StudentId,
            FullName = realName,
            Email = realEmail,
            DepartmentName = department?.Name ?? "N/A",
            DateOfBirth = student.DateOfBirth,
            Address = student.Address
        };
    }

    public async Task<IEnumerable<AvailableCourseDto>> GetAvailableCoursesAsync(string userId)
    {
        var student = await GetStudentEntityByUserIdAsync(userId);
        if (student == null) return new List<AvailableCourseDto>();

        var allCourses = await _unitOfWork.Repository<Course>().GetAllAsync();
        var departments = await _unitOfWork.Repository<Department>().GetAllAsync();
        
        // Find active semester
        var semesters = await _unitOfWork.Repository<Semester>().FindAsync(s => s.IsActive);
        var activeSemester = semesters.FirstOrDefault();
        
        var enrollments = new List<Enrollment>();
        if (activeSemester != null)
        {
            var enr = await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == student.Id && e.SemesterId == activeSemester.Id);
            enrollments = enr.ToList();
        }

        return allCourses.Select(c => new AvailableCourseDto
        {
            CourseId = c.Id,
            Code = c.Code,
            Title = c.Title,
            Credits = c.Credits,
            DepartmentName = departments.FirstOrDefault(d => d.Id == c.DepartmentId)?.Name ?? "N/A",
            IsEnrolled = enrollments.Any(e => e.CourseId == c.Id)
        });
    }

    public async Task<bool> EnrollInCourseAsync(string userId, Guid courseId)
    {
        var student = await GetStudentEntityByUserIdAsync(userId);
        if (student == null) return false;

        var semesters = await _unitOfWork.Repository<Semester>().FindAsync(s => s.IsActive);
        var activeSemester = semesters.FirstOrDefault();
        if (activeSemester == null) return false;

        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(courseId);
        if (course == null) return false;

        // 1. Kiểm tra đã đăng ký môn này trong kỳ chưa
        var existingEnrollment = await _unitOfWork.Repository<Enrollment>()
            .FindAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.SemesterId == activeSemester.Id);
            
        if (existingEnrollment.Any()) return false; // Already enrolled

        // 2. SRS: Kiểm tra môn tiên quyết (Prerequisite Course Validation)
        if (course.PrerequisiteCourseId.HasValue)
        {
            var passedPrereq = await _unitOfWork.Repository<Enrollment>().FindAsync(
                e => e.StudentId == student.Id 
                     && e.CourseId == course.PrerequisiteCourseId.Value 
                     && e.Grade.HasValue 
                     && e.Grade >= 5.0);

            if (!passedPrereq.Any())
            {
                return false; // Chưa hoàn thành môn tiên quyết với điểm >= 5.0
            }
        }

        // 3. SRS: Giới hạn tín chỉ tối đa mỗi học kỳ (Max Credits per semester from AppConstants)
        var currentEnrollments = (await _unitOfWork.Repository<Enrollment>().FindAsync(
            e => e.StudentId == student.Id && e.SemesterId == activeSemester.Id)).ToList();
        var allCourses = (await _unitOfWork.Repository<Course>().GetAllAsync()).ToDictionary(c => c.Id, c => c);
        int currentCredits = currentEnrollments.Sum(e => allCourses.TryGetValue(e.CourseId, out var c) ? c.Credits : 0);

        if (currentCredits + course.Credits > AppConstants.MaxCreditsPerSemester)
        {
            return false; // Vượt quá giới hạn tín chỉ cho phép trong học kỳ
        }

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = courseId,
            SemesterId = activeSemester.Id,
            EnrollmentDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Enrollment>().AddAsync(enrollment);
        var success = await _unitOfWork.CompleteAsync() > 0;

        // 4. Observer Pattern: Phát sự kiện khi đăng ký thành công để ghi AuditLog và gửi thông báo
        if (success && _enrollmentSubject != null)
        {
            await _enrollmentSubject.NotifyAsync(new EnrollmentCreatedEvent(
                enrollment.Id,
                student.Id,
                student.StudentId,
                course.Id,
                course.Code,
                activeSemester.Id,
                enrollment.EnrollmentDate
            ));
        }

        return success;
    }

    public async Task<bool> DropCourseAsync(string userId, Guid courseId)
    {
        var student = await GetStudentEntityByUserIdAsync(userId);
        if (student == null) return false;

        var semesters = await _unitOfWork.Repository<Semester>().FindAsync(s => s.IsActive);
        var activeSemester = semesters.FirstOrDefault();
        if (activeSemester == null) return false;

        var enrollments = await _unitOfWork.Repository<Enrollment>()
            .FindAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.SemesterId == activeSemester.Id);
            
        var enrollment = enrollments.FirstOrDefault();
        if (enrollment == null) return false;

        _unitOfWork.Repository<Enrollment>().Remove(enrollment);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<IEnumerable<MyGradeDto>> GetMyGradesAsync(string userId)
    {
        var student = await GetStudentEntityByUserIdAsync(userId);
        if (student == null) return new List<MyGradeDto>();

        var enrollments = await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == student.Id);
        var courses = await _unitOfWork.Repository<Course>().GetAllAsync();
        var semesters = await _unitOfWork.Repository<Semester>().GetAllAsync();

        return enrollments.Select(e => {
            var course = courses.FirstOrDefault(c => c.Id == e.CourseId);
            var sem = semesters.FirstOrDefault(s => s.Id == e.SemesterId);
            return new MyGradeDto
            {
                CourseCode = course?.Code ?? "N/A",
                CourseTitle = course?.Title ?? "N/A",
                Credits = course?.Credits ?? 0,
                SemesterName = sem?.Name ?? "N/A",
                Grade = e.Grade
            };
        });
    }
}
