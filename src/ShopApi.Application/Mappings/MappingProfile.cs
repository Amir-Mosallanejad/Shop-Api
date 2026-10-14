using AutoMapper;
using ShopApi.Application.Common.Dtos;
using ShopApi.Domain.Entities;

namespace ShopApi.Application.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));

        CreateMap<Category, CategoryDto>();

        CreateMap<Address, AddressDto>();

        CreateMap<ProductImage, ProductImageDto>();

        // NOTE: Product/CartItem/Order/OrderItem use explicit ConstructUsing because
        // AutoMapper 13 cannot constructor-bind their nested-path flattenings
        // (e.g. s.Price.Amount). The expressions below preserve the exact
        // null-tolerance semantics the ForMember versions intended.
        CreateMap<Product, ProductDto>()
            .ConstructUsing((s, ctx) => new ProductDto(
                s.Id,
                s.Name,
                s.Slug,
                s.Description,
                s.Price.Amount,
                s.Price.Currency,
                s.Stock,
                s.CategoryId,
                s.Category != null ? s.Category.Name : null,
                ctx.Mapper.Map<IReadOnlyList<ProductImageDto>>(s.Images),
                s.CreatedAt))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<CartItem, CartItemDto>()
            .ConstructUsing(s => new CartItemDto(
                s.ProductId,
                s.Product != null ? s.Product.Name : "Unknown",
                s.Product != null ? s.Product.Price.Amount : 0,
                s.Product != null ? s.Product.Price.Currency : "USD",
                s.Quantity,
                s.Product != null ? s.Product.Price.Amount * s.Quantity : 0,
                s.Product != null
                    ? s.Product.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                        ?? s.Product.Images.Select(i => i.Url).FirstOrDefault()
                    : null))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Order, OrderDto>()
            .ConstructUsing((s, ctx) => new OrderDto(
                s.Id,
                s.OrderNumber,
                s.Status.ToString(),
                s.TotalAmount.Amount,
                s.TotalAmount.Currency,
                ctx.Mapper.Map<AddressDto>(s.Address),
                ctx.Mapper.Map<IReadOnlyList<OrderItemDto>>(s.Items),
                s.CreatedAt))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<OrderItem, OrderItemDto>()
            .ConstructUsing(s => new OrderItemDto(
                s.ProductId,
                s.Product != null ? s.Product.Name : "Unknown",
                s.Quantity,
                s.UnitPrice.Amount,
                s.UnitPrice.Currency,
                s.UnitPrice.Amount * s.Quantity))
            .ForAllMembers(opt => opt.Ignore());
    }
}
