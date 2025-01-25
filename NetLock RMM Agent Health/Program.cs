using NetLock_RMM_Agent_Health;
using Global.Helper;

Console.WriteLine("Starting NetLock RMM Health Agent");

var builder = Host.CreateApplicationBuilder(args);

// Check if debug mode
if (Logging.Check_Debug_Mode()) // debug_mode
{
    Console.WriteLine("Debug mode enabled");
    Global.Configuration.Agent.debug_mode = true;
}
else
    Console.WriteLine("Debug mode disabled");

// Check if SSL
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

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
