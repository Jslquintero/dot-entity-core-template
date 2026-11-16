using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Template.Api.Configuration;
using Template.Api.Models;
using Template.Model.Entities;

namespace Template.Api.Services;

public sealed class TokenService(IOptions<JwtOptions> options, UserManager<User> users)
{
    public async Task<LoginResponse> CreateAsync(User user)
    {
        var jwt = options.Value;
        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtOptions.SecurityStampClaim, await users.GetSecurityStampAsync(user))
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256)
        });
        return new LoginResponse(handler.WriteToken(token), token.ValidTo,
            new UserViewModel(user.Id, user.Email ?? string.Empty, user.Name, user.LastName, roles));
    }
}
