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

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Price.Amount))
            .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.Price.Currency))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null));

        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : "Unknown"))
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.Product != null ? s.Product.Price.Amount : 0))
            .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.Product != null ? s.Product.Price.Currency : "USD"))
            .ForMember(d => d.LineTotal, opt => opt.MapFrom(s =>
                s.Product != null ? s.Product.Price.Amount * s.Quantity : 0))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s =>
                s.Product != null && s.Product.Images != null
                    ? s.Product.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                        ?? s.Product.Images.Select(i => i.Url).FirstOrDefault()
                    : null));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.TotalAmount.Amount))
            .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.TotalAmount.Currency));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : "Unknown"))
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPrice.Amount))
            .ForMember(d => d.Currency, opt => opt.MapFrom(s => s.UnitPrice.Currency))
            .ForMember(d => d.LineTotal, opt => opt.MapFrom(s => s.UnitPrice.Amount * s.Quantity));
    }
}
