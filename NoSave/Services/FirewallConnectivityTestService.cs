using NoSave.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NoSave.Services
{
    public class FirewallConnectivityTestService
    {
        private readonly string[] _testAddresses =
        {
            "1.1.1.1",
            "8.8.8.8"
        };

        private const int TestPort = 443;

        private readonly List<string> _availableTestAddresses = new List<string>();

        private readonly IFirewallService _firewallService;

        public FirewallConnectivityTestService(IFirewallService firewallService)
        {
            _firewallService = firewallService;
        }
        public bool IsInternetWorking()
        {
            _availableTestAddresses.Clear();
            foreach (var address in _testAddresses)
            {
                if (CanConnect(address, TestPort))
                {
                    _availableTestAddresses.Add(address);
                }
            }

            if (_availableTestAddresses.Count == 0)
                return false;

            return true;
        }

        public bool IsFirewallWorking()
        {
            const string ruleName = "NoSaveTestRule";

            if (_availableTestAddresses.Count == 0)
                return false;

            try
            {
                // A previous run may have been killed in the middle of this check and left the test rule behind.
                // AddRule skips existing rules, so a stale rule (possibly for another address) must go first.
                _firewallService.RemoveRule(ruleName);

                foreach (var address in _availableTestAddresses)
                {
                    _firewallService.AddRule(ruleName, address);
                    if (CanConnect(address, TestPort))
                    {
                        return false;
                    }

                    _firewallService.RemoveRule(ruleName);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Firewall check failed: {ex}");
                return false;
            }
            finally
            {
                try
                {
                    _firewallService.RemoveRule(ruleName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not remove test rule: {ex.Message}");
                }
            }
        }

        private bool CanConnect(string address, int port, int timeoutMs = 5000)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    Task connectTask = client.ConnectAsync(address, port);
                    bool connected = connectTask.Wait(timeoutMs);

                    return connected && client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
