using System.Configuration;
using MudBlazor.Services;
using NetLock_RMM_Web_Console.Components;
using NetLock_RMM_Web_Console;
using System.Net;
using NetLock_RMM_Web_Console.Classes.MySQL;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using NetLock_RMM_Web_Console.Classes.Authentication;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authentication;
using NetLock_RMM_Web_Console.Configuration;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.Features;
using NetLock_RMM_Web_Console.Components.Pages.Devices;
using MudBlazor;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using static MudBlazor.Defaults;
using NetLock_RMM_Web_Console.Classes.Helper;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;

NetLock_RMM_Web_Console.Classes.Setup.Directories.Check_Directories(); // Check if directories exist and create them if not

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Get UseHttps from config
var http = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Http:Enabled", true);
var http_port = builder.Configuration.GetValue<int>("Kestrel:Endpoint:Http:Port", 80);
var https = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Https:Enabled", true);
var https_port = builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Port", 443);
var https_force = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Https:Force", true);
var hsts = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Https:Hsts:Enabled", true);
var hsts_max_age = builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Hsts:MaxAge");
var cert_path = builder.Configuration.GetValue<string>("Kestrel:Endpoint:Https:Certificate:Path", String.Empty);
var cert_password = builder.Configuration.GetValue<string>("Kestrel:Endpoint:Https:Certificate:Password", String.Empty);
var loggingEnabled = builder.Configuration.GetValue<bool>("Logging:Custom:Enabled", true);
var publicOverrideUrlRaw = builder.Configuration.GetValue<string>("Webinterface:publicOverrideUrl", string.Empty);
var publicOverrideUrl = publicOverrideUrlRaw.TrimEnd('/');
var allowedIps = builder.Configuration.GetSection("Kestrel:IpWhitelist").Get<List<string>>() ?? new List<string>();
var knownProxies = builder.Configuration.GetSection("Kestrel:KnownProxies").Get<List<string>>() ?? new List<string>();

Web_Console.loggingEnabled = loggingEnabled;

// Add Remote_Server to the services
var remoteServerConfig = builder.Configuration.GetSection("NetLock_Remote_Server").Get<NetLock_RMM_Web_Console.Classes.Remote_Server.Config>();
Remote_Server.Hostname = remoteServerConfig.Server;

if (remoteServerConfig.UseSSL)
{
    Remote_Server.Connection_String = $"https://{remoteServerConfig.Server}:{remoteServerConfig.Port}";
}
else
{
    Remote_Server.Connection_String = $"http://{remoteServerConfig.Server}:{remoteServerConfig.Port}";
}

// Add File Server to the services
var fileServerConfig = builder.Configuration.GetSection("NetLock_File_Server").Get<NetLock_RMM_Web_Console.Classes.File_Server.Config>();
File_Server.Hostname = fileServerConfig.Server;

if (fileServerConfig.UseSSL)
{
    File_Server.Connection_String = $"https://{fileServerConfig.Server}:{fileServerConfig.Port}";
}
else
{
    File_Server.Connection_String = $"http://{fileServerConfig.Server}:{fileServerConfig.Port}";
}

// Public override URL
if (!String.IsNullOrEmpty(publicOverrideUrl))
    Web_Console.publicOverrideUrl = publicOverrideUrl;

// Title
Web_Console.title = builder.Configuration.GetValue<string>("Webinterface:Title", "NetLock RMM");

if (Web_Console.title == "Your company name")
    Web_Console.title = "NetLock RMM"; // Default title if not set

var language = builder.Configuration["Webinterface:Language"];

// Check members portal parts
//OSSCH_START 3eb7e221-4f4f-437a-b306-765953894a92 //OSSCH_END
Console.WriteLine("---------Loader_End----------");

// Output OS
Console.WriteLine("OS: " + RuntimeInformation.OSDescription);
Console.WriteLine("Architecture: " + RuntimeInformation.OSArchitecture);
Console.WriteLine("Framework: " + RuntimeInformation.FrameworkDescription);
Console.WriteLine(Environment.NewLine);

