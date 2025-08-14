using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NetLock_RMM_Server.Agent.Windows;
using System.Security.Principal;
using Microsoft.AspNetCore.SignalR;
using NetLock_RMM_Server.SignalR;
using System.Net;
using System;
using System.Text.Json;
using static NetLock_RMM_Server.Agent.Windows.Authentification;
using Microsoft.Extensions.DependencyInjection;
using NetLock_RMM_Server;
using NetLock_RMM_Server.Events;
using Microsoft.Extensions.Primitives;
using LettuceEncrypt;
using System.Threading;
using System.IO;
using static NetLock_RMM_Server.SignalR.CommandHub;
using Microsoft.AspNetCore.Builder;
using LLama.Common;
using LLama;
//using NetLock_RMM_Server.LLM;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using NetLock_RMM_Server.Configuration;
using System.Security.Cryptography;
using NetLock_RMM_Server.Members_Portal;
using System.Globalization;
using System.IO.Compression;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using System.Reflection;
using System.Configuration;
using NetLock_RMM_Server.Background_Services;

NetLock_RMM_Server.Configuration.Server.serverStartTime = DateTime.Now; // Set server start time

// Check directories
NetLock_RMM_Server.Setup.Directories.Check_Directories(); // Check if directories exist and create them if not

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
var letsencrypt = builder.Configuration.GetValue<bool>("LettuceEncrypt:Enabled", true);
var letsencrypt_password = builder.Configuration.GetValue<string>("LettuceEncrypt:CertificateStoredPfxPassword", String.Empty);
var cert_path = builder.Configuration.GetValue<string>("Kestrel:Endpoint:Https:Certificate:Path", String.Empty);
var cert_password = builder.Configuration.GetValue<string>("Kestrel:Endpoint:Https:Certificate:Password", String.Empty);
var isRunningInDocker = builder.Configuration.GetValue<bool>("Environment:Docker", true);
var loggingEnabled = builder.Configuration.GetValue<bool>("Logging:Custom:Enabled", true);

Server.isDocker = isRunningInDocker;
Server.loggingEnabled = loggingEnabled;

var role_comm = builder.Configuration.GetValue<bool>("Kestrel:Roles:Comm", true);
var role_update = builder.Configuration.GetValue<bool>("Kestrel:Roles:Update", true);
var role_trust = builder.Configuration.GetValue<bool>("Kestrel:Roles:Trust", true);
var role_remote = builder.Configuration.GetValue<bool>("Kestrel:Roles:Remote", true);
var role_notification = builder.Configuration.GetValue<bool>("Kestrel:Roles:Notification", true);
var role_file = builder.Configuration.GetValue<bool>("Kestrel:Roles:File", true);
var role_llm = builder.Configuration.GetValue<bool>("Kestrel:Roles:LLM", true);

Roles.Comm = role_comm;
Roles.Update = role_update;
Roles.Trust = role_trust;
Roles.Remote = role_remote;
Roles.Notification = role_notification;
Roles.File = role_file;
Roles.LLM = role_llm;

// Members Portal Api
var membersPortal = builder.Configuration.GetSection("Members_Portal_Api").Get<NetLock_RMM_Server.Members_Portal.Config>() ?? new NetLock_RMM_Server.Members_Portal.Config();

if (membersPortal.Enabled)
{
    Members_Portal.api_enabled = true;
    Members_Portal.api_key = membersPortal.ApiKeyOverride ?? String.Empty;
}

