using _x101.HWID_System;
using Global.Helper;
using NetLock_RMM_Agent_Comm;

var builder = Host.CreateApplicationBuilder(args);

// Check if debug mode
if (Logging.Check_Debug_Mode()) // debug_mode
{
    Console.WriteLine("Debug mode enabled");
    Global.Configuration.Agent.debug_mode = true;
}

Global.Configuration.Agent.debug_mode = true;

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
Global.Configuration.Agent.hwid = ENGINE.HW_UID;

builder.Services.AddHostedService<Device_Worker>();

// Get access key
Device_Worker.access_key = Global.Initialization.Server_Config.Access_Key();

Device_Worker.authorized = Global.Initialization.Server_Config.Authorized();

Global.Initialization.Health.Check_Directories();
Global.Initialization.Health.Check_Firewall();
Global.Initialization.Health.Check_Databases();
Global.Offline_Mode.Handler.Policy();
Global.Initialization.Health.User_Process();
Global.Initialization.Health.Setup_Events_Virtual_Datatable();

// Check if platform is Windows
if (OperatingSystem.IsWindows())
{
    Logging.Debug("Program.cs", "Startup", "Windows platform detected");
    builder.Services.AddHostedService<Windows_Worker>();
}
else if (OperatingSystem.IsLinux())
{
    Logging.Debug("Program.cs", "Startup", "Linux platform detected");
    builder.Services.AddHostedService<Linux_Worker>();
}

var host = builder.Build();
host.Run();