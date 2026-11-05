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
        if (enrollments.Count == 0) return new List<CourseGradeDto>();

        var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();
        var students = await _unitOfWork.Repository<Student>().FindAsync(s => studentIds.Contains(s.Id));
        var studentsById = students.ToDictionary(s => s.Id);

        var result = new List<CourseGradeDto>();
        foreach (var enrollment in enrollments)
        {
            if (studentsById.TryGetValue(enrollment.StudentId, out var student))
            {
                result.Add(new CourseGradeDto
                {
                    EnrollmentId = enrollment.Id,
                    StudentId = student.Id,
                    StudentCode = student.StudentId,
                    StudentName = "Student " + student.StudentId, // Placeholder because we don't eager load AppUser in Generic Repo yet
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
