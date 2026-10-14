using AutoMapper;
using Moq;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Features.Auth;
using ShopApi.Application.Interfaces;
using ShopApi.Application.Mappings;
using ShopApi.Domain.Entities;
using ShopApi.Domain.Enums;

namespace ShopApi.Application.Tests.Features.Auth;

// Sample handler tests demonstrating the project conventions:
// Arrange (Moq at seams, REAL AutoMapper profile), Act (single Handle call),
// Assert (Result shape — expected failures are values, never exceptions).
public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StubPasswordHasher _hasher = new();
    private readonly StubJwtService _jwt = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    public RegisterCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
    }

    [Fact]
    public async Task Handle_valid_request_returns_success_with_token_and_normalized_email()
    {
        // Arrange
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        User? captured = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => captured = user)
            .Returns(Task.CompletedTask);
        var handler = new RegisterCommandHandler(_unitOfWork.Object, _hasher, _jwt, _mapper);

        // Act
        var result = await handler.Handle(
            new RegisterCommand("Vera@Shop.Test", "P@ssw0rd123", "Vera", "Fy"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Value!.Token);
        Assert.Equal("vera@shop.test", result.Value.User.Email);
        Assert.Equal("Customer", result.Value.User.Role);
        Assert.NotNull(captured);
        Assert.Equal(UserRole.Customer, captured!.Role);
        Assert.StartsWith("HASHED:", captured.PasswordHash);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_duplicate_email_returns_conflict_without_creating_user()
    {
        // Arrange
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new RegisterCommandHandler(_unitOfWork.Object, _hasher, _jwt, _mapper);

        // Act
        var result = await handler.Handle(
            new RegisterCommand("vera@shop.test", "P@ssw0rd123", "Vera", "Fy"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error!.Code);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "HASHED:" + password;

        public bool Verify(string password, string passwordHash) =>
            passwordHash == "HASHED:" + password;
    }

    private sealed class StubJwtService : IJwtService
    {
        public string GenerateToken(User user) => "test-token";

        public System.Security.Claims.ClaimsPrincipal? ValidateToken(string token) => null;
    }
}
