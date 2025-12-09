using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly ClaimsIdentity _anonymousIdentity = new();
        private readonly ClaimsPrincipal _anonymous = new(_anonymousIdentity);
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly TokenService _tokenService;
        private readonly TokenValidationParameters _validationParameters;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        // Cache for normalized SSO claims to avoid repeated normalization
        private ClaimsPrincipal? _cachedNormalizedPrincipal;
        private string? _cachedPrincipalIdentifier;

        public const string Issuer = "NetLockRMMWebConsole";
        public const string Audience = "NetLockRMMUser";

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage, TokenService tokenService, IHttpContextAccessor httpContextAccessor)
        {
            try
            {
                _sessionStorage = sessionStorage;
                _tokenService = tokenService;
                _httpContextAccessor = httpContextAccessor;

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
                // Check HttpContext.User first (for SSO authentication via Cookie/OIDC)
                try
                {
                    var ctxUser = _httpContextAccessor?.HttpContext?.User;
                    if (ctxUser != null && ctxUser.Identity != null && ctxUser.Identity.IsAuthenticated)
                    {
                        // User is authenticated via SSO (Azure AD, etc.)
                        // Normalize claims for SSO users with caching
                        var normalizedPrincipal = GetOrCreateNormalizedPrincipal(ctxUser);
                        return new AuthenticationState(normalizedPrincipal);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Classes.Authentification.CustomAuthenticationStateProvider", "GetAuthenticationStateAsync: HttpContext check error", ex.ToString());
                }

                // If not authenticated via SSO, check for custom JWT SessionToken
                var result = await _sessionStorage.GetAsync<string>("SessionToken");
                var jwt = result.Success ? result.Value : null;

                if (!string.IsNullOrEmpty(jwt))
                {
                    var principal = ValidateToken(jwt);
                    return new AuthenticationState(principal);
                }

                // No authentication found
                return new AuthenticationState(_anonymous);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // This typically occurs during prerendering
                // Check HttpContext as fallback
                try
                {
                    var ctxUser = _httpContextAccessor?.HttpContext?.User;
                    if (ctxUser != null && ctxUser.Identity != null && ctxUser.Identity.IsAuthenticated)
                    {
                        var normalizedPrincipal = GetOrCreateNormalizedPrincipal(ctxUser);
                        return new AuthenticationState(normalizedPrincipal);
                    }
                }
                catch { }
                
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

        /// <summary>
        /// Gets or creates a cached normalized principal to avoid repeated normalization
        /// </summary>
        private ClaimsPrincipal GetOrCreateNormalizedPrincipal(ClaimsPrincipal principal)
        {
            try
            {
                // Create a unique identifier for this principal based on key claims
                var identifier = principal.Identity?.Name ?? 
                                principal.FindFirst(ClaimTypes.Email)?.Value ?? 
                                principal.FindFirst("preferred_username")?.Value ?? 
                                "unknown";

                // Check if we have a cached version for this principal
                if (_cachedPrincipalIdentifier == identifier && _cachedNormalizedPrincipal != null)
                {
                    return _cachedNormalizedPrincipal;
                }

                // Normalize and cache
                var normalized = NormalizeSsoClaims(principal);
                _cachedPrincipalIdentifier = identifier;
                _cachedNormalizedPrincipal = normalized;

                return normalized;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CustomAuthenticationStateProvider", "GetOrCreateNormalizedPrincipal", ex.ToString());
                return NormalizeSsoClaims(principal); // Fallback without caching
            }
        }

        /// <summary>
        /// Normalizes SSO claims to ensure email and name are available in standard claim types
        /// Azure AD and other providers use different claim names that need to be mapped
        /// </summary>
        private ClaimsPrincipal NormalizeSsoClaims(ClaimsPrincipal principal)
        {
            try
            {
                var claims = new List<Claim>(principal.Claims);
                
                // Azure AD/Microsoft Entra ID claim mappings
                // Try to find email in various possible claim types
                var emailClaim = principal.FindFirst(ClaimTypes.Email) 
                    ?? principal.FindFirst("preferred_username") 
                    ?? principal.FindFirst("upn")
                    ?? principal.FindFirst("email")
                    ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

                // If we found an email but it's not in ClaimTypes.Email, add it
                if (emailClaim != null && !claims.Any(c => c.Type == ClaimTypes.Email && c.Value == emailClaim.Value))
                {
                    claims.Add(new Claim(ClaimTypes.Email, emailClaim.Value));
                    Logging.Handler.Debug("CustomAuthenticationStateProvider", "NormalizeSsoClaims", $"Added ClaimTypes.Email: {emailClaim.Value}");
                }

                // Try to find name in various possible claim types
                var nameClaim = principal.FindFirst(ClaimTypes.Name)
                    ?? principal.FindFirst("name")
                    ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");

                if (nameClaim != null && !claims.Any(c => c.Type == ClaimTypes.Name && c.Value == nameClaim.Value))
                {
                    claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
                }

                // Check for role claims
                var roleClaim = principal.FindFirst(ClaimTypes.Role)
                    ?? principal.FindFirst("role")
                    ?? principal.FindFirst("roles");

                if (roleClaim != null && !claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                }

                // Create new identity with normalized claims
                var identity = new ClaimsIdentity(claims, principal.Identity?.AuthenticationType ?? "SSO");
                return new ClaimsPrincipal(identity);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CustomAuthenticationStateProvider", "NormalizeSsoClaims", ex.ToString());
                return principal; // Return original if normalization fails
            }
        }

        /// <summary>
        /// Clears the SSO claims cache. Should be called on logout.
        /// </summary>
        public void ClearSsoCache()
        {
            _cachedNormalizedPrincipal = null;
            _cachedPrincipalIdentifier = null;
            Logging.Handler.Debug("CustomAuthenticationStateProvider", "ClearSsoCache", "SSO cache cleared");
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
                    
                    // Clear SSO cache
                    ClearSsoCache();
                    
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
