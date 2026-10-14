using ShopApi.Domain.Common;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Domain.Entities;

public sealed class Address : BaseEntity
{
    private const int MaxProvinceLength = 100;
    private const int MaxCityLength = 100;
    private const int MinPostalCodeLength = 3;
    private const int MaxPostalCodeLength = 20;
    private const int MinFullAddressLength = 5;
    private const int MaxFullAddressLength = 500;

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public string Province { get; private set; }

    public string City { get; private set; }

    public string PostalCode { get; private set; }

    public string FullAddress { get; private set; }

    public Address(Guid userId, string province, string city, string postalCode, string fullAddress)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Address user id is required.");
        }

        UserId = userId;
        Province = EnsureProvince(province);
        City = EnsureCity(city);
        PostalCode = EnsurePostalCode(postalCode);
        FullAddress = EnsureFullAddress(fullAddress);
    }

    public void Update(string province, string city, string postalCode, string fullAddress)
    {
        Province = EnsureProvince(province);
        City = EnsureCity(city);
        PostalCode = EnsurePostalCode(postalCode);
        FullAddress = EnsureFullAddress(fullAddress);
        Touch();
    }

    private static string EnsureProvince(string province) =>
        EnsureText(province, nameof(Province), 1, MaxProvinceLength);

    private static string EnsureCity(string city) =>
        EnsureText(city, nameof(City), 1, MaxCityLength);

    private static string EnsurePostalCode(string postalCode) =>
        EnsureText(postalCode, nameof(PostalCode), MinPostalCodeLength, MaxPostalCodeLength);

    private static string EnsureFullAddress(string fullAddress) =>
        EnsureText(fullAddress, nameof(FullAddress), MinFullAddressLength, MaxFullAddressLength);

    private static string EnsureText(string value, string propertyName, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"Address {propertyName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            throw new DomainException(
                $"Address {propertyName} must be between {minLength} and {maxLength} characters.");
        }

        return trimmed;
    }
}
