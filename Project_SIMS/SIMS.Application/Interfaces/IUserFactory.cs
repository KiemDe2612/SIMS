using Microsoft.AspNetCore.Identity;
using SIMS.Domain.Entities;

namespace SIMS.Application.Interfaces;

public class CreateUserRequest
{
    public string Identifier { get; set; } = string.Empty; // Mã SV hoặc FacultyId
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty;
}

public interface IUserFactory
{
    AppUser CreateUser(CreateUserRequest request);
    Task<(IdentityResult Result, AppUser? User)> CreateUserWithRoleAsync(CreateUserRequest request);
}

