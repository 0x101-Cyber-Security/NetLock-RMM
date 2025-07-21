using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NetLock_RMM_Web_Console.Classes.Authentication;
using System.Configuration;

public class TokenService
{
    private readonly SymmetricSecurityKey _signingKey = new(Encoding.UTF8.GetBytes(NetLock_RMM_Web_Console.Configuration.Web_Console.token_service_secret_key));

    public string GenerateToken(UserSession userSession)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, userSession.UserName),
                new(ClaimTypes.Role, userSession.Role),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(16), // 16 hours are a usual work day (with pause included and for dashboard monitoring)
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature),
                Issuer = CustomAuthenticationStateProvider.Issuer,  // New entry
                Audience = CustomAuthenticationStateProvider.Audience // New entry
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("Classes.Authentification.TokenService", "GenerateToken", ex.ToString());

            // Log the exception if needed
            return string.Empty;
        }
    }
}
