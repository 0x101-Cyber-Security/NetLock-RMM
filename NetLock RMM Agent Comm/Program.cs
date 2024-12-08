using _x101.HWID_System;
using Global.Helper;
using NetLock_RMM_Agent_Comm;
using Windows.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Check if debug mode
if (Global.Helper.Logging.Check_Debug_Mode()) // debug_mode
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

Global.Configuration.Agent.package_guid = Global.Initialization.Server_Config.Load(2);
Global.Configuration.Agent.communication_servers = Global.Initialization.Server_Config.Load(3);
Global.Configuration.Agent.remote_servers = Global.Initialization.Server_Config.Load(4);
Global.Configuration.Agent.update_servers = Global.Initialization.Server_Config.Load(5);
Global.Configuration.Agent.trust_servers = Global.Initialization.Server_Config.Load(6);
Global.Configuration.Agent.file_servers = Global.Initialization.Server_Config.Load(7);
Global.Configuration.Agent.tenant_guid = Global.Initialization.Server_Config.Load(8);
Global.Configuration.Agent.location_guid = Global.Initialization.Server_Config.Load(9);
Global.Configuration.Agent.language = Global.Initialization.Server_Config.Load(10);
Global.Initialization.Server_Config.Load(0); // Check/setup access key
Global.Configuration.Agent.device_name = Environment.MachineName;
Global.Configuration.Agent.hwid = ENGINE.HW_UID;

//builder.Services.AddHostedService<Device_Worker>();
builder.Services.AddHostedService<Device_Worker>();

// Get access key
Device_Worker.access_key = Global.Initialization.Server_Config.Load(11);

if (Global.Initialization.Server_Config.Load(12) == "true")
    Device_Worker.authorized = true;
else
    Device_Worker.authorized = false;

Global.Initialization.Health.Check_Directories();
Global.Initialization.Health.Check_Registry();
Global.Initialization.Health.Check_Firewall();
Global.Initialization.Health.Check_Databases();
Global.Initialization.Health.User_Process();
Global.Initialization.Health.Setup_Events_Virtual_Datatable();

builder.Services.AddHostedService<Windows_Worker>();

Console.Write("Starting agent...");
Logging.Debug("Program.cs", "Startup", "Starting agent...");

// Check if platform is Windows
if (OperatingSystem.IsWindows())
{
    Logging.Debug("Program.cs", "Startup", "Windows platform detected");
    //builder.Services.AddHostedService<Windows_Worker>();
}

var host = builder.Build();
host.Run();