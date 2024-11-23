using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;

namespace NetLock_RMM_Comm_Agent_Windows.Microsoft_Defender_Firewall
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
                Logging.Handler.Error("Microsoft_Defender_Firewall.Handler.Status", "Windows Firewall Status", ex.ToString());
                return false;
            }
        }
        
        public static void NetLock_RMM_Comm_Agent_Rule_Outbound()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Comm Agent Outbound");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_RMM_Comm_Agent_Rule_Outbound", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Comm Agent Outbound";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.netlock_service_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_RMM_Comm_Agent_Rule_Outbound", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.NetLock_RMM_Comm_Agent_Rule_Outbound", "Add NetLock service rule (outbound)", ex.Message);
            }
        }

        public static void NetLock_RMM_Comm_Agent_Rule_Inbound()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Comm Agent Inbound");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_RMM_Comm_Agent_Rule_Inbound", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Comm Agent Inbound";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.netlock_service_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_RMM_Comm_Agent_Rule_Inbound", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.NetLock_RMM_Comm_Agent_Rule_Inbound", "Add NetLock service rule (inbound)", ex.Message);
            }
        }

        public static void NetLock_RMM_Health_Service_Rule()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Health Agent");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_RMM_Health_Service_Rule", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Health Agent";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.netlock_health_service_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_RMM_Health_Service_Rule", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.NetLock_RMM_Health_Service_Rule", "Add NetLock health service rule", ex.Message);
            }
        }

        public static void NetLock_Installer_Rule()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock installer rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Installer");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_Installer_Rule", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Installer";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.netlock_installer_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_Installer_Rule", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.NetLock_Installer_Rule", "Add NetLock installer rule", ex.Message);
            }
        }

        public static void NetLock_Uninstaller_Rule()
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock installer rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Uninstaller");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_Uninstaller_Rule", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Uninstaller";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.ApplicationName = Application_Paths.netlock_uninstaller_exe;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Microsoft_Defender_Firewall("Microsoft_Defender_Firewall.NetLock_Uninstaller_Rule", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.NetLock_Uninstaller_Rule", "Add NetLock uninstaller rule", ex.Message);
            }
        }
    }
}
