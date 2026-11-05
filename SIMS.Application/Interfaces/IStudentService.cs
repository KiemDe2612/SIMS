using SIMS.Application.DTOs;
using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
    Task<StudentDto?> GetStudentByIdAsync(Guid id);
    Task<bool> CreateStudentAsync(CreateStudentDto dto);
    Task<bool> UpdateStudentAsync(UpdateStudentDto dto);
    Task<bool> DeleteStudentAsync(Guid id);
    Task<IEnumerable<Department>> GetDepartmentsAsync();
}
