using NetLock_RMM_Agent_Remote;
using Global.Helper;
using System.Collections.Generic;
using Windows.Helper.ScreenControl; // Hinzugefügt für List<T>

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine("Starting NetLock RMM Remote Agent");

//SessionScreenshotService.MakeScreenshot(IntPtr.Zero, 0);
//SessionScreenshotService.Run();

// Check if debug mode
if (Logging.Check_Debug_Mode()) // debug_mode
{
    Console.WriteLine("Debug mode enabled");
    Global.Configuration.Agent.debug_mode = true;
}
else
    Console.WriteLine("Debug mode disabled");

// Thats dev stuff
Global.Configuration.Agent.debug_mode = true;

//var test = Win32Interop.GetActiveSessions(); // Typinferenz mit var, List<T> Problem gelöst

/*
foreach (var session in test)
{
    Console.WriteLine($"Session ID: {session.Id}, Name: {session.Name}, Type: {session.Type}, Username: {session.Username}");
    Logging.Debug("Program.cs", "Startup", $"Session ID: {session.Id}, Name: {session.Name}, Type: {session.Type}, Username: {session.Username}");
}
*/

/*bool success = Win32Interop.CreateInteractiveSystemProcess(
    commandLine: @"C:\Program Files\New folder (2)\NetLock RMM Windows Login Screen Test.exe",
    targetSessionId: 0, // oder z. B. Process.GetCurrentProcess().SessionId
    hiddenWindow: false,
    out var procInfo);

if (success)
{
    Console.WriteLine("Prozess gestartet mit PID: " + procInfo.dwProcessId);
    Logging.Debug("Program.cs", "Startup", "Prozess gestartet mit PID: " + procInfo.dwProcessId);
}
else
{
    Console.WriteLine("Fehler beim Starten des Prozesses.");
    Logging.Debug("Program.cs", "Startup", "Fehler beim Starten des Prozesses.");
}
*/

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
