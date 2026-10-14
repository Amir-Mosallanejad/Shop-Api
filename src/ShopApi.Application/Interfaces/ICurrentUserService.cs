namespace ShopApi.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }
}
