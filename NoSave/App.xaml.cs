using NoSave.Models;
using NoSave.Services;
using NoSave.Services.Interfaces;
using NoSave.Services.Localization;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace NoSave
{
    public partial class App : Application
    {
        public static LocalizationService Localization { get; private set; }

        private IFirewallService _firewallService;
        private FirewallModel _firewallModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CloseOtherInstances();

            Localization = new LocalizationService(e.Args);

            _firewallService = new FirewallService();
            _firewallModel = new FirewallModel(_firewallService);

            FirewallConnectivityTestService firewallConnectivityTest = new FirewallConnectivityTestService(_firewallService);

            if (firewallConnectivityTest.IsInternetWorking())
            {
                if (!firewallConnectivityTest.IsFirewallWorking())
                {
                    MessageBox.Show(
                        Localization.GetString("FirewallCheckFailedMessage"),
                        Localization.GetString("FirewallCheckFailedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    Shutdown();
                    return;
                }
            }
            else
            {
                MessageBox.Show(
                    Localization.GetString("ConnectionCheckFailedMessage"),
                    Localization.GetString("ConnectionCheckFailedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
                return;
            }

            MainWindow mainWindow = new MainWindow(_firewallService, _firewallModel)
            {
                Left = 10,
                Top = 10
            };
            mainWindow.Show();
        }

        public static void ShowError(string messageKey, Exception ex)
        {
            string message = Localization.GetString(messageKey + "Message");
            if (ex != null)
                message += Environment.NewLine + Environment.NewLine + ex.Message;

            MessageBox.Show(
                message,
                Localization.GetString(messageKey + "Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Closes older copies of NoSave. Only processes started from the same executable file are closed,
        /// so an unrelated program that happens to be called "NoSave" is left alone.
        /// </summary>
        private void CloseOtherInstances()
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                string currentPath = GetExecutablePath(currentProcess);
                if (currentPath == null)
                    return;

                foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    using (process)
                    {
                        if (process.Id == currentProcess.Id)
                            continue;

                        string otherPath = GetExecutablePath(process);
                        if (!string.Equals(otherPath, currentPath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        try
                        {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Could not close previous instance {process.Id}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static string GetExecutablePath(Process process)
        {
            try
            {
                return Path.GetFullPath(process.MainModule.FileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not read path of process {process.Id}: {ex.Message}");
                return null;
            }
        }
    }
}