// Output version
Console.WriteLine("NetLock RMM Web Console");
Console.WriteLine("Web Console Version: " + Application_Settings.web_console_version);
Console.WriteLine("Database Version: " + Application_Settings.db_version);
Console.WriteLine(Environment.NewLine);
Console.WriteLine("Configuration loaded from appsettings.json");
Console.WriteLine(Environment.NewLine);

// Output http port
Console.WriteLine("[Kestrel Configuration]");
Console.WriteLine($"Http: {http}");
Console.WriteLine($"Http Port: {http_port}");
Console.WriteLine($"Https: {https}");
Console.WriteLine($"Https Port: {https_port}");
Console.WriteLine($"Https (force): {https_force}");
Console.WriteLine($"Hsts: {hsts}");
Console.WriteLine($"Hsts Max Age: {hsts_max_age}");
Console.WriteLine($"Allowed IPs: {string.Join(", ", allowedIps)}");
Console.WriteLine($"Known Proxies: {string.Join(", ", knownProxies)}");

Console.WriteLine($"Custom Certificate Path: {cert_path}");
Console.WriteLine($"Custom Certificate Password: {cert_password}");
Console.WriteLine(Environment.NewLine);

// Output mysql configuration
var mysqlConfig = builder.Configuration.GetSection("MySQL").Get<Config>() ?? new Config();
MySQL.Connection_String = $"Server={mysqlConfig.Server};Port={mysqlConfig.Port};Database={mysqlConfig.Database};User={mysqlConfig.User};Password={mysqlConfig.Password};SslMode={mysqlConfig.SslMode};{mysqlConfig.AdditionalConnectionParameters}";
MySQL.Database = mysqlConfig.Database;

Console.WriteLine("[MySQL]");
Console.WriteLine($"MySQL Server: {mysqlConfig.Server}");
Console.WriteLine($"MySQL Port: {mysqlConfig.Port}");
Console.WriteLine($"MySQL Database: {mysqlConfig.Database}");
Console.WriteLine($"MySQL User: {mysqlConfig.User}");
Console.WriteLine($"MySQL Password: {mysqlConfig.Password}");
Console.WriteLine($"MySQL SSL Mode: {mysqlConfig.SslMode}");
Console.WriteLine($"MySQL Additional Parameters: {mysqlConfig.AdditionalConnectionParameters}");
Console.WriteLine(Environment.NewLine);

// Output remote server configuration
Console.WriteLine("[Remote Server]");
Console.WriteLine($"Remote Server: {remoteServerConfig.Server}");
Console.WriteLine($"Remote Port: {remoteServerConfig.Port}");
Console.WriteLine($"Remote Use SSL: {remoteServerConfig.UseSSL}");
Console.WriteLine($"Remote Connnection String: {Remote_Server.Connection_String}");
Console.WriteLine(Environment.NewLine);

// Output file server configuration
Console.WriteLine("[File Server]");
Console.WriteLine($"File Server: {fileServerConfig.Server}");
Console.WriteLine($"File Port: {fileServerConfig.Port}");
Console.WriteLine($"File Use SSL: {fileServerConfig.UseSSL}");
Console.WriteLine($"File Connnection String: {File_Server.Connection_String}");
Console.WriteLine(Environment.NewLine);

// Webinterface
Console.WriteLine("[Webinterface]");
Console.WriteLine($"Language: {language}");
Console.WriteLine($"Title: {Web_Console.title}");
Console.WriteLine($"Public Override Domain: {Web_Console.publicOverrideUrl}");
Console.WriteLine(Environment.NewLine);

