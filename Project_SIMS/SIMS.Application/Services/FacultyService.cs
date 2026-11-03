using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class FacultyService : IFacultyService
{
    private readonly IUnitOfWork _unitOfWork;

    public FacultyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Course>> GetAssignedCoursesAsync(string userId)
    {
        var faculties = await _unitOfWork.Repository<Faculty>().FindAsync(f => f.UserId == userId);
        var faculty = faculties.FirstOrDefault();
        
        if (faculty == null) return new List<Course>();

        var courses = await _unitOfWork.Repository<Course>().FindAsync(c => c.AssignedFacultyId == faculty.Id);
        return courses;
    }

    public async Task<Course?> GetCourseByIdAsync(Guid courseId)
    {
        return await _unitOfWork.Repository<Course>().GetByIdAsync(courseId);
    }

    public async Task<IEnumerable<CourseGradeDto>> GetCourseGradesAsync(Guid courseId)
    {
        var enrollments = (await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.CourseId == courseId)).ToList();
        var studentIds = enrollments.Select(e => e.StudentId).ToHashSet();
        var allStudents = (await _unitOfWork.Repository<Student>().GetAllAsync()).Where(s => studentIds.Contains(s.Id)).ToDictionary(s => s.Id, s => s);
        var userIds = allStudents.Values.Select(s => s.UserId).ToHashSet();
        var allUsers = (await _unitOfWork.Repository<AppUser>().GetAllAsync()).Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u);

        var result = new List<CourseGradeDto>();
        foreach (var enrollment in enrollments)
        {
            if (allStudents.TryGetValue(enrollment.StudentId, out var student))
            {
                allUsers.TryGetValue(student.UserId, out var appUser);
                string realName = appUser != null ? $"{appUser.FirstName} {appUser.LastName}".Trim() : $"Sinh viên {student.StudentId}";

                result.Add(new CourseGradeDto
                {
                    EnrollmentId = enrollment.Id,
                    StudentId = student.Id,
                    StudentCode = student.StudentId,
                    StudentName = realName,
                    Grade = enrollment.Grade
                });
            }
        }

        return result;
    }

    public async Task UpdateGradesAsync(IEnumerable<CourseGradeUpdateDto> updates)
    {
        foreach (var update in updates)
        {
            var enrollment = await _unitOfWork.Repository<Enrollment>().GetByIdAsync(update.EnrollmentId);
            if (enrollment != null)
            {
                enrollment.Grade = update.Grade;
                _unitOfWork.Repository<Enrollment>().Update(enrollment);
            }
        }
        await _unitOfWork.CompleteAsync();
    }
}
