using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication;
using System;

namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    /// <summary>
    /// Centralized SSO provider registration to support multiple authentication providers
    /// </summary>
    public static class AuthProviderRegistrar
    {
        /// <summary>
        /// Registers all enabled SSO providers from configuration
        /// </summary>
        public static void RegisterSsoProviders(IServiceCollection services, IConfiguration configuration)
        {
            try
            {
                bool anyProviderRegistered = false;

                // Try to register Azure AD
                var azureSection = configuration.GetSection("Authentication:AzureAd");
                if (azureSection.Exists() && azureSection.GetValue<bool>("Enabled", false))
                {
                    RegisterAzureAd(services, azureSection);
                    anyProviderRegistered = true;
                    Console.WriteLine("SSO: Azure AD (Microsoft Entra ID) provider registered.");
                }

                // Try to register Keycloak
                var keycloakSection = configuration.GetSection("Authentication:Keycloak");
                if (keycloakSection.Exists() && keycloakSection.GetValue<bool>("Enabled", false))
                {
                    RegisterKeycloak(services, keycloakSection);
                    anyProviderRegistered = true;
                    Console.WriteLine("SSO: Keycloak provider registered.");
                }

            // Try to register Google Workspace / Google Identity
            var googleSection = configuration.GetSection("Authentication:GoogleIdentity");
            if (googleSection.Exists() && googleSection.GetValue<bool>("Enabled", false))
            {
                RegisterGoogleIdentity(services, googleSection);
                anyProviderRegistered = true;
                Console.WriteLine("SSO: Google Workspace / Google Identity provider registered.");
            }

                // Try to register Okta
                var oktaSection = configuration.GetSection("Authentication:Okta");
                if (oktaSection.Exists() && oktaSection.GetValue<bool>("Enabled", false))
                {
                    RegisterOkta(services, oktaSection);
                    anyProviderRegistered = true;
                    Console.WriteLine("SSO: Okta provider registered.");
                }

                // Try to register Auth0
                var auth0Section = configuration.GetSection("Authentication:Auth0");
                if (auth0Section.Exists() && auth0Section.GetValue<bool>("Enabled", false))
                {
                    RegisterAuth0(services, auth0Section);
                    anyProviderRegistered = true;
                    Console.WriteLine("SSO: Auth0 provider registered.");
                }

                if (!anyProviderRegistered)
                {
                    Console.WriteLine("SSO: No SSO providers enabled. Using default authentication.");
                }
            }
            catch (Exception e)
            {
                Logging.Handler.Error("SSO Provider Registration", "Initialization Failed", e.Message);
                Console.WriteLine("SSO Provider Registration", "Initialization Failed", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Registers Azure AD / Microsoft Entra ID authentication
        /// </summary>
        private static void RegisterAzureAd(IServiceCollection services, IConfigurationSection section)
        {
            try
            {
                // AddMicrosoftIdentityWebApp already registers Cookie authentication internally
                // So we don't need to call AddCookie separately
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(options =>
                    {
                        section.Bind(options);
                        
                        // Configure sign-out redirect - where to go after Azure AD logout
                        options.SignedOutRedirectUri = "/?logout=true";
                        options.SignedOutCallbackPath = "/signout-callback-oidc";
                        
                        // Don't save tokens in cookie (security best practice)
                        options.SaveTokens = false;
                        
                        // Configure OIDC events
                        options.Events = new OpenIdConnectEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                // After successful authentication, check if user is authorized
                                Console.WriteLine("Azure AD SSO: Token validated successfully");
                                
                                // Try to find email in various Azure AD claim types
                                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                    ?? context.Principal?.FindFirst("preferred_username")?.Value
                                    ?? context.Principal?.FindFirst("upn")?.Value
                                    ?? context.Principal?.FindFirst("email")?.Value;
                                
                                if (!string.IsNullOrEmpty(email))
                                {
                                    Console.WriteLine($"Azure AD SSO: User email identified as: {email}");
                                    
                                    // Check if user exists in database with SSO role
                                    bool userAuthorized = await NetLock_RMM_Web_Console.Classes.Authentication.User.CheckSsoUserAuthorization(email);
                                    
                                    if (!userAuthorized)
                                    {
                                        Console.WriteLine($"Azure AD SSO: User {email} not authorized or does not have SSO role");
                                        Logging.Handler.Debug("Azure AD SSO", "Authorization Failed", $"User {email} not authorized for SSO login");
                                        
                                        // Reject the authentication
                                        context.Fail("User is not authorized for SSO login. Please contact your administrator.");
                                        return;
                                    }
                                    
                                    Console.WriteLine($"Azure AD SSO: User {email} authorized for SSO login");
                                    Logging.Handler.Debug("Azure AD SSO", "Authorization Success", $"User {email} authorized");
                                }
                                else
                                {
                                    Console.WriteLine("Azure AD SSO: Warning - Could not find email claim in token");
                                    // Log all claims for debugging
                                    foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                                    {
                                        Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
                                    }
                                    
                                    // Reject authentication if no email found
                                    context.Fail("Email claim not found in authentication token.");
                                }
                            },
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"Azure AD SSO: Authentication failed - {context.Exception?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRemoteFailure = context =>
                            {
                                Console.WriteLine($"Azure AD SSO: Remote failure - {context.Failure?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                Console.WriteLine("Azure AD SSO: Redirecting to Azure AD for sign-out");
                                Console.WriteLine($"Azure AD SSO: Post logout redirect URI: {context.Properties.RedirectUri}");
                                return Task.CompletedTask;
                            },
                            OnSignedOutCallbackRedirect = context =>
                            {
                                Console.WriteLine("Azure AD SSO: Signed out callback redirect");
                                Console.WriteLine($"Azure AD SSO: Redirecting to: {context.Properties?.RedirectUri ?? "default"}");
                                return Task.CompletedTask;
                            }
                        };
                    });

                // Add Authorization services (without FallbackPolicy to avoid redirect loops)
                // Pages should use @attribute [Authorize] to require authentication
                services.AddAuthorization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring Azure AD SSO: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers Keycloak authentication
        /// </summary>
        private static void RegisterKeycloak(IServiceCollection services, IConfigurationSection section)
        {
            try
            {
                var authority = section.GetValue<string>("Authority");
                var clientId = section.GetValue<string>("ClientId");
                var clientSecret = section.GetValue<string>("ClientSecret");
                var callbackPath = section.GetValue<string>("CallbackPath") ?? "/signin-keycloak";
                var signedOutCallbackPath = section.GetValue<string>("SignedOutCallbackPath") ?? "/signout-callback-keycloak";
                var responseType = section.GetValue<string>("ResponseType") ?? "code";
                var saveTokens = section.GetValue<bool>("SaveTokens", false);
                var getClaimsFromUserInfoEndpoint = section.GetValue<bool>("GetClaimsFromUserInfoEndpoint", true);
                var requireHttpsMetadata = section.GetValue<bool>("RequireHttpsMetadata", true);

                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        options.Authority = authority;
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = responseType;
                        options.SaveTokens = saveTokens;
                        options.GetClaimsFromUserInfoEndpoint = getClaimsFromUserInfoEndpoint;
                        options.RequireHttpsMetadata = requireHttpsMetadata;
                        options.CallbackPath = callbackPath;
                        options.SignedOutCallbackPath = signedOutCallbackPath;
                        
                        // Configure sign-out redirect
                        options.SignedOutRedirectUri = "/?logout=true";
                        
                        // Request standard OIDC scopes
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                        
                        // Map Keycloak claims to standard claims
                        options.TokenValidationParameters.NameClaimType = "preferred_username";
                        options.TokenValidationParameters.RoleClaimType = "roles";
                        
                        // Configure OIDC events
                        options.Events = new OpenIdConnectEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                Console.WriteLine("Keycloak SSO: Token validated successfully");
                                
                                // Try to find email in various claim types
                                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                    ?? context.Principal?.FindFirst("email")?.Value
                                    ?? context.Principal?.FindFirst("preferred_username")?.Value
                                    ?? context.Principal?.FindFirst("upn")?.Value;
                                
                                if (!string.IsNullOrEmpty(email))
                                {
                                    Console.WriteLine($"Keycloak SSO: User email identified as: {email}");
                                    
                                    // Check if user exists in database with SSO role
                                    bool userAuthorized = await NetLock_RMM_Web_Console.Classes.Authentication.User.CheckSsoUserAuthorization(email);
                                    
                                    if (!userAuthorized)
                                    {
                                        Console.WriteLine($"Keycloak SSO: User {email} not authorized or does not have SSO role");
                                        Logging.Handler.Debug("Keycloak SSO", "Authorization Failed", $"User {email} not authorized for SSO login");
                                        
                                        // Reject the authentication
                                        context.Fail("User is not authorized for SSO login. Please contact your administrator.");
                                        return;
                                    }
                                    
                                    Console.WriteLine($"Keycloak SSO: User {email} authorized for SSO login");
                                    Logging.Handler.Debug("Keycloak SSO", "Authorization Success", $"User {email} authorized");
                                }
                                else
                                {
                                    Console.WriteLine("Keycloak SSO: Warning - Could not find email claim in token");
                                    // Log all claims for debugging
                                    foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                                    {
                                        Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
                                    }
                                    
                                    // Reject authentication if no email found
                                    context.Fail("Email claim not found in authentication token.");
                                }
                            },
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"Keycloak SSO: Authentication failed - {context.Exception?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRemoteFailure = context =>
                            {
                                Console.WriteLine($"Keycloak SSO: Remote failure - {context.Failure?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                Console.WriteLine("Keycloak SSO: Redirecting to Keycloak for sign-out");
                                Console.WriteLine($"Keycloak SSO: Post logout redirect URI: {context.Properties.RedirectUri}");
                                return Task.CompletedTask;
                            },
                            OnSignedOutCallbackRedirect = context =>
                            {
                                Console.WriteLine("Keycloak SSO: Signed out callback redirect");
                                Console.WriteLine($"Keycloak SSO: Redirecting to: {context.Properties?.RedirectUri ?? "default"}");
                                return Task.CompletedTask;
                            }
                        };
                    });

                // Add Authorization services
                services.AddAuthorization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring Keycloak SSO: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers Google Workspace / Google Identity authentication
        /// </summary>
        private static void RegisterGoogleIdentity(IServiceCollection services, IConfigurationSection section)
        {
            try
            {
                var clientId = section.GetValue<string>("ClientId");
                var clientSecret = section.GetValue<string>("ClientSecret");
                var callbackPath = section.GetValue<string>("CallbackPath") ?? "/signin-google";
                var signedOutCallbackPath = section.GetValue<string>("SignedOutCallbackPath") ?? "/signout-callback-google";
                var saveTokens = section.GetValue<bool>("SaveTokens", false);
                var hostedDomain = section.GetValue<string>("HostedDomain"); // Optional: Restrict to specific Google Workspace domain

                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        // Google's OpenID Connect endpoints
                        options.Authority = "https://accounts.google.com";
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";
                        options.SaveTokens = saveTokens;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.CallbackPath = callbackPath;
                        options.SignedOutCallbackPath = signedOutCallbackPath;
                        
                        // Configure sign-out redirect
                        options.SignedOutRedirectUri = "/?logout=true";
                        
                        // Request standard OIDC scopes
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                        
                        // Configure OIDC events
                        options.Events = new OpenIdConnectEvents
                        {
                            OnRedirectToIdentityProvider = context =>
                            {
                                // If hosted domain is specified, add hd parameter to restrict to that domain
                                if (!string.IsNullOrEmpty(hostedDomain))
                                {
                                    context.ProtocolMessage.SetParameter("hd", hostedDomain);
                                }
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = async context =>
                            {
                                await ValidateGoogleIdentityToken(context, hostedDomain);
                            },
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"Google SSO: Authentication failed - {context.Exception?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRemoteFailure = context =>
                            {
                                Console.WriteLine($"Google SSO: Remote failure - {context.Failure?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                Console.WriteLine("Google SSO: Redirecting to Google for sign-out");
                                Console.WriteLine($"Google SSO: Post logout redirect URI: {context.Properties.RedirectUri}");
                                return Task.CompletedTask;
                            },
                            OnSignedOutCallbackRedirect = context =>
                            {
                                Console.WriteLine("Google SSO: Signed out callback redirect");
                                Console.WriteLine($"Google SSO: Redirecting to: {context.Properties?.RedirectUri ?? "default"}");
                                return Task.CompletedTask;
                            }
                        };
                        
                        // Map Google claims to standard claims
                        options.TokenValidationParameters.NameClaimType = "name";
                        options.TokenValidationParameters.RoleClaimType = "role";
                    });

                // Add Authorization services
                services.AddAuthorization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring Google SSO: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates Google token and checks user authorization
        /// </summary>
        private static async Task ValidateGoogleIdentityToken(TokenValidatedContext context, string? hostedDomain)
        {
            try
            {
                Console.WriteLine("Google SSO: Token validated successfully");
                
                // Try to find email in various claim types
                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? context.Principal?.FindFirst("email")?.Value;
                
                if (!string.IsNullOrEmpty(email))
                {
                    Console.WriteLine($"Google SSO: User email identified as: {email}");
                    
                    // If hosted domain is specified, verify the email domain matches
                    if (!string.IsNullOrEmpty(hostedDomain))
                    {
                        var emailDomain = email.Split('@').LastOrDefault();
                        if (!string.Equals(emailDomain, hostedDomain, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Google SSO: Email domain {emailDomain} does not match required domain {hostedDomain}");
                            Logging.Handler.Debug("Google SSO", "Domain Validation Failed", 
                                $"User {email} does not belong to required domain {hostedDomain}");
                            context.Fail($"Email must belong to {hostedDomain} domain.");
                            return;
                        }
                    }
                    
                    // Check if user exists in database with SSO role
                    bool userAuthorized = await NetLock_RMM_Web_Console.Classes.Authentication.User.CheckSsoUserAuthorization(email);
                    
                    if (!userAuthorized)
                    {
                        Console.WriteLine($"Google SSO: User {email} not authorized or does not have SSO role");
                        Logging.Handler.Debug("Google SSO", "Authorization Failed", 
                            $"User {email} not authorized for SSO login");
                        
                        // Reject the authentication
                        context.Fail("User is not authorized for SSO login. Please contact your administrator.");
                        return;
                    }
                    
                    Console.WriteLine($"Google SSO: User {email} authorized for SSO login");
                    Logging.Handler.Debug("Google SSO", "Authorization Success", $"User {email} authorized");
                }
                else
                {
                    Console.WriteLine("Google SSO: Warning - Could not find email claim in token");
                    // Log all claims for debugging
                    foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                    {
                        Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
                    }
                    
                    // Reject authentication if no email found
                    context.Fail("Email claim not found in authentication token.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Google SSO: Error during token validation - {e.Message}");
                context.Fail("Error during token validation.");
            }
        }

        /// <summary>
        /// Registers Okta authentication
        /// </summary>
        private static void RegisterOkta(IServiceCollection services, IConfigurationSection section)
        {
            try
            {
                var domain = section.GetValue<string>("Domain");
                var clientId = section.GetValue<string>("ClientId");
                var clientSecret = section.GetValue<string>("ClientSecret");
                var callbackPath = section.GetValue<string>("CallbackPath") ?? "/signin-okta";
                var signedOutCallbackPath = section.GetValue<string>("SignedOutCallbackPath") ?? "/signout-callback-okta";
                var saveTokens = section.GetValue<bool>("SaveTokens", false);
                var getClaimsFromUserInfoEndpoint = section.GetValue<bool>("GetClaimsFromUserInfoEndpoint", true);
                var authorizationServerId = section.GetValue<string>("AuthorizationServerId");

                // Build Okta authority URL
                // If authorization server ID is provided, use it (e.g., "default" or custom)
                // Otherwise use the org authorization server
                var authority = string.IsNullOrEmpty(authorizationServerId)
                    ? $"https://{domain}/oauth2"
                    : $"https://{domain}/oauth2/{authorizationServerId}";

                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        options.Authority = authority;
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";
                        options.SaveTokens = saveTokens;
                        options.GetClaimsFromUserInfoEndpoint = getClaimsFromUserInfoEndpoint;
                        options.CallbackPath = callbackPath;
                        options.SignedOutCallbackPath = signedOutCallbackPath;
                        
                        // Configure sign-out redirect
                        options.SignedOutRedirectUri = "/?logout=true";
                        
                        // Request standard OIDC scopes
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                        
                        // Map Okta claims to standard claims
                        options.TokenValidationParameters.NameClaimType = "name";
                        options.TokenValidationParameters.RoleClaimType = "groups";
                        
                        // Configure OIDC events
                        options.Events = new OpenIdConnectEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                Console.WriteLine("Okta SSO: Token validated successfully");
                                
                                // Try to find email in various claim types
                                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                    ?? context.Principal?.FindFirst("email")?.Value
                                    ?? context.Principal?.FindFirst("preferred_username")?.Value;
                                
                                if (!string.IsNullOrEmpty(email))
                                {
                                    Console.WriteLine($"Okta SSO: User email identified as: {email}");
                                    
                                    // Check if user exists in database with SSO role
                                    bool userAuthorized = await NetLock_RMM_Web_Console.Classes.Authentication.User.CheckSsoUserAuthorization(email);
                                    
                                    if (!userAuthorized)
                                    {
                                        Console.WriteLine($"Okta SSO: User {email} not authorized or does not have SSO role");
                                        Logging.Handler.Debug("Okta SSO", "Authorization Failed", 
                                            $"User {email} not authorized for SSO login");
                                        
                                        // Reject the authentication
                                        context.Fail("User is not authorized for SSO login. Please contact your administrator.");
                                        return;
                                    }
                                    
                                    Console.WriteLine($"Okta SSO: User {email} authorized for SSO login");
                                    Logging.Handler.Debug("Okta SSO", "Authorization Success", $"User {email} authorized");
                                }
                                else
                                {
                                    Console.WriteLine("Okta SSO: Warning - Could not find email claim in token");
                                    // Log all claims for debugging
                                    foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                                    {
                                        Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
                                    }
                                    
                                    // Reject authentication if no email found
                                    context.Fail("Email claim not found in authentication token.");
                                }
                            },
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"Okta SSO: Authentication failed - {context.Exception?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRemoteFailure = context =>
                            {
                                Console.WriteLine($"Okta SSO: Remote failure - {context.Failure?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                Console.WriteLine("Okta SSO: Redirecting to Okta for sign-out");
                                Console.WriteLine($"Okta SSO: Post logout redirect URI: {context.Properties.RedirectUri}");
                                return Task.CompletedTask;
                            },
                            OnSignedOutCallbackRedirect = context =>
                            {
                                Console.WriteLine("Okta SSO: Signed out callback redirect");
                                Console.WriteLine($"Okta SSO: Redirecting to: {context.Properties?.RedirectUri ?? "default"}");
                                return Task.CompletedTask;
                            }
                        };
                    });

                // Add Authorization services
                services.AddAuthorization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring Okta SSO: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers Auth0 authentication
        /// </summary>
        private static void RegisterAuth0(IServiceCollection services, IConfigurationSection section)
        {
            try
            {
                var domain = section.GetValue<string>("Domain");
                var clientId = section.GetValue<string>("ClientId");
                var clientSecret = section.GetValue<string>("ClientSecret");
                var callbackPath = section.GetValue<string>("CallbackPath") ?? "/signin-auth0";
                var signedOutCallbackPath = section.GetValue<string>("SignedOutCallbackPath") ?? "/signout-callback-auth0";
                var saveTokens = section.GetValue<bool>("SaveTokens", false);
                var audience = section.GetValue<string>("Audience");

                // Build Auth0 authority URL
                var authority = $"https://{domain}";

                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        options.Authority = authority;
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";
                        options.SaveTokens = saveTokens;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.CallbackPath = callbackPath;
                        options.SignedOutCallbackPath = signedOutCallbackPath;
                        
                        // Configure sign-out redirect
                        options.SignedOutRedirectUri = "/?logout=true";
                        
                        // Request standard OIDC scopes
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                        
                        // If audience is specified, add it (for API access)
                        if (!string.IsNullOrEmpty(audience))
                        {
                            options.TokenValidationParameters.ValidAudiences = new[] { clientId, audience };
                        }
                        
                        // Map Auth0 claims to standard claims
                        options.TokenValidationParameters.NameClaimType = "name";
                        options.TokenValidationParameters.RoleClaimType = "https://schemas.auth0.com/roles";
                        
                        // Configure OIDC events
                        options.Events = new OpenIdConnectEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                Console.WriteLine("Auth0 SSO: Token validated successfully");
                                
                                // Try to find email in various claim types
                                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                    ?? context.Principal?.FindFirst("email")?.Value
                                    ?? context.Principal?.FindFirst("preferred_username")?.Value;
                                
                                if (!string.IsNullOrEmpty(email))
                                {
                                    Console.WriteLine($"Auth0 SSO: User email identified as: {email}");
                                    
                                    // Check if user exists in database with SSO role
                                    bool userAuthorized = await NetLock_RMM_Web_Console.Classes.Authentication.User.CheckSsoUserAuthorization(email);
                                    
                                    if (!userAuthorized)
                                    {
                                        Console.WriteLine($"Auth0 SSO: User {email} not authorized or does not have SSO role");
                                        Logging.Handler.Debug("Auth0 SSO", "Authorization Failed", 
                                            $"User {email} not authorized for SSO login");
                                        
                                        // Reject the authentication
                                        context.Fail("User is not authorized for SSO login. Please contact your administrator.");
                                        return;
                                    }
                                    
                                    Console.WriteLine($"Auth0 SSO: User {email} authorized for SSO login");
                                    Logging.Handler.Debug("Auth0 SSO", "Authorization Success", $"User {email} authorized");
                                }
                                else
                                {
                                    Console.WriteLine("Auth0 SSO: Warning - Could not find email claim in token");
                                    // Log all claims for debugging
                                    foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                                    {
                                        Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
                                    }
                                    
                                    // Reject authentication if no email found
                                    context.Fail("Email claim not found in authentication token.");
                                }
                            },
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"Auth0 SSO: Authentication failed - {context.Exception?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRemoteFailure = context =>
                            {
                                Console.WriteLine($"Auth0 SSO: Remote failure - {context.Failure?.Message}");
                                context.Response.Redirect("/");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            },
                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                Console.WriteLine("Auth0 SSO: Redirecting to Auth0 for sign-out");
                                
                                // Auth0 logout URL format: https://YOUR_DOMAIN/v2/logout?client_id=YOUR_CLIENT_ID&returnTo=RETURN_URL
                                var logoutUri = $"https://{domain}/v2/logout?client_id={clientId}";
                                var returnUri = context.Properties?.RedirectUri ?? "/?logout=true";
                                
                                // Build full return URL
                                var request = context.Request;
                                var returnUrl = $"{request.Scheme}://{request.Host}{returnUri}";
                                
                                logoutUri += $"&returnTo={Uri.EscapeDataString(returnUrl)}";
                                
                                Console.WriteLine($"Auth0 SSO: Logout URI: {logoutUri}");
                                
                                context.Response.Redirect(logoutUri);
                                context.HandleResponse();
                                
                                return Task.CompletedTask;
                            },
                            OnSignedOutCallbackRedirect = context =>
                            {
                                Console.WriteLine("Auth0 SSO: Signed out callback redirect");
                                Console.WriteLine($"Auth0 SSO: Redirecting to: {context.Properties?.RedirectUri ?? "default"}");
                                return Task.CompletedTask;
                            }
                        };
                    });

                // Add Authorization services
                services.AddAuthorization();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring Auth0 SSO: {ex.Message}");
                throw;
            }
        }
    }
}