// Members Portal Api
Console.WriteLine("[Members Portal Api]");
Console.WriteLine($"Api Enabled: {Members_Portal.IsApiEnabled}");
Console.WriteLine($"Api Key Override: {membersPortal.ApiKeyOverride}");
Console.WriteLine($"Cloud Enabled: {Members_Portal.IsCloudEnabled}");
Console.WriteLine($"Agent Configuration Connection String: {Web_Console.agentConfigurationConnectionString}");
Console.WriteLine($"Server Guid: {Members_Portal.ServerGuid}");
Console.WriteLine($"License Valid Until: {validUntilStr}");
Console.WriteLine($"License Package Name: {packageName}");
Console.WriteLine($"License Max Devices: {licensesMax}");
Console.WriteLine($"License Hard Limit: {licensesHardLimit}");
Console.WriteLine($"Code Signed: {Members_Portal.IsCodeSigned}");
Console.WriteLine(Environment.NewLine);

// Logging
Console.WriteLine("[Logging]");
Console.WriteLine($"Logging: {loggingEnabled}");
Console.WriteLine(Environment.NewLine);

builder.WebHost.UseKestrel(k =>
{
    IServiceProvider appServices = k.ApplicationServices;

    // Set the maximum request body size to 10 GB
    k.Limits.MaxRequestBodySize = 10L * 1024 * 1024 * 1024; // 10 GB
    
    if (https)
    {
        k.Listen(IPAddress.Any, https_port, o =>
        {
            if (String.IsNullOrEmpty(cert_password) && File.Exists(cert_path))
            {
                o.UseHttps(cert_path);
            }
            else if (!String.IsNullOrEmpty(cert_password) && File.Exists(cert_path))
            {
                o.UseHttps(cert_path, cert_password);
            }
            else
            {
                Console.WriteLine("Custom certificate path or password is not set or file does not exist. Exiting...");
                Thread.Sleep(5000);
                Environment.Exit(1);
            }
        });
    }

    if (http)
        k.Listen(IPAddress.Any, http_port);
});

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue; // In case of form
    x.MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024; // 10 GB // In case of multipart
});

// Check mysql connection
Console.WriteLine(Environment.NewLine);
Console.WriteLine("Checking MySQL connection...");

if (!await Database.Check_Connection())
{
    Console.WriteLine("MySQL connection failed. Exiting...");
    Thread.Sleep(5000);
    Environment.Exit(1);
}
else
{
    Console.WriteLine("SQL connection successful.");

    if (!await Database.Verify_Supported_SQL_Server())
    {
        Console.WriteLine("SQL Server version is not supported. We only support MySQL! Exiting...");
        Thread.Sleep(5000);
        Environment.Exit(1);
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("SQL Server version is supported.");
        Console.ResetColor();

        // Check if tables exist
        if (!await Database.Check_Table_Existing()) // Table does not exist
        {
            Console.WriteLine("Database tables do not exist. Creating tables...");
            await Database.Execute_Installation_Script(false);
            await Database.Execute_Update_Scripts();
            Console.WriteLine("Database tables created.");
        }
        else // Table exists
        {
            Console.WriteLine("Verifying & updating database structure if required.");

            // Update database
            await Database.Execute_Update_Scripts();

            await Database.Update_DB_Version();

            await Database.Fix_Settings();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Database structure okay.");
            Console.ResetColor();

            // Get api key
            if (String.IsNullOrEmpty(Members_Portal.ApiKey))
            {
                Members_Portal.ApiKey = await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Get_Api_Key();

                Console.WriteLine("Members Portal API key loaded from database: " + Members_Portal.ApiKey);
            }
            else
                await NetLock_RMM_Web_Console.Classes.Members_Portal.Handler.Set_Api_Key(Members_Portal.ApiKey);

            // Do cloud stuff
            if (Members_Portal.IsCloudEnabled)
            {
                // Enforce cloud settings
                await Database.EnforceCloudSettings();
                
                Console.WriteLine("Cloud enabled. Checking cloud connection...");
                if (await NetLock_RMM_Web_Console.Classes.Members_Portal.Handler.Check_Connection())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Cloud connection successful.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Cloud connection failed. Please check your internet connection or firewall settings.");
                    Console.ResetColor();
                }
            }
            
            // Update license info
            await NetLock_RMM_Web_Console.Classes.Members_Portal.Handler.Request_License_Info_Json(Members_Portal.ApiKey);
        }
    }
}

