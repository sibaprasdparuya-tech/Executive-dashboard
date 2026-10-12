using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcxiomCRM.Models;
using Microsoft.IdentityModel.Tokens;

namespace AcxiomCRM.Services;

public interface IJwtTokenService
{
    string GenerateToken(ApplicationUser user, string role);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(ApplicationUser user, string role)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? "AcxiomCRM_Default_Super_Secure_Secret_Key_2026_Minimum_32_Chars!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "AcxiomCRM";
        var audience = _configuration["JwtSettings:Audience"] ?? "AcxiomCRMClient";
        var expiryMinutes = int.TryParse(_configuration["JwtSettings:ExpiryInMinutes"], out var mins) ? mins : 1440;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, role),
            new("FullName", user.FullName ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
