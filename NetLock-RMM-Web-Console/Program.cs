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
using NetLock_RMM_Web_Console.Configuration;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.Features;
using NetLock_RMM_Web_Console.Components.Pages.Devices;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Get UseHttps from config
var https = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Https:Enabled");
var https_force = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Https:Force");

var hsts = builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Https:Hsts:Enabled");
var hsts_max_age = builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Hsts:MaxAge");
var letsencrypt = builder.Configuration.GetValue<bool>("LettuceEncrypt:Enabled");
var cert_path = builder.Configuration["Kestrel:Endpoint:Https:Certificate:Path"];
var cert_password = builder.Configuration["Kestrel:Endpoint:Https:Certificate:Password"];

// Add Remote_Server to the services
var remoteServerConfig = builder.Configuration.GetSection("NetLock_Remote_Server").Get<NetLock_RMM_Web_Console.Classes.Remote_Server.Config>();
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
if (fileServerConfig.UseSSL)
{
    File_Server.Connection_String = $"https://{fileServerConfig.Server}:{fileServerConfig.Port}";
}
else
{
    File_Server.Connection_String = $"http://{fileServerConfig.Server}:{fileServerConfig.Port}";
}

var language = builder.Configuration["Webinterface:Language"];

// Members Portal Api
var membersPortal = builder.Configuration.GetSection("Members_Portal_Api").Get<NetLock_RMM_Web_Console.Classes.Members_Portal.Config>();

if (membersPortal.Enabled)
    Members_Portal.api_enabled = true;

// Output OS
Console.WriteLine("OS: " + RuntimeInformation.OSDescription);
Console.WriteLine("Architecture: " + RuntimeInformation.OSArchitecture);
Console.WriteLine("Framework: " + RuntimeInformation.FrameworkDescription);
Console.WriteLine(Environment.NewLine);

// Output version
Console.WriteLine("NetLock RMM Web Console");
Console.WriteLine("Version: " + Application_Settings.version);
Console.WriteLine(Environment.NewLine);
Console.WriteLine("Configuration loaded from appsettings.json");
Console.WriteLine(Environment.NewLine);

// Output http port
Console.WriteLine("[Webserver]");
Console.WriteLine($"Http: {builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Http:Enabled")}");
Console.WriteLine($"Http Port: {builder.Configuration.GetValue<int>("Kestrel:Endpoint:Http:Port")}");
Console.WriteLine($"Https: {https}");
Console.WriteLine($"Https Port: {builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Port")}");
Console.WriteLine($"Https (force): {https_force}");
Console.WriteLine($"Hsts: {hsts}");
Console.WriteLine($"Hsts Max Age: {hsts_max_age}");
Console.WriteLine($"LetsEncrypt: {letsencrypt}");

Console.WriteLine($"Custom Certificate Path: {cert_path}");
Console.WriteLine($"Custom Certificate Password: {cert_password}");
Console.WriteLine(Environment.NewLine);

// Output mysql configuration
var mysqlConfig = builder.Configuration.GetSection("MySQL").Get<Config>();
MySQL.Connection_String = $"Server={mysqlConfig.Server};Port={mysqlConfig.Port};Database={mysqlConfig.Database};User={mysqlConfig.User};Password={mysqlConfig.Password};SslMode={mysqlConfig.SslMode};{mysqlConfig.AdditionalConnectionParameters}";
MySQL.Database = mysqlConfig.Database;

Console.WriteLine("[MySQL]");
Console.WriteLine($"MySQL Server: {mysqlConfig.Server}");
Console.WriteLine($"MySQL Port: {mysqlConfig.Port}");
Console.WriteLine($"MySQL Database: {mysqlConfig.Database}");
Console.WriteLine($"MySQL User: {mysqlConfig.User}");
Console.WriteLine($"MySQL Password: {mysqlConfig.Password}");
Console.WriteLine($"MySQL SSL Mode: {mysqlConfig.SslMode}");
Console.WriteLine($"MySQL additional parameters: {mysqlConfig.AdditionalConnectionParameters}");
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

// Output members portal configuration
Console.WriteLine("[Members Portal]");
Console.WriteLine($"Api Enabled: {Members_Portal.api_enabled}");
Console.WriteLine(Environment.NewLine);