Console.WriteLine(Environment.NewLine);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// SSO Configuration
Console.WriteLine(Environment.NewLine);
Console.WriteLine("[SSO Configuration]");

var ssoConfig = builder.Configuration.GetSection("Authentication").Get<NetLock_RMM_Web_Console.Classes.Authentication.SsoConfig>() ?? new NetLock_RMM_Web_Console.Classes.Authentication.SsoConfig();

if (ssoConfig.Enabled)
{
    Sso.IsEnabled = true;
    
    // Check which providers are enabled
    if (ssoConfig.AzureAd?.Enabled == true)
    {
        Sso.IsAzureAdEnabled = true;
        Console.WriteLine("SSO: Azure AD (Microsoft Entra ID) enabled");
    }
    
    if (ssoConfig.Keycloak?.Enabled == true)
    {
        Sso.IsKeycloakEnabled = true;
        Console.WriteLine("SSO: Keycloak enabled");
    }
    
    if (ssoConfig.GoogleIdentity?.Enabled == true)
    {
        Sso.isGoogleIdentityEnabled = true;
        Console.WriteLine("SSO: Google Workspace / Google Identity enabled");
    }
    
    if (ssoConfig.Okta?.Enabled == true)
    {
        Sso.IsOktaEnabled = true;
        Console.WriteLine("SSO: Okta enabled");
    }
    
    if (ssoConfig.Auth0?.Enabled == true)
    {
        Sso.IsAuth0Enabled = true;
        Console.WriteLine("SSO: Auth0 enabled");
    }
    
    if (Sso.IsAzureAdEnabled || Sso.IsKeycloakEnabled || Sso.isGoogleIdentityEnabled || Sso.IsOktaEnabled || Sso.IsAuth0Enabled)
    {
        Console.WriteLine("SSO is enabled. Registering SSO authentication providers...");
        AuthProviderRegistrar.RegisterSsoProviders(builder.Services, builder.Configuration);
    }
    else
    {
        Console.WriteLine("SSO is enabled in config but no providers are configured. Using default authentication core.");
        builder.Services.AddAuthenticationCore();
    }
}
else
{
    Console.WriteLine("SSO is disabled. Using default authentication core.");
    builder.Services.AddAuthenticationCore();
}
Console.WriteLine(Environment.NewLine);

// Blazor and core services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddHttpContextAccessor();

// Register CustomAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>(sp =>
{
    var sessionStorage = sp.GetRequiredService<ProtectedSessionStorage>();
    var tokenService = sp.GetRequiredService<TokenService>();
    var httpAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new CustomAuthenticationStateProvider(sessionStorage, tokenService, httpAccessor);
});
builder.Services.AddScoped<ProtectedSessionStorage>();

// Additional services
builder.Services.AddOptions();
builder.Services.AddLocalization();
builder.Services.AddSingleton<MudBlazor.MudThemeProvider>();
builder.Services.AddMvc();


// Configure form options to increase the maximum upload file size limit to 150 MB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5L * 1024 * 1024 * 1024; // 5 GB
});