// Output OS
Console.WriteLine("OS: " + RuntimeInformation.OSDescription);
Console.WriteLine("Architecture: " + RuntimeInformation.OSArchitecture);
Console.WriteLine("Framework: " + RuntimeInformation.FrameworkDescription);
Console.WriteLine("Server started at: " + NetLock_RMM_Server.Configuration.Server.serverStartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
Console.WriteLine(Environment.NewLine);

// Output version
Console.WriteLine("NetLock RMM Server");
Console.WriteLine("Version: " + Application_Settings.server_version);
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
Console.WriteLine($"LetsEncrypt: {letsencrypt}");

Console.WriteLine($"Custom Certificate Path: {cert_path}");
Console.WriteLine($"Custom Certificate Password: {cert_password}");
Console.WriteLine(Environment.NewLine);

// Output mysql configuration
var mysqlConfig = builder.Configuration.GetSection("MySQL").Get<NetLock_RMM_Server.MySQL.Config>() ?? new NetLock_RMM_Server.MySQL.Config();
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

// Output kestrel configuration
Console.WriteLine($"Server role (comm): {role_comm}");
Console.WriteLine($"Server role (update): {role_update}");
Console.WriteLine($"Server role (trust): {role_trust}");
Console.WriteLine($"Server role (remote): {role_remote}");
Console.WriteLine($"Server role (notification): {role_notification}");
Console.WriteLine($"Server role (file): {role_file}");
Console.WriteLine(Environment.NewLine);

// Output members portal configuration
Console.WriteLine("[Members Portal]");
Console.WriteLine($"Api Enabled: {Members_Portal.api_enabled}");

if (!String.IsNullOrEmpty(Members_Portal.api_key))
    Console.WriteLine($"Api Key Override: {membersPortal.ApiKeyOverride}");

Console.WriteLine(Environment.NewLine);

// Logging
Console.WriteLine("[Logging]");
Console.WriteLine($"Logging: {loggingEnabled}");
Console.WriteLine(Environment.NewLine);

// Environment
Console.WriteLine("[Environment]");
Console.WriteLine($"Running under Docker: {isRunningInDocker}");

// Output firewall status
Console.WriteLine(Environment.NewLine);
Console.WriteLine("[Firewall Status]");
bool microsoft_defender_firewall_status = NetLock_RMM_Server.Microsoft_Defender_Firewall.Handler.Status();

if (microsoft_defender_firewall_status && OperatingSystem.IsWindows())
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Microsoft Defender Firewall is enabled.");
}
else if (!microsoft_defender_firewall_status && OperatingSystem.IsWindows())
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Microsoft Defender Firewall is disabled. You should enable it for your own safety. NetLock adds firewall rules automatically according to your configuration.");
}

Console.ResetColor();

// Add firewall rule for HTTP
NetLock_RMM_Server.Microsoft_Defender_Firewall.Handler.Rule_Inbound(http_port.ToString());
NetLock_RMM_Server.Microsoft_Defender_Firewall.Handler.Rule_Outbound(http_port.ToString());

if (https)
{
    // Add firewall rule for HTTPS
    NetLock_RMM_Server.Microsoft_Defender_Firewall.Handler.Rule_Inbound(https_port.ToString());
    NetLock_RMM_Server.Microsoft_Defender_Firewall.Handler.Rule_Outbound(https_port.ToString());

    if (letsencrypt)
        builder.Services.AddLettuceEncrypt().PersistDataToDirectory(new DirectoryInfo(Application_Paths.lettuceencrypt_persistent_data_dir), letsencrypt_password);
}

// Configure Kestrel server options
builder.WebHost.UseKestrel(k =>
{
    IServiceProvider appServices = k.ApplicationServices;

    // Set the maximum request body size to 10 gb
    k.Limits.MaxRequestBodySize = 10L * 1024 * 1024 * 1024; // 10 GB
    
    if (https)
    {
        k.Listen(IPAddress.Any, https_port, o =>
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
            }
        });
    }

    // Check if HTTP is enabled
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

if (!await NetLock_RMM_Server.MySQL.Handler.Check_Connection())
{
    Console.WriteLine("MySQL connection failed. Exiting...");
    Thread.Sleep(5000);
    Environment.Exit(1);
}
else
{
    Console.WriteLine("MySQL connection successful.");

    if (!await NetLock_RMM_Server.MySQL.Handler.Verify_Supported_SQL_Server())
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
    }

    await NetLock_RMM_Server.MySQL.Handler.Update_Server_Information();

    // Get api key
    if (String.IsNullOrEmpty(Members_Portal.api_key))
    {
        Members_Portal.api_key = await NetLock_RMM_Server.MySQL.Handler.Get_Members_Portal_Api_Key();

        Console.WriteLine("Members Portal API key loaded from database: " + Members_Portal.api_key);
    }
}

Console.WriteLine(Environment.NewLine);

// Check Package Status and Request Packages from Members Portal API if roles apply, api enabled and API key is set
if (Roles.Update || Roles.Trust)
{
    if (Members_Portal.api_enabled)
    {
        //OSSCH_START c315f156-62c8-49a5-9c33-9f50f3684f8a //OSSCH_END
        Console.WriteLine("----------------------------------------");
    }
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMvc();
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400000; // Increase maximum message size to 100 MB
});


// Add the LLaMa model service as a singleton. Currently disabled because in testings using ANY llm was just to CPU intensive. As most servers dont have a GPU, implementation needs to be postboned to a unknown time. Might find a solution in future
//builder.Services.AddSingleton<LLaMaService>();

