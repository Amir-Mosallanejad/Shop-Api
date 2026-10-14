using Microsoft.EntityFrameworkCore;
using ShopApi.Domain.Entities;
using ShopApi.Domain.ValueObjects;

namespace ShopApi.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var electronics = new Category("Electronics");
        var books = new Category("Books");
        var clothing = new Category("Clothing");

        await context.Categories.AddRangeAsync([electronics, books, clothing], cancellationToken);

        var laptop = new Product(
            "Laptop Pro",
            "High-performance laptop for developers.",
            new Money(1299.99m, "USD"),
            25,
            electronics.Id);
        laptop.AddImage("https://example.com/images/laptop-pro.jpg", true);

        var mouse = new Product(
            "Wireless Mouse",
            "Ergonomic wireless mouse with long battery life.",
            new Money(29.99m, "USD"),
            150,
            electronics.Id);
        mouse.AddImage("https://example.com/images/wireless-mouse.jpg", true);

        var book = new Product(
            "C# in Depth",
            "Deep dive into the C# language.",
            new Money(49.99m, "USD"),
            60,
            books.Id);
        book.AddImage("https://example.com/images/csharp-in-depth.jpg", true);

        var shirt = new Product(
            "Cotton T-Shirt",
            "Comfortable everyday cotton t-shirt.",
            new Money(19.99m, "USD"),
            200,
            clothing.Id);
        shirt.AddImage("https://example.com/images/cotton-tshirt.jpg", true);

        await context.Products.AddRangeAsync([laptop, mouse, book, shirt], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
