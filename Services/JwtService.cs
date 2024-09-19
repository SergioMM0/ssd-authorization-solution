using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ssd_authorization_solution.Util;

namespace ssd_authorization_solution.Services;

public class JwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public string GenerateToken(string userName, string role)
    {
        // Create Claims
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Create the signing key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the JWT token
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_jwtSettings.TokenExpirationMinutes),
            signingCredentials: creds
        );

        // Return the serialized JWT token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