// Register background service
builder.Services.AddHostedService<Events_Notification_Service>();
builder.Services.AddHostedService<Server_Information_Service>();
builder.Services.AddHostedService<Members_Portal_License_Service>();
builder.Services.AddHostedService<UpdateStateMonitoringService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (hsts)
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (https_force)
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// Only use the middleware for the commandHub, to verify the signalR connection
app.UseWhen(context => context.Request.Path.StartsWithSegments("/commandHub"), appBuilder =>
{
    appBuilder.UseMiddleware<JsonAuthMiddleware>();
});

app.MapHub<CommandHub>("/commandHub");

// Initialisiere das Singleton mit dem HubContext
var hubContext = app.Services.GetService<IHubContext<CommandHub>>();
CommandHubSingleton.Instance.Initialize(hubContext);

//API URLs*

// Test endpoint
app.MapGet("/test", async context =>
{
    context.Response.StatusCode = 200;
    await context.Response.WriteAsync("ok");
});

//Check Version
if (role_comm)
{
    app.MapPost("/Agent/Windows/Check_Version", async context =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Check_Version", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString();

            // Verify package guid
            bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

            if (hasPackageGuid == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

                if (package_guid_status == false)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }
            }

            // Read the JSON data
            string json;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Check the version of the device
            string version_status = await Version_Handler.Check_Version(json);

            // Return the device status
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(version_status);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Check_Version", ex.ToString());
            await context.Response.WriteAsync("Invalid request.");
        }
    });
}

if (role_comm)
{
    //Verify Device
    app.MapPost("/Agent/Windows/Verify_Device", async context =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Verify_Device", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address from the X-Forwarded-For header
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

            // Verify package guid
            bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

            if (hasPackageGuid == false)
            {
                Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Verify_Device", "No guid provided. Unauthorized.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

                if (package_guid_status == false)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }
            }

            // Read the JSON data
            string json;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Verify the device
            string device_status = await Authentification.Verify_Device(json, ip_address_external, true);

            await context.Response.WriteAsync(device_status);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Verify_Device", ex.ToString());

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Invalid request.");
        }
    });
}

if (role_comm)
{
    //Update device information
    app.MapPost("/Agent/Windows/Update_Device_Information", async context =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Update_Device_Information", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address from the X-Forwarded-For header
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

            // Verify package guid
            bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

            if (hasPackageGuid == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

                if (package_guid_status == false)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }
            }

            // Read the JSON data
            string json;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Verify the device
            string device_status = await Authentification.Verify_Device(json, ip_address_external, true);

            // Check if the device is authorized, synced or not synced. If so, update the device information
            if (device_status == "authorized" || device_status == "synced" || device_status == "not_synced")
            {
                await Device_Handler.Update_Device_Information(json);
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 403;
            }

            await context.Response.WriteAsync(device_status);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Update_Device_Information", ex.Message);

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Invalid request.");
        }
    });
}

if (role_comm)
{
    //Insert events
    app.MapPost("/Agent/Windows/Events", async context =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Events", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address from the X-Forwarded-For header
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

            // Verify package guid
            bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

            if (hasPackageGuid == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

                if (package_guid_status == false)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }
            }

            // Read the JSON data
            string json;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Verify the device
            string device_status = await Authentification.Verify_Device(json, ip_address_external, false);

            // Check if the device is authorized. If so, consume the events
            if (device_status == "authorized" || device_status == "synced" || device_status == "not_synced")
            {
                device_status = await Event_Handler.Consume(json);
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 403;
            }

            await context.Response.WriteAsync(device_status);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Events", ex.Message);

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Invalid request.");
        }
    });
}

if (role_comm)
{
    //Get policy
    app.MapPost("/Agent/Windows/Policy", async context =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Policy", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address from the X-Forwarded-For header
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

            // Verify package guid
            bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

            if (hasPackageGuid == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

                if (package_guid_status == false)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }
            }

            // Read the JSON data
            string json;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Verify the device
            string device_status = await Authentification.Verify_Device(json, ip_address_external, true);

            string device_policy_json = string.Empty;

            // Check if the device is authorized, synced, or not synced. If so, get the policy
            if (device_status == "authorized" || device_status == "synced" || device_status == "not_synced")
            {
                device_policy_json = await Policy_Handler.Get_Policy(json, ip_address_external);
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(device_policy_json);
            }
            else // If the device is not authorized, return the device status as unauthorized
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(device_status);
            }
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Policy", ex.ToString());

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Invalid request.");
        }
    });
}

