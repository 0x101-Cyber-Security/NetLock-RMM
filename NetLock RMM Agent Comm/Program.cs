using _x101.HWID_System;
using Global.Helper;
using NetLock_RMM_Agent_Comm;
using System.Net;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Starting NetLock RMM Comm Agent");

var builder = Host.CreateApplicationBuilder(args);

foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    foreach (var type in assembly.GetTypes())
    {
        if (typeof(FileSystemWatcher).IsAssignableFrom(type) || type.Name.Contains("FileSystemWatcher"))
        {
            Console.WriteLine($"Gefunden in: {assembly.FullName}, Typ: {type.FullName}");
        }
    }
}

// Check if debug mode
if (Logging.Check_Debug_Mode()) // debug_mode
{
    Console.WriteLine("Debug mode enabled");
    Global.Configuration.Agent.debug_mode = true;
}
else
    Console.WriteLine("Debug mode disabled");

// Thats dev stuff
//Global.Configuration.Agent.debug_mode = true;

// Read server_config.json
if (Convert.ToBoolean(Global.Initialization.Server_Config.Ssl())) // ssl
{
    Global.Configuration.Agent.ssl = true;
    Global.Configuration.Agent.http_https = "https://";
}
else
{
    Global.Configuration.Agent.ssl = false;
    Global.Configuration.Agent.http_https = "http://";
}

Global.Configuration.Agent.package_guid = Global.Initialization.Server_Config.Package_Guid();
Global.Configuration.Agent.communication_servers = Global.Initialization.Server_Config.Communication_Servers();
Global.Configuration.Agent.remote_servers = Global.Initialization.Server_Config.Remote_Servers();
Global.Configuration.Agent.update_servers = Global.Initialization.Server_Config.Update_Servers();
Global.Configuration.Agent.trust_servers = Global.Initialization.Server_Config.Trust_Servers();
Global.Configuration.Agent.file_servers = Global.Initialization.Server_Config.File_Servers();
Global.Configuration.Agent.tenant_guid = Global.Initialization.Server_Config.Tenant_Guid();
Global.Configuration.Agent.location_guid = Global.Initialization.Server_Config.Location_Guid();
Global.Configuration.Agent.language = Global.Initialization.Server_Config.Language();
Global.Configuration.Agent.device_name = Environment.MachineName;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("Windows platform detected");
    Logging.Debug("Program.cs", "Startup", "Windows platform detected");

    Global.Configuration.Agent.platform = "Windows";
}
else if (OperatingSystem.IsLinux())
{
    Console.WriteLine("Linux platform detected");
    Logging.Debug("Program.cs", "Startup", "Linux platform detected");

    Global.Configuration.Agent.platform = "Linux";
}
else if (OperatingSystem.IsMacOS())
{
    Console.WriteLine("MacOS platform detected");
    Logging.Debug("Program.cs", "Startup", "MacOS platform detected");

    Global.Configuration.Agent.platform = "MacOS";
}

builder.Services.AddHostedService<Device_Worker>();

// Get access key
Device_Worker.access_key = Global.Initialization.Server_Config.Access_Key();
Global.Configuration.Agent.hwid = ENGINE.HW_UID; // init after access key, because the linux & macos hwid generation is based on the access key

Device_Worker.authorized = Global.Initialization.Server_Config.Authorized();

Global.Initialization.Health.Check_Directories();
Global.Initialization.Health.Check_Firewall();
Global.Initialization.Health.Check_Databases();
Global.Offline_Mode.Handler.Policy();
Global.Initialization.Health.Setup_Events_Virtual_Datatable();

// Check if platform is Windows
if (OperatingSystem.IsWindows())
{
    Console.WriteLine("Starting Windows Worker");
    Logging.Debug("Program.cs", "Startup", "Starting Windows Worker");

    builder.Services.AddHostedService<Windows_Worker>();
    builder.Services.AddWindowsService();
}
else if (OperatingSystem.IsLinux())
{    
    builder.Services.AddSystemd();
    // Currently disabled because there are no linux only native functions implemented
    /*Console.WriteLine("Starting Linux Worker");
    Logging.Debug("Program.cs", "Startup", "Starting Linux Worker");
    builder.Services.AddHostedService<Linux_Worker>();*/
}
else if (OperatingSystem.IsMacOS())
{    
    // Currently disabled because there are no macos only native functions implemented
/*    Console.WriteLine("Starting MacOS Worker");
    Logging.Debug("Program.cs", "Startup", "Starting MacOS Worker");
    builder.Services.AddHostedService<MacOS_Worker>();*/
}

Console.WriteLine("Starting Host");
Logging.Debug("Program.cs", "Startup", "Starting Host");

var host = builder.Build();
host.Run();