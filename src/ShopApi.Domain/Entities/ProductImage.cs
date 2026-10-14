using ShopApi.Domain.Common;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Domain.Entities;

public sealed class ProductImage : BaseEntity
{
    private const int MaxUrlLength = 2000;

    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    public string Url { get; private set; }

    public bool IsMain { get; private set; }

    public ProductImage(Guid productId, string url, bool isMain)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product image product id is required.");
        }

        ProductId = productId;
        Url = EnsureUrl(url);
        IsMain = isMain;
    }

    public void MarkAsMain()
    {
        IsMain = true;
        Touch();
    }

    public void UnmarkAsMain()
    {
        IsMain = false;
        Touch();
    }

    private static string EnsureUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Product image url is required.");
        }

        var trimmed = url.Trim();
        if (trimmed.Length > MaxUrlLength)
        {
            throw new DomainException($"Product image url must be at most {MaxUrlLength} characters.");
        }

        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException($"Product image url must start with http:// or https://. Url: {url}.");
        }

        return trimmed;
    }
}