//Remote Command: Will be used in later development
/*app.MapPost("/Agent/Windows/Remote/Command", async (HttpContext context, IHubContext<CommandHub> hubContext) =>
{
    try
    {
        Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Remote/Command", "Request received.");

        // Add headers
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

        // Get the remote IP address
        string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString();

        // Verify package guid
        bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid);

        if (hasPackageGuid == false)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized.");
            return;
        }
        else
        {
            bool package_guid_status = await Helper.Verify_Package_Guid(package_guid);

            if (package_guid_status == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
        }

        string api_key = string.Empty;

        // Read the JSON data
        string json;
        using (StreamReader reader = new StreamReader(context.Request.Body))
        {
            json = await reader.ReadToEndAsync() ?? string.Empty;
        }

        bool api_key_status = await NetLock_RMM_Server.SignalR.Webconsole.Handler.Verify_Api_Key(json);

        if (api_key_status == false)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid api key.");
            return;
        }

        // Get the command
        string command = await NetLock_RMM_Server.SignalR.Webconsole.Handler.Get_Command(json);

        // Get list of all connected clients
        var clients = ConnectionManager.Instance.ClientConnections;
        
        foreach (var client in clients)
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Remote/Command", "Client: " + client.Key + " - " + client.Value);

        // Check if the command is "sync_all" that means all devices should sync with the server
        if (command == "sync_all")
        {
            await hubContext.Clients.All.SendAsync("ReceiveCommand", "sync"); // Send command to all clients
        }

        // Return the device status
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("ok");
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Remote/Command", ex.Message);
        await context.Response.WriteAsync("Invalid request.");
    }
}).WithName("Swagger5").WithOpenApi();
*/

if (role_file)
{
    // File download public
    app.MapGet("/public/downloads/{fileName}", async context =>
    {
        try
        {
            Logging.Handler.Debug("/public/downloads", "Request received.", "");

            var fileName = (string)context.Request.RouteValues["fileName"];
            var downloadPath = Application_Paths._public_downloads_user + "\\" + fileName;

            if (!File.Exists(downloadPath))
            {
                Logging.Handler.Error("GET Request Mapping", "/public_download", "File not found: " + downloadPath);
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("File not found.");
                return;
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(downloadPath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            context.Response.ContentType = "application/octet-stream";
            context.Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");
            await memory.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("GET Request Mapping", "/public_download", ex.ToString());

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An error occurred while downloading the file.");
        }
    });
}

// NetLock admin files, get index
if (role_file)
{
    app.MapPost("/admin/files/index/{path}", async (HttpContext context, string path) =>
    {
        try
        {
            // Check whether the path is null or empty
            if (String.IsNullOrWhiteSpace(path))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request.");
                return;
            }

            // Handle the special base path
            if (path.Equals("base1337", StringComparison.OrdinalIgnoreCase))
            {
                path = String.Empty;
            }
            else
            {
                // URL decoding and removal of possible unauthorised characters
                path = Uri.UnescapeDataString(path);

                // Prevent path traversal attacks by normalising the path
                path = Path.GetFullPath(Path.Combine(Application_Paths._private_files, path));

                if (!path.StartsWith(Application_Paths._private_files))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid path.");
                    return;
                }
            }

            Logging.Handler.Debug("/admin/files", "Request received.", path);

            // Add security header
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

            // Determine external IP address (if available)
            string ipAddressExternal = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue)
                ? headerValue.ToString()
                : context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Verify API key
            if (!context.Request.Headers.TryGetValue("x-api-key", out StringValues apiKey) || !await NetLock_RMM_Server.Files.Handler.Verify_Api_Key(apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            // Check directory
            var fullPath = Path.Combine(Application_Paths._private_files, path);

            if (!Directory.Exists(fullPath))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Directory not found.");
                return;
            }

            // Retrieve directory contents
            var directoryTree = await Helper.IO.Get_Directory_Index(fullPath);

            //  Create json (directoryTree) & Application_Paths._private_files
            var jsonObject = new
            {
                index = directoryTree,
                server_path = Application_Paths._private_files
            };

            // Convert the object into a JSON string
            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
            Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "json", json);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("/admin/files/index", "General error", ex.ToString());
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An error occurred while processing the request.");
        }
    });
}

