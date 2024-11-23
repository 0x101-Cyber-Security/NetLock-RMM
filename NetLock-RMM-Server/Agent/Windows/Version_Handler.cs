using MySqlConnector;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Diagnostics;
using System.Reflection;

namespace NetLock_RMM_Server.Agent.Windows
{
    public class Version_Handler
    {
        public class Device_Identity_Entity
        {
            public string? agent_version { get; set; }
            public string? device_name { get; set; }
            public string? location_guid{ get; set; }
            public string? tenant_guid { get; set; }
            public string? access_key { get; set; }
            public string? hwid { get; set; }
            public string? ip_address_internal { get; set; }
            public string? operating_system { get; set; }
            public string? domain { get; set; }
            public string? antivirus_solution { get; set; }
            public string? firewall_status { get; set; }
            public string? architecture { get; set; }
            public string? last_boot { get; set; }
            public string? timezone { get; set; }
            public string? cpu { get; set; }
            public string? cpu_usage { get; set; }
            public string? mainboard { get; set; }
            public string? gpu { get; set; }
            public string? ram { get; set; }
            public string? ram_usage { get; set; }
            public string? tpm { get; set; }
            public string? environment_variables { get; set; }
        }

        public class Root_Entity
        {
            public Device_Identity_Entity? device_identity { get; set; }
        }

        public static async Task<string> Check_Version(string json)
        {
            try
            {
                // Extract JSON
                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(json);
                Device_Identity_Entity device_identity = rootData.device_identity;

                // Log the communicated agent version
                string agent_version = device_identity.agent_version;
                Logging.Handler.Debug("Agent.Windows.Version_Handler.Check_Version", "Communicated agent version", agent_version);

                string windowsAgentVersion = Application_Settings.version;

                Logging.Handler.Debug("Agent.Windows.Version_Handler.Check_Version", "windowsAgentVersion", windowsAgentVersion);

                // Check if the communicated agent version is equal to the version in the appsettings.json file
                if (agent_version == windowsAgentVersion)
                {
                    return "identical";
                }
                else
                {
                    return "different";
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Agent.Windows.Version_Handler.Check_Version", "General error", ex.ToString());
                return "Invalid request.";
            }
        }
    }
}
