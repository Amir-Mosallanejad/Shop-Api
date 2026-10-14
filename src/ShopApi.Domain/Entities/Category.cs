using ShopApi.Domain.Common;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Domain.Entities;

public sealed class Category : BaseEntity
{
    private const int MinNameLength = 2;
    private const int MaxNameLength = 100;
    private const int MaxSlugLength = 120;

    private readonly List<Category> _children = new();
    private readonly List<Product> _products = new();

    public string Name { get; private set; }

    // TODO(phase-04): enforce slug uniqueness with a database unique index.
    public string Slug { get; private set; }

    public Guid? ParentId { get; private set; }

    public Category? Parent { get; private set; }

    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    public Category(string name, Guid? parentId = null, string? slug = null)
    {
        Name = EnsureName(name);
        Slug = EnsureSlug(slug ?? GenerateSlug(name));
        ParentId = parentId;
    }

    public void Rename(string name)
    {
        Name = EnsureName(name);
        Slug = EnsureSlug(GenerateSlug(name));
        Touch();
    }

    public void SetParent(Guid? parentId)
    {
        if (parentId == Id)
        {
            throw new DomainException("Category cannot be its own parent.");
        }

        ParentId = parentId;
        Touch();
    }

    private static string EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length < MinNameLength || trimmed.Length > MaxNameLength)
        {
            throw new DomainException(
                $"Category name must be between {MinNameLength} and {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static string EnsureSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < MinNameLength || slug.Length > MaxSlugLength)
        {
            throw new DomainException(
                $"Category slug must be between {MinNameLength} and {MaxSlugLength} characters.");
        }

        return slug;
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
            throw new DomainException($"Category name cannot be converted to a slug. Name: {name}.");
        }

        return slug;
    }
}
