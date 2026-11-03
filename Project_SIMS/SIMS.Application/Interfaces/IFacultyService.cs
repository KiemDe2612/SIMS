using SIMS.Application.DTOs;
using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

public interface IFacultyService
{
    Task<IEnumerable<Course>> GetAssignedCoursesAsync(string userId);
    Task<Course?> GetCourseByIdAsync(Guid courseId);
    Task<IEnumerable<CourseGradeDto>> GetCourseGradesAsync(Guid courseId);
    Task UpdateGradesAsync(IEnumerable<CourseGradeUpdateDto> updates);
}
