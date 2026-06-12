using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Infrastructure.Security;
using FluentResults;
using MediatR;
using Microsoft.IdentityModel.Tokens;

namespace CGI.LeadTracker.API.Application.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<TokenDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration  = configuration;
    }

    public async Task<Result<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !PasswordHelper.Verify(request.Password, user.PasswordHash))
            return Result.Fail<TokenDto>("E-mail ou senha inválidos.");

        var secret            = _configuration["Auth:Secret"]!;
        var issuer            = _configuration["Auth:Issuer"]!;
        var audience          = _configuration["Auth:Audience"]!;
        var expirationMinutes = _configuration.GetValue("Auth:ExpirationMinutes", 480);
        var expiresAt         = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.Name),
            new Claim(ClaimTypes.Role,               user.Role),
        };

        var token = new JwtSecurityToken(
            issuer:            issuer,
            audience:          audience,
            claims:            claims,
            expires:           expiresAt,
            signingCredentials: credentials);

        return Result.Ok(new TokenDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Name,
            user.Email,
            user.Role,
            expiresAt));
    }
}