try
{
    builder.Services.AddLocalization(options => { options.ResourcesPath = "Resources"; });
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

//increase size of textarea accepted value value
builder.Services.AddServerSideBlazor().AddHubOptions(x => x.MaximumReceiveMessageSize = 102400000);

// Add background services
builder.Services.AddHostedService<NetLock_RMM_Web_Console.Classes.MySQL.AutoCleanupService>();
builder.Services.AddHostedService<NetLock_RMM_Web_Console.Classes.ScreenRecorder.AutoCleanupService>(); //disabled until remote screen control release

// Generate tokenservice secretkey
Web_Console.token_service_secret_key = Randomizer.Handler.Token(true, 32);

var app = builder.Build();

// If SSO is enabled, ensure authentication/authorization middleware are added
if (Sso.IsEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Middleware to filter the IP addresses
var knownProxiesStrings = builder.Configuration.GetSection("Kestrel:KnownProxies").Get<List<string>>() ?? new List<string>();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

bool hasValidProxy = false;

foreach (var proxyIpString in knownProxiesStrings)
{
    if (IPAddress.TryParse(proxyIpString, out var proxyIp))
    {
        forwardedHeadersOptions.KnownProxies.Add(proxyIp);
        hasValidProxy = true;
        Logging.Handler.Debug("Middleware", "KnownProxies", $"Added known proxy IP: {proxyIp}");
        Console.WriteLine($"Added known proxy IP: {proxyIp}");
    }
    else
    {
        Logging.Handler.Error("Middleware", "KnownProxies", $"'{proxyIpString}' is not a valid IP address and will be ignored.");
        Console.WriteLine($"Warning: '{proxyIpString}' is not a valid IP address and will be ignored.");
    }
}

// Check if proxy IPs were added
if (hasValidProxy)
{
    app.UseForwardedHeaders(forwardedHeadersOptions);
}
else
{
    Console.WriteLine("No valid KnownProxies found, skipping UseForwardedHeaders middleware.");
}

if (allowedIps == null || allowedIps.Count == 0)
{
    Logging.Handler.Debug("Middleware", "IP Whitelisting", "No IP addresses are whitelisted. All IPs will be allowed.");
    Console.WriteLine("No IP addresses are whitelisted. All IPs will be allowed.");
}
else
{
    Logging.Handler.Debug("Middleware", "IP Whitelisting", "IP whitelisting enabled. Whitelisted IPs: " + string.Join(", ", allowedIps));
    Console.WriteLine("IP whitelisting enabled. Whitelisted IPs: " + string.Join(", ", allowedIps));

    app.Use(async (context, next) =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();

        Logging.Handler.Debug("Middleware", "RemoteIpAddress", remoteIp);
        Logging.Handler.Debug("Middleware", "X-Forwarded-For", forwardedFor);
        Logging.Handler.Debug("Middleware", "X-Forwarded-Proto", forwardedProto);

        // If the request is forwarded, use the first IP from the X-Forwarded-For header
        if (!allowedIps.Contains(remoteIp))
        {
            Logging.Handler.Error("Middleware", "IP Whitelisting", $"IP {remoteIp} is not whitelisted.");
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Your IP is unknown.");
            return;
        }

        await next();
    });
}

// temporary static selection
if (language == "en-US")
    app.UseRequestLocalization("en-US");
else if (language == "de-DE")
{
    Web_Console.language = "de-DE";
    app.UseRequestLocalization("de-DE");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    if (hsts)
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
}

if (https_force)
{
    app.UseHttpsRedirection();
}

//app.MapBlazorHub(); // 
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

//OSSCH_START 269aeefa-6750-41e1-96c1-f2fa9a018564 //OSSCH_END

Console.WriteLine("---------Loader_End----------");

Console.WriteLine(Environment.NewLine);
Console.WriteLine("Server started.");

// SSO Challenge Endpoints
if (Sso.IsEnabled)
{
    Console.WriteLine("SSO endpoints registered:");
    
    // Azure AD / Microsoft Entra ID Endpoints
    if (Sso.IsAzureAdEnabled)
    {
        // Azure AD Challenge
        app.MapGet("/challenge/azuread", async context =>
        {
            await context.ChallengeAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme, 
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/sso-callback"
                });
        });
        
        // Azure AD Signout Callback - Handles the return from Azure AD after logout (POST)
        app.MapPost("/signout-callback-oidc", async context =>
        {
            Console.WriteLine("SSO: Signout callback received from Azure AD (POST)");
            Logging.Handler.Debug("SSO", "Signout Callback", "Returned from Azure AD logout (POST)");
            
            try
            {
                // Sign out from ALL authentication schemes to ensure complete logout
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Signout Callback Error", ex.ToString());
            }
            
            Console.WriteLine("SSO: Redirecting to login with logout flag and forcing reload");
            // Add cache control headers to force browser to reload and not use cached version
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        // Azure AD Signout Callback - Also handle GET requests (some IdPs use GET instead of POST)
        app.MapGet("/signout-callback-oidc", async context =>
        {
            Console.WriteLine("SSO: Signout callback received from Azure AD (GET)");
            Logging.Handler.Debug("SSO", "Signout Callback", "Returned from Azure AD logout (GET)");
            
            try
            {
                // Sign out from ALL authentication schemes to ensure complete logout
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Signout Callback Error", ex.ToString());
            }
            
            Console.WriteLine("SSO: Redirecting to login with logout flag and forcing reload");
            // Add cache control headers to force browser to reload and not use cached version
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        Console.WriteLine("  - /challenge/azuread (Azure AD / Microsoft Entra ID login)");
        Console.WriteLine("  - /signout-callback-oidc (Azure AD logout callback)");
    }
    
    // Keycloak Endpoints
    if (Sso.IsKeycloakEnabled)
    {
        // Keycloak Challenge
        app.MapGet("/challenge/keycloak", async context =>
        {
            await context.ChallengeAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme, 
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/sso-callback"
                });
        });
        
        // Keycloak Signout Callback (POST)
        app.MapPost("/signout-callback-keycloak", async context =>
        {
            Console.WriteLine("SSO: Keycloak signout callback received (POST)");
            Logging.Handler.Debug("SSO", "Keycloak Signout Callback", "Returned from Keycloak logout (POST)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Keycloak - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Keycloak Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Keycloak - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Keycloak Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        // Keycloak Signout Callback (GET)
        app.MapGet("/signout-callback-keycloak", async context =>
        {
            Console.WriteLine("SSO: Keycloak signout callback received (GET)");
            Logging.Handler.Debug("SSO", "Keycloak Signout Callback", "Returned from Keycloak logout (GET)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Keycloak - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Keycloak Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Keycloak - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Keycloak Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        Console.WriteLine("  - /challenge/keycloak (Keycloak login)");
        Console.WriteLine("  - /signout-callback-keycloak (Keycloak logout callback)");
    }
    
    // Google Workspace / Google Identity Endpoints
    if (Sso.isGoogleIdentityEnabled)
    {
        // Google Challenge
        app.MapGet("/challenge/google", async context =>
        {
            await context.ChallengeAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme, 
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/sso-callback"
                });
        });
        
        // Google Signout Callback (POST)
        app.MapPost("/signout-callback-google", async context =>
        {
            Console.WriteLine("SSO: Google signout callback received (POST)");
            Logging.Handler.Debug("SSO", "Google Signout Callback", "Returned from Google logout (POST)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Google - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Google Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Google - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Google Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        // Google Signout Callback (GET)
        app.MapGet("/signout-callback-google", async context =>
        {
            Console.WriteLine("SSO: Google signout callback received (GET)");
            Logging.Handler.Debug("SSO", "Google Signout Callback", "Returned from Google logout (GET)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Google - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Google Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Google - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Google Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        Console.WriteLine("  - /challenge/google (Google Workspace / Google Identity login)");
        Console.WriteLine("  - /signout-callback-google (Google logout callback)");
    }
    
    // Okta Endpoints
    if (Sso.IsOktaEnabled)
    {
        // Okta Challenge
        app.MapGet("/challenge/okta", async context =>
        {
            await context.ChallengeAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme, 
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/sso-callback"
                });
        });
        
        // Okta Signout Callback (POST)
        app.MapPost("/signout-callback-okta", async context =>
        {
            Console.WriteLine("SSO: Okta signout callback received (POST)");
            Logging.Handler.Debug("SSO", "Okta Signout Callback", "Returned from Okta logout (POST)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Okta - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Okta Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Okta - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Okta Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        // Okta Signout Callback (GET)
        app.MapGet("/signout-callback-okta", async context =>
        {
            Console.WriteLine("SSO: Okta signout callback received (GET)");
            Logging.Handler.Debug("SSO", "Okta Signout Callback", "Returned from Okta logout (GET)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Okta - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Okta Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Okta - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Okta Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        Console.WriteLine("  - /challenge/okta (Okta login)");
        Console.WriteLine("  - /signout-callback-okta (Okta logout callback)");
    }
    
    // Auth0 Endpoints
    if (Sso.IsAuth0Enabled)
    {
        // Auth0 Challenge
        app.MapGet("/challenge/auth0", async context =>
        {
            await context.ChallengeAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme, 
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/sso-callback"
                });
        });
        
        // Auth0 Signout Callback (POST)
        app.MapPost("/signout-callback-auth0", async context =>
        {
            Console.WriteLine("SSO: Auth0 signout callback received (POST)");
            Logging.Handler.Debug("SSO", "Auth0 Signout Callback", "Returned from Auth0 logout (POST)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Auth0 - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Auth0 Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Auth0 - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Auth0 Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        // Auth0 Signout Callback (GET)
        app.MapGet("/signout-callback-auth0", async context =>
        {
            Console.WriteLine("SSO: Auth0 signout callback received (GET)");
            Logging.Handler.Debug("SSO", "Auth0 Signout Callback", "Returned from Auth0 logout (GET)");
            
            try
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
                
                Console.WriteLine("SSO: Auth0 - All local authentication schemes cleared");
                Logging.Handler.Debug("SSO", "Auth0 Signout Callback", "Local sessions cleared");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Auth0 - Error clearing local sessions: {ex.Message}");
                Logging.Handler.Error("SSO", "Auth0 Signout Callback Error", ex.ToString());
            }
            
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            context.Response.Redirect("/?logout=true");
        });
        
        Console.WriteLine("  - /challenge/auth0 (Auth0 login)");
        Console.WriteLine("  - /signout-callback-auth0 (Auth0 logout callback)");
    }
    
    // Common SSO Signout Handler - Used by all providers
    if (Sso.IsAzureAdEnabled || Sso.IsKeycloakEnabled || Sso.isGoogleIdentityEnabled || Sso.IsOktaEnabled || Sso.IsAuth0Enabled)
    {
        app.MapGet("/sso-signout", async context =>
        {
            Console.WriteLine("SSO: Signout handler called");
            Logging.Handler.Debug("SSO", "Signout", "Initiating SSO logout process");
            
            try
            {
                // Check if user is authenticated
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    Console.WriteLine($"SSO: Signing out user: {context.User.Identity.Name}");
                    Logging.Handler.Debug("SSO", "Signout User", context.User.Identity.Name ?? "unknown");
                    
                    // IMPORTANT: Sign out from Cookie scheme FIRST to clear local session immediately
                    // This prevents automatic re-login when user returns
                    await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                    Console.WriteLine("SSO: Local cookie session cleared");
                    Logging.Handler.Debug("SSO", "Signout", "Local cookie session cleared");
                    
                    // Then sign out from OIDC scheme to logout from the identity provider
                    // This will redirect to the IdP logout page and then back to our app
                    await context.SignOutAsync(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme,
                        new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                        {
                            RedirectUri = "/?logout=true"
                        });
                    
                    Console.WriteLine("SSO: OIDC signout initiated, redirecting to identity provider");
                    Logging.Handler.Debug("SSO", "Signout", "OIDC signout flow started");
                }
                else
                {
                    Console.WriteLine("SSO: No authenticated user found, redirecting to login");
                    Logging.Handler.Debug("SSO", "Signout", "No authenticated user, redirecting");
                    context.Response.Redirect("/?logout=true");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Error during signout: {ex.Message}");
                Logging.Handler.Error("SSO", "Signout Error", ex.ToString());
                context.Response.Redirect("/?logout=true");
            }
        });
        
        Console.WriteLine("  - /sso-signout (Initiate SSO logout)");
    }
}

// Test endpoint
app.MapGet("/test", async context =>
{
    context.Response.StatusCode = 200;
    await context.Response.WriteAsync("ok");
});

// Members Portal Api Cloud Version Endpoints
if (Members_Portal.IsApiEnabled && Members_Portal.IsCloudEnabled)
{
    //OSSCH_START 5c023aad-e525-48fe-bc13-00db8f83b187 //OSSCH_END
}

// Start server
app.Run();
