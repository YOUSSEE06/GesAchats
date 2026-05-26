using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using Moq;

namespace GesAchats.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IUserSession> _userSessionMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _userSessionMock = new Mock<IUserSession>();
        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _authService = new AuthService(_uowMock.Object, _userSessionMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        var username = "admin";
        var password = "password";
        var user = new User { Login = username, PasswordHash = password, IsActive = true };
        _userRepoMock.Setup(r => r.GetByLoginAsync(username)).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Login);
        _uowMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var username = "admin";
        var user = new User { Login = username, PasswordHash = "correct", IsActive = true };
        _userRepoMock.Setup(r => r.GetByLoginAsync(username)).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(username, "wrong");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePassword_ValidOldPassword_UpdatesPassword()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, PasswordHash = "old" };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _authService.ChangePasswordAsync(userId, "old", "new");

        // Assert
        Assert.True(result);
        Assert.Equal("new", user.PasswordHash);
        _uowMock.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
