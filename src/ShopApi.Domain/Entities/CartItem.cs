using ShopApi.Domain.Common;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Domain.Entities;

public sealed class CartItem : BaseEntity
{
    private const int MinQuantity = 1;
    private const int MaxQuantity = 999;

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    public int Quantity { get; private set; }

    public CartItem(Guid userId, Guid productId, int quantity)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Cart item user id is required.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainException("Cart item product id is required.");
        }

        UserId = userId;
        ProductId = productId;
        Quantity = EnsureQuantity(quantity);
    }

    public void ChangeQuantity(int quantity)
    {
        Quantity = EnsureQuantity(quantity);
        Touch();
    }

    public void Increase(int amount)
    {
        if (amount <= 0)
        {
            throw new DomainException($"Cart item increase amount must be positive. Amount: {amount}.");
        }

        Quantity = EnsureQuantity(Quantity + amount);
        Touch();
    }

    public void Decrease(int amount)
    {
        if (amount <= 0)
        {
            throw new DomainException($"Cart item decrease amount must be positive. Amount: {amount}.");
        }

        Quantity = EnsureQuantity(Quantity - amount);
        Touch();
    }

    private static int EnsureQuantity(int quantity)
    {
        if (quantity < MinQuantity || quantity > MaxQuantity)
        {
            throw new DomainException(
                $"Cart item quantity must be between {MinQuantity} and {MaxQuantity}. Quantity: {quantity}.");
        }

        return quantity;
    }
}
