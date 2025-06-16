using NetLock_RMM_Agent_Remote;
using Global.Helper;
using System.Collections.Generic;
using Windows.Helper.ScreenControl; // Hinzugefügt für List<T>

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine("Starting NetLock RMM Remote Agent");

// Check if debug mode
if (Logging.Check_Debug_Mode()) // debug_mode
{
    Console.WriteLine("Debug mode enabled");
    Global.Configuration.Agent.debug_mode = true;
}
else
    Console.WriteLine("Debug mode disabled");

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("Windows platform detected");
    Logging.Debug("Program.cs", "Startup", "Windows platform detected");

    Global.Configuration.Agent.platform = "Windows";
    builder.Services.AddWindowsService();
}
else if (OperatingSystem.IsLinux())
{
    Console.WriteLine("Linux platform detected");
    Logging.Debug("Program.cs", "Startup", "Linux platform detected");

    Global.Configuration.Agent.platform = "Linux";
    builder.Services.AddSystemd();
}
else if (OperatingSystem.IsMacOS())
{
    Console.WriteLine("MacOS platform detected");
    Logging.Debug("Program.cs", "Startup", "MacOS platform detected");

    Global.Configuration.Agent.platform = "MacOS";
}

Global.Initialization.Health.Check_Directories();

builder.Services.AddHostedService<Remote_Worker>();

var host = builder.Build();
host.Run();
