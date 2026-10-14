using ShopApi.Application.Common.Dtos;
using ShopApi.Domain.Entities;

namespace ShopApi.Application.Features.Cart;

internal static class CartMapping
{
    private const string DefaultCurrency = "USD";

    internal static CartDto ToCart(IEnumerable<CartItem> lines)
    {
        var lineList = lines.ToList();
        var items = lineList.Select(line =>
        {
            var unitPrice = line.Product?.Price.Amount ?? 0;
            var currency = line.Product?.Price.Currency ?? DefaultCurrency;
            var imageUrl = line.Product?.Images
                .Where(i => i.IsMain)
                .Select(i => i.Url)
                .FirstOrDefault()
                ?? line.Product?.Images.Select(i => i.Url).FirstOrDefault();

            return new CartItemDto(
                line.ProductId,
                line.Product?.Name ?? "Unknown",
                unitPrice,
                currency,
                line.Quantity,
                unitPrice * line.Quantity,
                imageUrl);
        }).ToList();

        var currency = lineList.FirstOrDefault()?.Product?.Price.Currency ?? DefaultCurrency;
        return new CartDto(
            items,
            items.Sum(i => i.LineTotal),
            currency,
            items.Sum(i => i.Quantity));
    }
}