// NetLock admin files command
if (role_file)
{
    app.MapPost("/admin/files/command", async context =>
    {
        try
        {
            Logging.Handler.Debug("/admin/files/command", "Request received.", "");

            // Add security headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

            // Verify API key
            bool hasApiKey = context.Request.Headers.TryGetValue("x-api-key", out StringValues files_api_key);
            if (!hasApiKey || !await NetLock_RMM_Server.Files.Handler.Verify_Api_Key(files_api_key))
            {
                Logging.Handler.Debug("/admin/files/command", "Missing or invalid API key.", "");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            // Deserializing the JSON data (command, path)
            string json;

            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            await NetLock_RMM_Server.Files.Handler.Command(json);

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("executed");
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("/admin/files/command", "General error", ex.ToString());
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("1"); // something went wrong
        }
    });
}

// NetLock admin files, upload
if (role_file)
{
    app.MapPost("/admin/files/upload/{path}", async (HttpContext context, string path) =>
    {
        try
        {
            Logging.Handler.Debug("/admin/files/upload", "Request received.", path);

            // Add security headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

            // Verify API key
            bool hasApiKey = context.Request.Headers.TryGetValue("x-api-key", out StringValues files_api_key);

            bool ApiKeyValid = await NetLock_RMM_Server.Files.Handler.Verify_Api_Key(files_api_key);

            if (!hasApiKey || !ApiKeyValid)
            {
                Logging.Handler.Debug("/admin/files/upload", "Missing or invalid API key.", "");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            // Query-String-Parameter extrahieren
            var tenant_guid = context.Request.Query["tenant_guid"].ToString();
            var location_guid = context.Request.Query["location_guid"].ToString();
            var device_name = context.Request.Query["device_name"].ToString();

            // Check if the request contains a file
            if (!context.Request.HasFormContentType)
            {
                Logging.Handler.Debug("/admin/files/upload", "Invalid request: No form content type.", "");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request. No file uploaded #1.");
                return;
            }

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                Logging.Handler.Debug("/admin/files/upload", "Invalid request: No file found in the form.", "");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request. No file uploaded #2.");
                return;
            }

            // Decode the URL-encoded path and sanitize
            if (string.IsNullOrEmpty(path) || path.Equals("base1337", StringComparison.OrdinalIgnoreCase))
            {
                path = string.Empty;
            }
            else
            {
                path = Uri.UnescapeDataString(path);
            }

            // Sanitize the path to prevent directory traversal attacks
            string safePath = Path.GetFullPath(Path.Combine(Application_Paths._private_files, path))
                .Replace('\\', '/').TrimEnd('/');

            // Normalize the allowed base path
            string allowedPath = Path.GetFullPath(Application_Paths._private_files)
                .Replace('\\', '/').TrimEnd('/');

            // Log for debugging
            Logging.Handler.Debug("/admin/files/upload", "Allowed Path", allowedPath);
            Logging.Handler.Debug("/admin/files/upload", "Sanitized Path", safePath);

            // Check if the sanitized path starts with the allowed base path
            if (!safePath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))
            {
                Logging.Handler.Debug("/admin/files/upload", "Invalid path: Outside allowed directory.", "");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid path.");
                return;
            }

            // Ensure the upload directory exists
            string directoryPath = Path.GetDirectoryName(safePath);
            if (!Directory.Exists(directoryPath))
            {
                Logging.Handler.Debug("/admin/files/upload", "Creating directory: " + directoryPath, "");
                Directory.CreateDirectory(directoryPath);
            }

            Logging.Handler.Debug("/admin/files/upload", "Uploading file: " + file.FileName, "");

            // Set the file path
            var filePath = Path.Combine(directoryPath, file.FileName);
            Logging.Handler.Debug("/admin/files/upload", "File Path", filePath);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Logging.Handler.Debug("/admin/files/upload", "File uploaded successfully: " + file.FileName, "");

            // Register the file with the correct directory path (excluding file name)
            string register_json = await NetLock_RMM_Server.Files.Handler.Register_File(filePath, tenant_guid, location_guid, device_name);

            context.Response.StatusCode = 200;

            // Send back info json if api key is valid
            if (hasApiKey && ApiKeyValid)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(register_json);
            }
            else // If the api key is invalid, just send a simple response
            {
                await context.Response.WriteAsync("uploaded");
            }
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("/admin/files/upload", "General error", ex.ToString());
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("1"); // something went wrong
        }
    });
}

// NetLock admin files, download
if (role_file)
{
    app.MapGet("/admin/files/download", async (HttpContext context) =>
    {
        try
        {
            Logging.Handler.Debug("/admin/files/download", "Request received.", "");

            // Add security headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

            // Get api key | is not required
            bool hasApiKey = context.Request.Headers.TryGetValue("x-api-key", out StringValues files_api_key);

            // Query parameters
            string guid = context.Request.Query["guid"].ToString();
            string password = context.Request.Query["password"].ToString();

            // Get guid
            guid = Uri.UnescapeDataString(guid);

            // Handle the case when password is null or empty
            password = password != null ? Uri.UnescapeDataString(password) : string.Empty;

            bool hasAccess = await NetLock_RMM_Server.Files.Handler.Verify_File_Access(guid, password, files_api_key); // api key is not required

            if (!hasAccess)
            {
                Logging.Handler.Debug("/admin/files/download", "Unauthorized.", "");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            string file_path = await NetLock_RMM_Server.Files.Handler.Get_File_Path_By_GUID(guid);
            string server_path = Path.Combine(Application_Paths._private_files_admin_db_friendly, file_path);

            string file_name = Path.GetFileName(server_path);

            using (var fileStream = new FileStream(server_path, FileMode.Open, FileAccess.Read))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/octet-stream";
                context.Response.Headers.Add("Content-Disposition", $"attachment; filename={file_name}");

                // Stream directly to the Response.body
                await fileStream.CopyToAsync(context.Response.Body);
            }
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("/admin/files/download", "General error", ex.ToString());
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("1"); // something went wrong
        }
    });
}

