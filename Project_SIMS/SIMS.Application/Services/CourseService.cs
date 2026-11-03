using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
    {
        var courses = await _unitOfWork.Repository<Course>().GetAllAsync();
        var depts = await _unitOfWork.Repository<Department>().GetAllAsync();
        
        return courses.Select(c => new CourseDto
        {
            Id = c.Id,
            Code = c.Code,
            Title = c.Title,
            Credits = c.Credits,
            DepartmentName = depts.FirstOrDefault(d => d.Id == c.DepartmentId)?.Name ?? "N/A",
            AssignedFacultyName = c.AssignedFacultyId != null ? "Assigned" : "Unassigned"
        });
    }

    public async Task<Course?> GetCourseByIdAsync(Guid id)
    {
        return await _unitOfWork.Repository<Course>().GetByIdAsync(id);
    }

    public async Task<bool> CreateCourseAsync(CreateCourseDto dto)
    {
        var course = new Course
        {
            Code = dto.Code,
            Title = dto.Title,
            Description = dto.Description,
            Credits = dto.Credits,
            DepartmentId = dto.DepartmentId,
            AssignedFacultyId = dto.AssignedFacultyId,
            PrerequisiteCourseId = dto.PrerequisiteCourseId
        };

        await _unitOfWork.Repository<Course>().AddAsync(course);
        var result = await _unitOfWork.CompleteAsync();
        return result > 0;
    }

    public async Task<bool> UpdateCourseAsync(UpdateCourseDto dto)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(dto.Id);
        if (course == null) return false;

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.Credits = dto.Credits;
        course.DepartmentId = dto.DepartmentId;
        course.AssignedFacultyId = dto.AssignedFacultyId;
        course.PrerequisiteCourseId = dto.PrerequisiteCourseId;

        _unitOfWork.Repository<Course>().Update(course);
        var result = await _unitOfWork.CompleteAsync();
        return result > 0;
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        if (course == null) return false;

        // Kiểm tra nếu đã có enrollment thì không cho xóa
        var enrollments = await _unitOfWork.Repository<Enrollment>().FindAsync(e => e.CourseId == id);
        if (enrollments.Any())
        {
            throw new InvalidOperationException("Không thể xóa môn học này vì đã có sinh viên đăng ký học phần.");
        }

        // Chuyển sang Soft Delete
        course.IsDeleted = true;
        course.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Course>().Update(course);
        var result = await _unitOfWork.CompleteAsync();
        return result > 0;
    }

    public async Task<IEnumerable<Department>> GetDepartmentsAsync()
    {
        return await _unitOfWork.Repository<Department>().GetAllAsync();
    }

    public async Task<IEnumerable<Faculty>> GetFacultiesAsync()
    {
        return await _unitOfWork.Repository<Faculty>().GetAllAsync();
    }
}
