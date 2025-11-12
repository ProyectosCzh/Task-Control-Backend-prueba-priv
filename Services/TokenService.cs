using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using taskcontrolv1.Services.Interfaces;

namespace taskcontrolv1.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;

    public TokenService(IConfiguration config)
    {
        _config = config;
        _key = System.Text.Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!);
        _issuer = _config["JwtSettings:Issuer"]!;
        _audience = _config["JwtSettings:Audience"]!;
    }

    public string CreateAccessToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string PlainToken, string Hash) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = Convert.ToBase64String(bytes);
        var hash = HashRefreshToken(plain);
        return (plain, hash);
    }

    public string HashRefreshToken(string plainToken)
    {
        using var sha256 = SHA256.Create();
        var raw = System.Text.Encoding.UTF8.GetBytes(plainToken);
        var hashed = sha256.ComputeHash(raw);
        return Convert.ToBase64String(hashed);
    }
}