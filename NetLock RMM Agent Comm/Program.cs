using _x101.HWID_System;
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
if (Convert.ToBoolean(Global.Initialization.Server_Config.Load(1))) // ssl
{
    Global.Configuration.Agent.ssl = true;
    Global.Configuration.Agent.http_https = "https";
}
else
{
    Global.Configuration.Agent.ssl = false;
    Global.Configuration.Agent.http_https = "http";
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
Global.Configuration.Agent.device_name = Environment.MachineName;
Global.Configuration.Agent.hwid = ENGINE.HW_UID;

builder.Services.AddHostedService<Device_Worker>();

// Check if platform is Windows
if (OperatingSystem.IsWindows())
{
    builder.Services.AddHostedService<Windows_Worker>();
}

var host = builder.Build();
host.Run();
