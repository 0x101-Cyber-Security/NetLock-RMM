using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private static readonly ClaimsIdentity identity = new ClaimsIdentity();
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(identity);

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Initial Dummy State
            try
            {
                var userSession = await GetUserSessionAsync();

                if (userSession == null)
                    return new AuthenticationState(_anonymous);

                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName),
                    new Claim(ClaimTypes.Role, userSession.Role)
                }, "CustomAuth"));

                return new AuthenticationState(claimsPrincipal);
            }
            catch (Exception ex)
            {
                //Logging.Handler.Error("CustomAuthentificationStateProvider", "GetAuthenticationStateAsync", ex.ToString());
                return new AuthenticationState(_anonymous);
            }
            
        }

        private async Task<UserSession> GetUserSessionAsync()
        {
            try
            {
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                return userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Logging.Handler.Error("CustomAuthentificationStateProvider", "GetUserSessionAsync", ex.ToString());
                return null;
            }
        }

        public async Task UpdateAuthentificationState(UserSession userSession, bool delete)
        {
            try
            {
                ClaimsPrincipal claimsPrincipal;

                if (userSession != null && delete == false)
                {
                    await _sessionStorage.SetAsync("UserSession", userSession);

                    claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userSession.UserName),
                        new Claim(ClaimTypes.Role, userSession.Role)
                    }));
                }
                else if (userSession != null && delete)
                {
                    await _sessionStorage.DeleteAsync("UserSession");
                    claimsPrincipal = _anonymous;
                }
                else
                {
                    await _sessionStorage.DeleteAsync("UserSession");
                    claimsPrincipal = _anonymous;
                }

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
            }
            catch (Exception ex)
            {
                await _sessionStorage.DeleteAsync("UserSession");
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                Logging.Handler.Error("CustomAuthentificationStateProvider", "UpdateAuthentificationState", ex.ToString());
            }
        }
    }
}