// NetLock admin files device download
app.MapGet("/admin/files/download/device", async (HttpContext context) =>
{
    try
    {
        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Request received.");

        // Add headers
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

        // Get the remote IP address from the X-Forwarded-For header
        string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

        // Verify package guid
        bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "hasGuid: " + hasPackageGuid.ToString());
        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Package guid: " + package_guid.ToString());

        if (hasPackageGuid == false)
        {
            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "No guid provided. Unauthorized.");

            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized.");
            return;
        }
        else
        {
            bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Package guid status: " + package_guid_status.ToString());

            if (package_guid_status == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
        }

        // Query parameters
        string guid = context.Request.Query["guid"].ToString();
        string tenant_guid = context.Request.Query["tenant_guid"].ToString();
        string location_guid = context.Request.Query["location_guid"].ToString();
        string device_name = context.Request.Query["device_name"].ToString();
        string access_key = context.Request.Query["access_key"].ToString();
        string hwid = context.Request.Query["hwid"].ToString();

        if (String.IsNullOrEmpty(guid) || String.IsNullOrEmpty(tenant_guid) || String.IsNullOrEmpty(location_guid) || String.IsNullOrEmpty(device_name) || String.IsNullOrEmpty(access_key) || String.IsNullOrEmpty(hwid))
        {
            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Invalid request.");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid request.");
            return;
        }

        // Build a device identity JSON object with nested "device_identity" object
        string device_identity_json = "{ \"device_identity\": { " +
                                      "\"tenant_guid\": \"" + tenant_guid + "\"," +
                                      "\"location_guid\": \"" + location_guid + "\"," +
                                      "\"device_name\": \"" + device_name + "\"," +
                                      "\"access_key\": \"" + access_key + "\"," +
                                      "\"hwid\": \"" + hwid + "\"" +
                                      "} }";

        // Verify the device
        string device_status = await Authentification.Verify_Device(device_identity_json, ip_address_external, false);

        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Device status: " + device_status);

        // Check if the device is authorized, synced, or not synced. If so, get the file from the database
        if (device_status == "authorized" || device_status == "synced" || device_status == "not_synced")
        {
            // Get the file path by GUID
            bool file_access = await NetLock_RMM_Server.Files.Handler.Verify_Device_File_Access(tenant_guid, location_guid, device_name, guid);

            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "File access: " + file_access.ToString());

            if (file_access == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                string file_path = await NetLock_RMM_Server.Files.Handler.Get_File_Path_By_GUID(guid);
                string server_path = Path.Combine(Application_Paths._private_files_admin_db_friendly, file_path);

                Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Server path: " + server_path);

                if (!File.Exists(server_path))
                {
                    Logging.Handler.Debug("/admin/files/download/device", "File not found", server_path);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("File not found.");
                    return;
                }

                string file_name = Path.GetFileName(server_path);

                Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "File name: " + file_name);

                using (var fileStream = new FileStream(server_path, FileMode.Open, FileAccess.Read))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.Headers.Add("Content-Disposition", $"attachment; filename={file_name}");

                    // Stream directly to the Response.body
                    await fileStream.CopyToAsync(context.Response.Body);
                }
            }
        }
        else // If the device is not authorized, return the device status as unauthorized
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(device_status);
        }
    }
    catch (Exception ex)
    {
        Logging.Handler.Error("/admin/files/download/device", "General error", ex.ToString());

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An error occurred while downloading the file.");
    }
});

