using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly ClaimsIdentity _anonymousIdentity = new();
        private readonly ClaimsPrincipal _anonymous = new(_anonymousIdentity);
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly TokenService _tokenService;
        private readonly TokenValidationParameters _validationParameters;

        public const string Issuer = "NetLockRMMWebConsole";
        public const string Audience = "NetLockRMMUser";

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage, TokenService tokenService)
        {
            try
            {
                _sessionStorage = sessionStorage;
                _tokenService = tokenService;

                // TokenValidationParameters must have the same settings as during generation:
                _validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,

                    ValidateAudience = true,
                    ValidAudience = Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.Web_Console.token_service_secret_key)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentification.CustomAuthenticationStateProvider", "General error", ex.ToString());
            }
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Verhindert JS-Interop während des Prerenderings
                var result = await _sessionStorage.GetAsync<string>("SessionToken");
                var jwt = result.Success ? result.Value : null;

                if (string.IsNullOrEmpty(jwt))
                    return new AuthenticationState(_anonymous);

                var principal = ValidateToken(jwt);
                return new AuthenticationState(principal);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // This typically occurs during prerendering
                Logging.Handler.Error("Classes.Authentification.CustomAuthenticationStateProvider", "GetAuthenticationStateAsync: Error while getting authentication state", ex.ToString());
                return new AuthenticationState(_anonymous);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentification.CustomAuthenticationStateProvider", "GetAuthenticationStateAsync: Error while getting authentication state", ex.ToString());
                return new AuthenticationState(_anonymous);
            }
        }

        private ClaimsPrincipal ValidateToken(string jwt)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();

                // Checks signature and expiry
                var principal = handler.ValidateToken(jwt, _validationParameters, out var validatedToken);

                // This ensures that ClaimTypes.Email and ClaimTypes.Role are included
                return principal;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentification.CustomAuthenticationStateProvider", "ValidateToken: Error while validating token", ex.ToString());
                return _anonymous;
            }
        }

        public async Task UpdateAuthentificationState(UserSession userSession, bool delete)
        {
            try
            {
                ClaimsPrincipal principal;

                if (userSession != null && !delete)
                {
                    // 1) Create new JWT
                    var jwt = _tokenService.GenerateToken(userSession);
                    await _sessionStorage.SetAsync("SessionToken", jwt);

                    // 2) ClaimsPrincipal for NotifyAuthenticationStateChanged creation
                    var claims = new List<Claim>
                    {
                    new Claim(ClaimTypes.Email, userSession.UserName),
                    new Claim(ClaimTypes.Role,  userSession.Role)
                    };
                    principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth"));

                    // Update remote_session_token
                    await Classes.Authentication.User.Update_Remote_Session_Token(userSession.UserName);
                }
                else
                {
                    // Logout case
                    await _sessionStorage.DeleteAsync("SessionToken");
                    principal = _anonymous;
                }

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentification.CustomAuthenticationStateProvider", "UpdateAuthentificationState", ex.ToString());
            }
        }
    }
}
