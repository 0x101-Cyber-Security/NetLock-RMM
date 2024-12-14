using NetLock_RMM_Agent_Remote;
using Global.Helper;

var builder = Host.CreateApplicationBuilder(args);

// Check if debug mode
if (Logging.Check_Debug_Mode()) // debug_mode
{
    Console.WriteLine("Debug mode enabled");
    Global.Configuration.Agent.debug_mode = true;
}

Global.Configuration.Agent.debug_mode = true;

if (OperatingSystem.IsWindows())
    Global.Configuration.Agent.platform = "Windows";
else if (OperatingSystem.IsLinux())
    Global.Configuration.Agent.platform = "Linux";
else if (OperatingSystem.IsMacOS())
    Global.Configuration.Agent.platform = "MacOS";

Global.Initialization.Health.Check_Directories();

builder.Services.AddHostedService<Remote_Worker>();

var host = builder.Build();
host.Run();