// NetLock admin files device upload
app.MapPost("/admin/files/upload/device", async (HttpContext context) =>
{
    try
    {
        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Request received.");

        // Add headers
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

        // Get the remote IP address from the X-Forwarded-For header
        string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

        // Verify package guid
        bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "hasGuid: " + hasPackageGuid.ToString());
        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Package guid: " + package_guid.ToString());

        if (hasPackageGuid == false)
        {
            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "No guid provided. Unauthorized.");

            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized.");
            return;
        }
        else
        {
            bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Package guid status: " + package_guid_status.ToString());

            if (package_guid_status == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
        }

        // Query parameters
        string tenant_guid = context.Request.Query["tenant_guid"].ToString();
        string location_guid = context.Request.Query["location_guid"].ToString();
        string device_name = context.Request.Query["device_name"].ToString();
        string access_key = context.Request.Query["access_key"].ToString();
        string hwid = context.Request.Query["hwid"].ToString();

        if (String.IsNullOrEmpty(tenant_guid) || String.IsNullOrEmpty(location_guid) || String.IsNullOrEmpty(device_name) || String.IsNullOrEmpty(access_key) || String.IsNullOrEmpty(hwid))
        {
            Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Invalid request.");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid request.");
            return;
        }

        // Build a device identity JSON object with nested "device_identity" object
        string device_identity_json = "{ \"device_identity\": { " +
                                      "\"tenant_guid\": \"" + tenant_guid + "\"," +
                                      "\"location_guid\": \"" + location_guid + "\"," +
                                      "\"device_name\": \"" + device_name + "\"," +
                                      "\"access_key\": \"" + access_key + "\"," +
                                      "\"hwid\": \"" + hwid + "\"" +
                                      "} }";

        // Verify the device
        string device_status = await Authentification.Verify_Device(device_identity_json, ip_address_external, false);

        Logging.Handler.Debug("Get Request Mapping", "/admin/files/download/device", "Device status: " + device_status);

        // Check if the device is authorized, synced, or not synced. If so, get the file from the database
        if (device_status == "authorized" || device_status == "synced" || device_status == "not_synced")
        {
            // Check if the request contains a file
            if (!context.Request.HasFormContentType)
            {
                Logging.Handler.Debug("/admin/files/upload/device", "Invalid request: No form content type.", "");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request. No file uploaded #1.");
                return;
            }

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                Logging.Handler.Debug("/admin/files/upload/device", "Invalid request: No file found in the form.", "");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request. No file uploaded #2.");
                return;
            }

            // Ensure the upload directory exists
            string directoryPath = Path.Combine(Application_Paths._private_files, "devices", tenant_guid, location_guid, device_name, "downloaded");
            if (!Directory.Exists(directoryPath))
            {
                Logging.Handler.Debug("/admin/files/upload/device", "Creating directory: " + directoryPath, "");
                Directory.CreateDirectory(directoryPath);
            }

            Logging.Handler.Debug("/admin/files/upload/device", "Uploading file: " + file.FileName, "");

            // Set the file path
            var filePath = Path.Combine(directoryPath, file.FileName);
            Logging.Handler.Debug("/admin/files/upload/device", "File Path", filePath);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Logging.Handler.Debug("/admin/files/upload/device", "File uploaded successfully: " + file.FileName, "");

            // Register the file with the correct directory path (excluding file name)
            string register_json = await NetLock_RMM_Server.Files.Handler.Register_File(filePath, tenant_guid, location_guid, device_name);

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(register_json);
        }
        else // If the device is not authorized, return the device status as unauthorized
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(device_status);
        }
    }
    catch (Exception ex)
    {
        Logging.Handler.Error("/admin/files/download/device", "General error", ex.ToString());

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An error occurred while downloading the file.");
    }
});


// NetLock files download private - GUID, used for update server & trust server
if (role_update || role_trust)
{
    //OSSCH_START ab7d3f12-ec94-41f1-a5e5-0208ec95593d //OSSCH_END
}

/*
if (role_llm)
{
    app.MapPost("/llm/chat/admin", async (HttpContext context, LLaMaService llamaService) =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/llm/chat/admin", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address from the X-Forwarded-For header
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue)
                ? headerValue.ToString()
                : context.Connection.RemoteIpAddress?.ToString();

            // Verify API key
            if (!context.Request.Headers.TryGetValue("x-api-key", out var apiKey) ||
                !await NetLock_RMM_Server.Files.Handler.Verify_Api_Key(apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            // Read the JSON data from the request body
            string json;
            using (var reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            Console.WriteLine("Request: " + json);

            // Handle LLaMa service response
            var response = await llamaService.GetResponseAsync(json);

            // Send response
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(response);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("POST Request Mapping", "/llm/chat/admin", ex.ToString());

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal Server Error.");
        }
    });
}
*/

// Temporary endpoint to bridge the remote control, due to issues related with signalr causing instability on client side

