using SIMS.Application.DTOs;
using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

public interface IStudentPortalService
{
    Task<Student?> GetStudentEntityByUserIdAsync(string userId);
    Task<MyProfileDto?> GetMyProfileAsync(string userId);
    Task<IEnumerable<AvailableCourseDto>> GetAvailableCoursesAsync(string userId);
    Task<bool> EnrollInCourseAsync(string userId, Guid courseId);
    Task<bool> DropCourseAsync(string userId, Guid courseId);
    Task<IEnumerable<MyGradeDto>> GetMyGradesAsync(string userId);
}
