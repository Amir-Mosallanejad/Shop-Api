using ShopApi.Domain.Common;
using ShopApi.Domain.Enums;
using ShopApi.Domain.Exceptions;
using ShopApi.Domain.ValueObjects;

namespace ShopApi.Domain.Entities;

public sealed class Order : BaseEntity
{
    private const int MinQuantity = 1;
    private const int MaxQuantity = 999;
    private const int OrderNumberSuffixLength = 6;
    private const string OrderNumberChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly List<OrderItem> _items = new();

    public string OrderNumber { get; private set; }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public Money TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public Guid AddressId { get; private set; }

    public Address? Address { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order(Guid userId, Guid addressId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Order user id is required.");
        }

        if (addressId == Guid.Empty)
        {
            throw new DomainException("Order address id is required.");
        }

        UserId = userId;
        AddressId = addressId;
        OrderNumber = GenerateOrderNumber();
        Status = OrderStatus.Pending;
        TotalAmount = Money.Zero();
    }

    public static Order Create(
        Guid userId,
        Guid addressId,
        IEnumerable<(Guid productId, int quantity, Money unitPrice)> lines)
    {
        var order = new Order(userId, addressId);

        var lineList = lines.ToList();
        if (lineList.Count == 0)
        {
            throw new DomainException("Order must contain at least one item.");
        }

        var currency = lineList[0].unitPrice?.Currency
            ?? throw new DomainException("Order item unit price is required.");

        foreach (var (productId, quantity, unitPrice) in lineList)
        {
            if (productId == Guid.Empty)
            {
                throw new DomainException("Order item product id is required.");
            }

            if (quantity < MinQuantity || quantity > MaxQuantity)
            {
                throw new DomainException(
                    $"Order item quantity must be between {MinQuantity} and {MaxQuantity}. Quantity: {quantity}.");
            }

            if (unitPrice is null)
            {
                throw new DomainException("Order item unit price is required.");
            }

            if (!string.Equals(unitPrice.Currency, currency, StringComparison.Ordinal))
            {
                throw new DomainException(
                    $"Order items must share a single currency. Expected: {currency}, Actual: {unitPrice.Currency}.");
            }

            var item = new OrderItem(Guid.Empty, productId, quantity, unitPrice);
            item.AttachToOrder(order.Id);
            order._items.Add(item);
        }

        order.RecalculateTotal();
        return order;
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        if (newStatus == Status)
        {
            return;
        }

        var allowed = Status switch
        {
            OrderStatus.Pending => newStatus is OrderStatus.Paid or OrderStatus.Cancelled,
            OrderStatus.Paid => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus is OrderStatus.Delivered,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException($"Order status transition is not allowed. From: {Status}, To: {newStatus}.");
        }

        Status = newStatus;
        Touch();
    }

    private Money RecalculateTotal()
    {
        var total = Money.Zero(_items[0].UnitPrice.Currency);
        foreach (var item in _items)
        {
            total += item.LineTotal;
        }

        TotalAmount = total;
        return total;
    }

    private static string GenerateOrderNumber()
    {
        var suffix = new string(Enumerable.Range(0, OrderNumberSuffixLength)
            .Select(_ => OrderNumberChars[Random.Shared.Next(OrderNumberChars.Length)])
            .ToArray());

        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
    }
}
