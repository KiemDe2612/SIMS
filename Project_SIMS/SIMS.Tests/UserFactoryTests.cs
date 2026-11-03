using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using SIMS.Application.Interfaces;
using SIMS.Application.Services;
using SIMS.Domain.Constants;
using SIMS.Domain.Entities;

namespace SIMS.Tests;

public class UserFactoryTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly UserFactory _factory;

    public UserFactoryTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _mockUserManager = new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _factory = new UserFactory(_mockUserManager.Object);
    }

    [Fact]
    public void CreateUser_ShouldConstructAppUser_WithDefaultDomainWhenEmailNotProvided()
    {
        // Arrange
        var req = new CreateUserRequest
        {
            Identifier = "SE12345",
            FirstName = "Van A",
            LastName = "Nguyen",
            Role = AppRoles.Student
        };

        // Act
        var user = _factory.CreateUser(req);

        // Assert
        user.Should().NotBeNull();
        user.UserName.Should().Be("se12345@sims.com");
        user.Email.Should().Be("se12345@sims.com");
        user.FirstName.Should().Be("Van A");
        user.LastName.Should().Be("Nguyen");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserWithRoleAsync_ShouldCreateUserAndAssignRole_Successfully()
    {
        // Arrange
        var req = new CreateUserRequest
        {
            Identifier = "SE12345",
            FirstName = "Van A",
            LastName = "Nguyen",
            Role = AppRoles.Student
        };

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), AppRoles.Student))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (result, user) = await _factory.CreateUserWithRoleAsync(req);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.Should().NotBeNull();
        _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<AppUser>(), "SE12345@123"), Times.Once);
        _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<AppUser>(), AppRoles.Student), Times.Once);
    }
}