if (role_remote)
{
    //Get policy
    app.MapPost("/Agent/Windows/Remote/Command", async (HttpContext context, IHubContext<CommandHub> hubContext) =>
    {
        try
        {
            Logging.Handler.Debug("POST Request Mapping", "/Agent/Windows/Remote/Command", "Request received.");

            // Add headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'"); // protect against XSS 

            // Get the remote IP address from the X-Forwarded-For header
            string ip_address_external = context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue) ? headerValue.ToString() : context.Connection.RemoteIpAddress.ToString();

            // Verify package guid
            bool hasPackageGuid = context.Request.Headers.TryGetValue("Package_Guid", out StringValues package_guid) || context.Request.Headers.TryGetValue("Package-Guid", out package_guid);

            if (hasPackageGuid == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }
            else
            {
                bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(package_guid);

                if (package_guid_status == false)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }
            }

            // Read the JSON data
            string json;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                json = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Verify the device
            string device_status = await Authentification.Verify_Device(json, ip_address_external, true);

            // Check if the device is authorized, synced, or not synced. If so, get the policy
            if (device_status == "authorized" || device_status == "synced" || device_status == "not_synced")
            {
                string responseId = string.Empty;
                string result = string.Empty;

                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    // Get the root element
                    JsonElement root = document.RootElement;

                    // Access the "remote_control" section
                    JsonElement remoteControlElement = root.GetProperty("remote_control");

                    // Extract "response_id" and "result"
                    responseId = remoteControlElement.GetProperty("response_id").GetString();
                    result = remoteControlElement.GetProperty("result").GetString();
                }

                string admin_identity_info_json = CommandHubSingleton.Instance.GetAdminIdentity(responseId);

                string admin_client_id = String.Empty;
                string admin_username = String.Empty;
                string device_id = String.Empty;
                string command = String.Empty;
                int type = 0;
                int file_browser_command = 0;

                // Deserialisierung des gesamten JSON-Strings
                using (JsonDocument document = JsonDocument.Parse(admin_identity_info_json))
                {
                    // Get the admin client ID from the JSON
                    JsonElement admin_client_id_element = document.RootElement.GetProperty("admin_client_id");
                    admin_client_id = admin_client_id_element.ToString();
                }

                await CommandHubSingleton.Instance.HubContext.Clients.Client(admin_client_id).SendAsync("ReceiveClientResponseRemoteControlScreenCapture", result);

                // Remove the response ID from the dictionary
                CommandHubSingleton.Instance.RemoveAdminCommand(responseId);

                // Return the device status
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("ok");
            }
            else // If the device is not authorized, return the device status as unauthorized
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(device_status);
            }
        }
        catch (Exception ex)
        { 
            Logging.Handler.Error("POST Request Mapping", "/Agent/Windows/Remote/Command", ex.ToString());

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Invalid request.");
        }
    });
}

// Admin command: Create custom installer
// NetLock admin files, download
if (role_file)
{
    app.MapPost("/admin/create_installer", async (HttpContext context) =>
    {
        try
        {
            Logging.Handler.Debug("/admin/create_installer", "Request received.", "");

            // Add security headers
            context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

            // Get api key | is not required
            bool hasApiKey = context.Request.Headers.TryGetValue("x-api-key", out StringValues files_api_key);

            // Verify API key
            if (!hasApiKey || !await NetLock_RMM_Server.Files.Handler.Verify_Api_Key(files_api_key))
            {
                Logging.Handler.Debug("/admin/create_installer", "Missing or invalid API key.", "");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            // Read the JSON data
            string body;
            using (StreamReader reader = new StreamReader(context.Request.Body))
            {
                body = await reader.ReadToEndAsync() ?? string.Empty;
            }

            // Extract the name and the JSON data
            // Deserialisierung des gesamten JSON-Strings
            string name = String.Empty;
            string server_config = String.Empty;

            using (JsonDocument document = JsonDocument.Parse(body))
            {
                JsonElement name_element = document.RootElement.GetProperty("name");
                name = name_element.ToString();

                JsonElement json_element = document.RootElement.GetProperty("server_config");
                server_config = json_element.ToString();
            }

            // Create installer file
            string result = await NetLock_RMM_Server.Files.Handler.Create_Custom_Installer(name, server_config);

            // Return the result as a JSON string
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(result);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("/admin/create_installer", "General error", ex.ToString());
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("1"); // something went wrong
        }
    });
}

// Add a middleware to handle exceptions globally and return a 500 status code with a message to the client in case of an unexpected error
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An unexpected error occurred.");
    });
});

Console.WriteLine(Environment.NewLine);
Console.WriteLine("Server started.");

//Start server
app.Run();

