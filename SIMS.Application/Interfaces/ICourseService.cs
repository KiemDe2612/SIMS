using SIMS.Application.DTOs;
using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

public interface ICourseService
{
    Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
    Task<Course?> GetCourseByIdAsync(Guid id);
    Task<bool> CreateCourseAsync(CreateCourseDto dto);
    Task<bool> UpdateCourseAsync(UpdateCourseDto dto);
    Task<bool> DeleteCourseAsync(Guid id);
    Task<IEnumerable<Department>> GetDepartmentsAsync();
    Task<IEnumerable<Faculty>> GetFacultiesAsync();
}
