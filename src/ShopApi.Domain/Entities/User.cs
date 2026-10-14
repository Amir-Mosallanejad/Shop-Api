using ShopApi.Domain.Common;
using ShopApi.Domain.Enums;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Domain.Entities;

public sealed class User : BaseEntity
{
    private const int MaxEmailLength = 256;
    private const int MinPasswordHashLength = 10;
    private const int MaxNameLength = 100;

    private readonly List<Address> _addresses = new();
    private readonly List<Order> _orders = new();

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public UserRole Role { get; private set; }

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";

    public User(string email, string passwordHash, string firstName, string lastName, UserRole role = UserRole.Customer)
    {
        Email = EnsureEmail(email);
        PasswordHash = EnsurePasswordHash(passwordHash);
        FirstName = EnsureName(firstName, nameof(firstName));
        LastName = EnsureName(lastName, nameof(lastName));
        Role = role;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = EnsureName(firstName, nameof(firstName));
        LastName = EnsureName(lastName, nameof(lastName));
        Touch();
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        PasswordHash = EnsurePasswordHash(newPasswordHash);
        Touch();
    }

    public void PromoteToAdmin()
    {
        if (Role == UserRole.Admin)
        {
            return;
        }

        Role = UserRole.Admin;
        Touch();
    }

    private static string EnsureEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("User email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > MaxEmailLength)
        {
            throw new DomainException($"User email must be at most {MaxEmailLength} characters.");
        }

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0 || !normalized[(atIndex + 1)..].Contains('.'))
        {
            throw new DomainException($"User email format is invalid. Email: {email}.");
        }

        return normalized;
    }

    private static string EnsurePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("User password hash is required.");
        }

        if (passwordHash.Length < MinPasswordHashLength)
        {
            throw new DomainException("User password hash is invalid.");
        }

        return passwordHash;
    }

    private static string EnsureName(string name, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException($"User {propertyName} is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainException($"User {propertyName} must be at most {MaxNameLength} characters.");
        }

        return trimmed;
    }
}