// Language
Console.WriteLine("[Webinterface]");
Console.WriteLine($"Language: {language}");

// Output firewall status
bool microsoft_defender_firewall_status = Microsoft_Defender_Firewall.Status();

if (microsoft_defender_firewall_status && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Microsoft Defender Firewall is enabled.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Microsoft Defender Firewall is disabled. You should enable it for your own safety. NetLock adds firewall rules automatically according to your configuration.");
}

Console.ResetColor();

// Add firewall rule for HTTP
Microsoft_Defender_Firewall.Rule_Inbound(builder.Configuration.GetValue<int>("Kestrel:Endpoint:Http:Port").ToString());
Microsoft_Defender_Firewall.Rule_Outbound(builder.Configuration.GetValue<int>("Kestrel:Endpoint:Http:Port").ToString());

if (https)
{
    // Add firewall rule for HTTPS
    Microsoft_Defender_Firewall.Rule_Inbound(builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Port").ToString());
    Microsoft_Defender_Firewall.Rule_Outbound(builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Port").ToString());

    if (letsencrypt)
        builder.Services.AddLettuceEncrypt();
}

builder.WebHost.UseKestrel(k =>
{
    IServiceProvider appServices = k.ApplicationServices;

    // Set the maximum request body size to 10 GB
    k.Limits.MaxRequestBodySize = 10L * 1024 * 1024 * 1024; // 10 GB
    
    if (https)
    {
        k.Listen(IPAddress.Any, builder.Configuration.GetValue<int>("Kestrel:Endpoint:Https:Port"), o =>
        {
            if (letsencrypt)
            {
                o.UseHttps(h =>
                {
                    h.UseLettuceEncrypt(appServices);
                });
            }
            else
            {
                if (!string.IsNullOrEmpty(cert_password) && File.Exists(cert_path))
                {
                    o.UseHttps(cert_path, cert_password);
                }
                else
                {
                    Console.WriteLine("Default certificate file not found and Let's Encrypt certificate is not enabled.");
                }
            }
        });
    }

    if (builder.Configuration.GetValue<bool>("Kestrel:Endpoint:Http:Enabled"))
        k.Listen(IPAddress.Any, builder.Configuration.GetValue<int>("Kestrel:Endpoint:Http:Port"));
});

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue; // In case of form
    x.MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024; // 10 GB // In case of multipart
});

// Check mysql connection
if (!await Database.Check_Connection())
{
    Console.WriteLine("MySQL connection failed. Exiting...");
    Thread.Sleep(5000);
    Environment.Exit(1);
}
else
{
    Console.WriteLine("MySQL connection successful.");
}

// Check if database exists
if (!await Database.Check_Table_Existing())
{
    Console.WriteLine("Database tables do not exist. Creating database...");
    await Database.Execute_Installation_Script();
    Console.WriteLine("Database tables created.");
}
else
{
    Console.WriteLine("Database tables exist.");
}

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddAuthenticationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddMudServices();
builder.Services.AddOptions();
builder.Services.AddLocalization();
builder.Services.AddSingleton<MudBlazor.MudThemeProvider>();
builder.Services.AddHttpContextAccessor();
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

///increase size of textarea accepted value value
builder.Services.AddServerSideBlazor().AddHubOptions(x => x.MaximumReceiveMessageSize = 102400000);


var app = builder.Build();

// temporary static selection
if (language == "en-US")
    app.UseRequestLocalization("en-US");
else if (language == "de-DE")
    app.UseRequestLocalization("de-DE");

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

// Add timer to sync members portal license information regulary
async Task Members_Portal_Task()
{
    if (Members_Portal.api_enabled)
    {
        string api_key = await Handler.Quick_Reader("SELECT * FROM settings;", "members_portal_api_key");
        await NetLock_RMM_Web_Console.Classes.Members_Portal.Handler.Request_Membership_License_Information(api_key);
    }
}

// Wrapper for Timer
void Members_Portal_TimerCallback(object state)
{
    if (Members_Portal.api_enabled)
    {
        // Call the asynchronous method and do not block it
        _ = Members_Portal_Task();
    }
}

Timer members_portal_timer = new Timer(Members_Portal_TimerCallback, null, TimeSpan.Zero, TimeSpan.FromHours(1));

app.Run();
