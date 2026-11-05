using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class StudentPortalService : IStudentPortalService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentPortalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        return new MyProfileDto
        {
            StudentId = student.StudentId,
            FullName = "Name is mapped in Controller",
            Email = "Email is mapped in Controller",
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
        
        var enrolledCourseIds = new HashSet<Guid>();
        if (activeSemester != null)
        {
            var enr = await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.StudentId == student.Id && e.SemesterId == activeSemester.Id);
            enrolledCourseIds = enr.Select(e => e.CourseId).ToHashSet();
        }

        var departmentNames = departments.ToDictionary(d => d.Id, d => d.Name);

        return allCourses.Select(c => new AvailableCourseDto
        {
            CourseId = c.Id,
            Code = c.Code,
            Title = c.Title,
            Credits = c.Credits,
            DepartmentName = departmentNames.GetValueOrDefault(c.DepartmentId) ?? "N/A",
            IsEnrolled = enrolledCourseIds.Contains(c.Id)
        });
    }

    public async Task<bool> EnrollInCourseAsync(string userId, Guid courseId)
    {
        var student = await GetStudentEntityByUserIdAsync(userId);
        if (student == null) return false;

        var semesters = await _unitOfWork.Repository<Semester>().FindAsync(s => s.IsActive);
        var activeSemester = semesters.FirstOrDefault();
        if (activeSemester == null) return false;

        var existingEnrollment = await _unitOfWork.Repository<Enrollment>()
            .FindAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.SemesterId == activeSemester.Id);
            
        if (existingEnrollment.Any()) return false; // Already enrolled

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = courseId,
            SemesterId = activeSemester.Id,
            EnrollmentDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Enrollment>().AddAsync(enrollment);
        return await _unitOfWork.CompleteAsync() > 0;
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
