using ShopApi.Domain.Exceptions;

namespace ShopApi.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; init; }

    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException($"Money amount cannot be negative. Amount: {amount}.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Money currency is required.");
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
        {
            throw new DomainException($"Money currency must be a 3-letter ISO code. Currency: {currency}.");
        }

        Amount = Math.Round(amount, 2, MidpointRounding.ToZero);
        Currency = normalized;
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public static Money From(decimal amount, string currency = "USD") => new(amount, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator *(Money money, int multiplier)
    {
        if (multiplier < 0)
        {
            throw new DomainException($"Money multiplier cannot be negative. Multiplier: {multiplier}.");
        }

        return new Money(money.Amount * multiplier, money.Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new DomainException(
                $"Cannot operate on Money with different currencies. Left: {left.Currency}, Right: {right.Currency}.");
        }
    }
}
