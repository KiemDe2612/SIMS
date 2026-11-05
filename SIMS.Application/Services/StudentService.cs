using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Application.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
    {
        var students = await _unitOfWork.Repository<Student>().GetAllAsync();
        var depts = await _unitOfWork.Repository<Department>().GetAllAsync();
        
        return students.Select(s => new StudentDto
        {
            Id = s.Id,
            StudentId = s.StudentId,
            DateOfBirth = s.DateOfBirth,
            Address = s.Address,
            DepartmentId = s.DepartmentId,
            DepartmentName = depts.FirstOrDefault(d => d.Id == s.DepartmentId)?.Name ?? "N/A"
        });
    }

    public async Task<StudentDto?> GetStudentByIdAsync(Guid id)
    {
        var student = await _unitOfWork.Repository<Student>().GetByIdAsync(id);
        if (student == null) return null;

        return new StudentDto
        {
            Id = student.Id,
            StudentId = student.StudentId,
            DateOfBirth = student.DateOfBirth,
            Address = student.Address,
            DepartmentId = student.DepartmentId
        };
    }

    public async Task<bool> CreateStudentAsync(CreateStudentDto dto)
    {
        var student = new Student
        {
            UserId = dto.UserId,
            StudentId = dto.StudentId,
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address,
            DepartmentId = dto.DepartmentId,
            EnrollmentDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Student>().AddAsync(student);
        var result = await _unitOfWork.CompleteAsync();
        return result > 0;
    }

    public async Task<bool> UpdateStudentAsync(UpdateStudentDto dto)
    {
        var student = await _unitOfWork.Repository<Student>().GetByIdAsync(dto.Id);
        if (student == null) return false;

        student.DateOfBirth = dto.DateOfBirth;
        student.Address = dto.Address;
        student.DepartmentId = dto.DepartmentId;

        _unitOfWork.Repository<Student>().Update(student);
        var result = await _unitOfWork.CompleteAsync();
        return result > 0;
    }

    public async Task<bool> DeleteStudentAsync(Guid id)
    {
        var student = await _unitOfWork.Repository<Student>().GetByIdAsync(id);
        if (student == null) return false;

        _unitOfWork.Repository<Student>().Remove(student);
        var result = await _unitOfWork.CompleteAsync();
        return result > 0;
    }

    public async Task<IEnumerable<Department>> GetDepartmentsAsync()
    {
        return await _unitOfWork.Repository<Department>().GetAllAsync();
    }
}
