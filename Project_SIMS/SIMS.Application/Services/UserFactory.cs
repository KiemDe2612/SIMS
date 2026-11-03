using Microsoft.AspNetCore.Identity;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;
using SIMS.Domain.Entities;

namespace SIMS.Application.Services;

public class UserFactory : IUserFactory
{
    private readonly UserManager<AppUser> _userManager;

    public UserFactory(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public AppUser CreateUser(CreateUserRequest request)
    {
        string email = !string.IsNullOrWhiteSpace(request.Email)
            ? request.Email.Trim().ToLower()
            : $"{request.Identifier.Trim().ToLower()}{AppConstants.DefaultEmailDomain}";

        return new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<(IdentityResult Result, AppUser? User)> CreateUserWithRoleAsync(CreateUserRequest request)
    {
        var user = CreateUser(request);
        string password = !string.IsNullOrWhiteSpace(request.Password)
            ? request.Password
            : $"{request.Identifier}{AppConstants.DefaultPasswordSuffix}";

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return (createResult, null);
        }

        string role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role : AppRoles.Student;
        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return (roleResult, user);
        }

        return (IdentityResult.Success, user);
    }
}

