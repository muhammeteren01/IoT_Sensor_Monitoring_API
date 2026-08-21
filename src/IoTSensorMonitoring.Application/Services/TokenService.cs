using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IoTSensorMonitoring.Application.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string CreateToken(User user, out DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.CompanyId.HasValue)
        {
            claims.Add(new Claim("company_id", user.CompanyId.Value.ToString()));
        }

        return CreateSignedToken(claims, out expiresAt);
    }

    public string CreateClientCredentialsToken(IntegrationClient client, out DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new(ClaimTypes.Role, AppRoles.IntegrationClient),
            new("company_id", client.CompanyId.ToString()),
            new("client_id", client.ClientId),
            new("token_type", "client_credentials")
        };

        return CreateSignedToken(claims, out expiresAt);
    }

    private string CreateSignedToken(IEnumerable<Claim> claims, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
