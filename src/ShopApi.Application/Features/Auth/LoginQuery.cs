using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Auth;

public sealed record LoginQuery(string Email, string Password) : IRequest<Result<AuthResponseDto>>;

public sealed class LoginQueryHandler : IRequestHandler<LoginQuery, Result<AuthResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public LoginQueryHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid email or password."));
        }

        var token = _jwtService.GenerateToken(user);
        return Result.Success(new AuthResponseDto(token, _mapper.Map<UserDto>(user)));
    }
}
