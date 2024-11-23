using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;

namespace NetLock_RMM_Server.Microsoft_Defender_Firewall
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
        
        public static void Rule_Inbound(string port)
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Server Inbound HTTP (" + port + ")");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Debug("Microsoft_Defender_Firewall.Rule_Inbound", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Server Inbound HTTP (" + port + ")";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    new_rule.LocalPorts = port;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Debug("Microsoft_Defender_Firewall.Rule_Inbound", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.Rule_Inbound", "Add NetLock service rule (outbound)", ex.ToString());
            }
        }

        public static void Rule_Outbound(string port)
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                // Check if NetLock service rule is existing
                bool rule_existing = firewallPolicy.Rules.Cast<INetFwRule>().Any(rule => rule.Name == "NetLock RMM Server Outbound HTTPS (" + port + ")");

                // Create NetLock service rule if not existing
                if (!rule_existing)
                {
                    Logging.Handler.Debug("Microsoft_Defender_Firewall.Rule_Outbound", "rule_existing", rule_existing.ToString());

                    INetFwRule2 new_rule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    new_rule.Name = "NetLock RMM Server Outbound HTTPS (" + port + ")";
                    new_rule.Enabled = true;
                    new_rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    new_rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    new_rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    new_rule.LocalPorts = port;
                    firewallPolicy.Rules.Add(new_rule);
                }
                else
                {
                    Logging.Handler.Debug("Microsoft_Defender_Firewall.Rule_Outbound", "rule_existing", rule_existing.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_Firewall.Rule_Outbound", "Add NetLock service rule (outbound)", ex.ToString());
            }
        }
    }
}
