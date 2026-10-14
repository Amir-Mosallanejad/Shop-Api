using ShopApi.Domain.Common;
using ShopApi.Domain.Exceptions;
using ShopApi.Domain.ValueObjects;

namespace ShopApi.Domain.Entities;

public sealed class Product : BaseEntity
{
    private const int MinNameLength = 2;
    private const int MaxNameLength = 200;
    private const int MaxSlugLength = 220;
    private const int MaxDescriptionLength = 2000;

    private readonly List<ProductImage> _images = new();

    public string Name { get; private set; }

    // TODO(phase-04): enforce slug uniqueness with a database unique index.
    public string Slug { get; private set; }

    public string Description { get; private set; }

    public Money Price { get; private set; }

    public int Stock { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    // EF Core materialization only. Domain code must use the public constructor.
    private Product()
    {
        Name = null!;
        Slug = null!;
        Description = null!;
        Price = null!;
    }

    public Product(string name, string description, Money price, int stock, Guid categoryId, string? slug = null)
    {
        Name = EnsureName(name);
        Slug = EnsureSlug(slug ?? GenerateSlug(name));
        Description = EnsureDescription(description);
        Price = price ?? throw new DomainException("Product price is required.");
        Stock = EnsureStock(stock);
        CategoryId = EnsureCategoryId(categoryId);
    }

    public void UpdateDetails(string name, string description)
    {
        Name = EnsureName(name);
        Slug = EnsureSlug(GenerateSlug(name));
        Description = EnsureDescription(description);
        Touch();
    }

    public void ChangePrice(Money newPrice)
    {
        Price = newPrice ?? throw new DomainException("Product price is required.");
        Touch();
    }

    public void AdjustStock(int delta)
    {
        var result = Stock + delta;
        if (result < 0)
        {
            throw new DomainException($"Product stock cannot go below zero. Stock: {Stock}, Delta: {delta}.");
        }

        Stock = result;
        Touch();
    }

    public void SetCategory(Guid categoryId)
    {
        CategoryId = EnsureCategoryId(categoryId);
        Touch();
    }

    public ProductImage AddImage(string url, bool isMain)
    {
        if (isMain)
        {
            foreach (var image in _images)
            {
                image.UnmarkAsMain();
            }
        }

        var productImage = new ProductImage(Id, url, isMain);
        _images.Add(productImage);
        Touch();
        return productImage;
    }

    public void SetMainImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            throw new DomainException($"Product image not found. ImageId: {imageId}.");
        }

        foreach (var sibling in _images)
        {
            sibling.UnmarkAsMain();
        }

        image.MarkAsMain();
        Touch();
    }

    private static string EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length < MinNameLength || trimmed.Length > MaxNameLength)
        {
            throw new DomainException(
                $"Product name must be between {MinNameLength} and {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static string EnsureSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < MinNameLength || slug.Length > MaxSlugLength)
        {
            throw new DomainException(
                $"Product slug must be between {MinNameLength} and {MaxSlugLength} characters.");
        }

        return slug;
    }

    private static string EnsureDescription(string description)
    {
        var text = description ?? string.Empty;
        if (text.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Product description must be at most {MaxDescriptionLength} characters.");
        }

        return text;
    }

    private static int EnsureStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException($"Product stock cannot be negative. Stock: {stock}.");
        }

        return stock;
    }

    private static Guid EnsureCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Product category id is required.");
        }

        return categoryId;
    }

    private static string GenerateSlug(string name)
    {
        var slug = new string(name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        slug = slug.Trim('-');
        if (slug.Length == 0)
        {
            throw new DomainException($"Product name cannot be converted to a slug. Name: {name}.");
        }

        return slug;
    }
}
