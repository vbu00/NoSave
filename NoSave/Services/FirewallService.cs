using System;
using NoSave.Services.Interfaces;
using System.Diagnostics;

namespace NoSave.Services
{
    /// <summary>
    /// Windows Firewall access through late-bound COM (HNetCfg.*).
    /// Late binding avoids the NetFwTypeLib COMReference, which needs the Windows SDK (TlbImp/AxImp) to build.
    /// </summary>
    public class FirewallService : IFirewallService
    {
        // Values from NetFwTypeLib (icftypes.h)
        private const int NET_FW_ACTION_BLOCK = 0;
        private const int NET_FW_RULE_DIR_OUT = 2;
        private const int NET_FW_PROFILE2_ALL = 0x7FFFFFFF;

        private const string RuleGroup = "NoSave";
        private const string RuleDescription = "Created by NoSave. Safe to delete if NoSave is not running.";

        private static dynamic CreateComObject(string progId)
        {
            Type type = Type.GetTypeFromProgID(progId, throwOnError: true);
            return Activator.CreateInstance(type);
        }

        private static dynamic GetFirewallPolicy() => CreateComObject("HNetCfg.FwPolicy2");

        public bool IsFirewallEnabled()
        {
            try
            {
                dynamic firewallManager = CreateComObject("HNetCfg.FwMgr");
                return (bool)firewallManager.LocalPolicy.CurrentProfile.FirewallEnabled;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking firewall status: {ex.Message}");
                return false;
            }
        }

        public bool CheckFirewallRule(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName))
                return false;

            try
            {
                dynamic firewallPolicy = GetFirewallPolicy();
                foreach (dynamic rule in firewallPolicy.Rules)
                {
                    string name = rule.Name;
                    if (string.Equals(name, ruleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking firewall rule '{ruleName}': {ex.Message}");
                return false;
            }
        }

        public void AddRule(string ruleName, string remoteIp)
        {
            if (CheckFirewallRule(ruleName))
                return;

            try
            {
                dynamic firewallRule = CreateComObject("HNetCfg.FwRule");

                firewallRule.Name = ruleName;
                firewallRule.Description = RuleDescription;
                firewallRule.Grouping = RuleGroup;
                firewallRule.Profiles = NET_FW_PROFILE2_ALL;
                firewallRule.Action = NET_FW_ACTION_BLOCK;
                firewallRule.Direction = NET_FW_RULE_DIR_OUT;
                firewallRule.RemoteAddresses = remoteIp;
                firewallRule.Enabled = true;

                dynamic firewallPolicy = GetFirewallPolicy();
                firewallPolicy.Rules.Add(firewallRule);
                Debug.WriteLine($"Successfully added firewall rule '{ruleName}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding firewall rule '{ruleName}': {ex.Message}");
                throw;
            }
        }

        public void RemoveRule(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName))
                return;

            // Rules.Remove deletes only one rule per call, so repeat until no rule with this name is left.
            const int maxAttempts = 50;
            for (int i = 0; i < maxAttempts && CheckFirewallRule(ruleName); i++)
            {
                try
                {
                    dynamic firewallPolicy = GetFirewallPolicy();
                    firewallPolicy.Rules.Remove(ruleName);
                    Debug.WriteLine($"Removed firewall rule '{ruleName}'.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not remove firewall rule '{ruleName}': {ex.Message}");
                    throw;
                }
            }
        }
    }
}
