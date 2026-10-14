using ShopApi.Domain.Common;
using ShopApi.Domain.Exceptions;
using ShopApi.Domain.ValueObjects;

namespace ShopApi.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    private const int MinQuantity = 1;
    private const int MaxQuantity = 999;

    public Guid OrderId { get; private set; }

    public Order? Order { get; private set; }

    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice * Quantity;

    // EF Core materialization only. Domain code must use the public constructor.
    private OrderItem()
    {
        UnitPrice = null!;
    }

    public OrderItem(Guid orderId, Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Order item product id is required.");
        }

        OrderId = orderId;
        ProductId = productId;
        Quantity = EnsureQuantity(quantity);
        UnitPrice = unitPrice ?? throw new DomainException("Order item unit price is required.");
    }

    internal void AttachToOrder(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order item order id is required.");
        }

        OrderId = orderId;
        Touch();
    }

    private static int EnsureQuantity(int quantity)
    {
        if (quantity < MinQuantity || quantity > MaxQuantity)
        {
            throw new DomainException(
                $"Order item quantity must be between {MinQuantity} and {MaxQuantity}. Quantity: {quantity}.");
        }

        return quantity;
    }
}
