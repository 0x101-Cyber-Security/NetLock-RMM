using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;
using Global.Helper;
using SCNCore_Plus_Agent_Comm;

namespace Windows.Microsoft_Defender_Firewall
{
    internal class Handler
    {
        // Check if Windows Firewall is enabled
        public static bool Status()
        {
            try
            {
                INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr", false));
                return mgr.LocalPolicy.CurrentProfile.FirewallEnabled;
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_Firewall.Handler.Status", "Windows Firewall Status", ex.ToString());
                return false;
            }
        }
        
        public static void SCNCore_Plus_Comm_Agent_Rule_Outbound()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if SCNCore service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "SCNCore Plus Comm Agent Outbound");

                // Create SCNCore service rule if not existing
                if (!rule_existing)
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Plus_Comm_Agent_Rule_Outbound", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "SCNCore Plus Comm Agent Outbound";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.scncore_service_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Plus_Comm_Agent_Rule_Outbound", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_Firewall.SCNCore_Plus_Comm_Agent_Rule_Outbound", "Add SCNCore service rule (outbound)", ex.Message);
            }
        }

        public static void SCNCore_Plus_Comm_Agent_Rule_Inbound()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if SCNCore service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "SCNCore Plus Comm Agent Inbound");

                // Create SCNCore service rule if not existing
                if (!rule_existing)
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Plus_Comm_Agent_Rule_Inbound", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "SCNCore Plus Comm Agent Inbound";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.scncore_service_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Plus_Comm_Agent_Rule_Inbound", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_Firewall.SCNCore_Plus_Comm_Agent_Rule_Inbound", "Add SCNCore service rule (inbound)", ex.Message);
            }
        }

        public static void SCNCore_Plus_Health_Service_Rule()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if SCNCore service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "SCNCore Plus Health Agent");

                // Create SCNCore service rule if not existing
                if (!rule_existing)
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Plus_Health_Service_Rule", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "SCNCore Plus Health Agent";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.scncore_health_service_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Plus_Health_Service_Rule", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_Firewall.SCNCore_Plus_Health_Service_Rule", "Add SCNCore health service rule", ex.Message);
            }
        }

        public static void SCNCore_Installer_Rule()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if SCNCore installer rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "SCNCore Plus Installer");

                // Create SCNCore service rule if not existing
                if (!rule_existing)
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Installer_Rule", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "SCNCore Plus Installer";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.c_temp_installer_path;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Installer_Rule", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging .Error("Microsoft_Defender_Firewall.SCNCore_Installer_Rule", "Add SCNCore installer rule", ex.Message);
            }
        }

        public static void SCNCore_Uninstaller_Rule()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if SCNCore installer rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "SCNCore Plus Uninstaller");

                // Create SCNCore service rule if not existing
                if (!rule_existing)
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Uninstaller_Rule", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "SCNCore Plus Uninstaller";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.c_temp_installer_path;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.SCNCore_Uninstaller_Rule", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_Firewall.SCNCore_Uninstaller_Rule", "Add SCNCore uninstaller rule", ex.Message);
            }
        }
    }
}
