using BlogSystem.Configuration.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlogSystem.Services;

public interface ITokenFactory
{
    string Create(Guid teacherId);
}

public class TokenFactory : ITokenFactory
{
    private readonly TokenOptions _options;

    public TokenFactory(
        IOptions<TokenOptions> options)
    {
        _options = options.Value;
    }

    public string Create(Guid userId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime expires = now.AddHours(_options.AccessExpiresHours);

        SigningCredentials credentials = new(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        string token = new JwtSecurityTokenHandler()
            .WriteToken(
                new JwtSecurityToken(
                    issuer: _options.Issuer,
                    audience: _options.Audience,
                    notBefore: now,
                    expires: expires,
                    claims: [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ],
                    signingCredentials: credentials));

        return token;
    }
}