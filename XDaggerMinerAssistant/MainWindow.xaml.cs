using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XDaggerMinerAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_Nuke_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定要卸载本机上所有与XDagger相关的服务吗？", "确认", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                UninstallAllServices();
                DeleteBinaryFiles();

                MessageBox.Show("卸载完成！");
            }
            catch (Exception ex)
            {
                //Swallow anything

                MessageBox.Show("卸载完成，但有些部分还需要自行清理。");
            }
        }
        
        private void btn_Prepare_Click(object sender, RoutedEventArgs e)
        {
            // Start the WinRM service
            ServiceController service = new ServiceController("winrm");
            TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
            
            // Run Powershell
            PowerShell psinstance = PowerShell.Create();
            psinstance.AddScript("Enable-PSRemoting -SkipNetworkProfileCheck -Force");
            psinstance.AddScript("Set-NetFirewallRule -Name 'WINRM-HTTP-In-TCP' -RemoteAddress Any");

            try
            {
                psinstance.Invoke();

                if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.Stopped)
                {
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }

                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }

                // Turn on file and printer sharing
                InvokeScript("netsh advfirewall firewall set rule group=\"File and Printer Sharing\" new enable=Yes");
                InvokeScript("netsh advfirewall set currentprofile state on");

                MessageBox.Show("本机设置完成！");
            }
            catch(Exception ex)
            {
                MessageBox.Show("本机设置错误：" + ex.ToString());
            }
        }

        private void UninstallAllServices()
        {
            string[] serviceNames = new string[] { "XDaggerMinerService", "XDaggerEthMinerService" };
            string[] serviceNameTemplates = new string[] { @"XDaggerMinerService_{0}", @"XDaggerEthMinerService_{0}" };
            
            foreach (string serviceName in serviceNames)
            {
                try
                {
                    TryUninstallService(serviceName);
                }
                catch(Exception ex)
                {

                }
            }

            foreach (string serviceNameTemplate in serviceNameTemplates)
            {
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        TryUninstallService(string.Format(serviceNameTemplate, i), i.ToString());
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void DeleteBinaryFiles()
        {

        }

        private void DeleteTempFiles()
        {

        }

        private void TryUninstallService(string serviceName, string instanceId = "")
        {
            ServiceController[] services = ServiceController.GetServices("localhost");
            ServiceController serviceController = services.FirstOrDefault(s => s.ServiceName == serviceName);

            if (serviceController == null)
            {
                return;
            }

            string serviceBinaryFullPath = serviceController.ImagePath;

            if (string.IsNullOrEmpty(serviceBinaryFullPath))
            {
                return;
            }

            string folderFullPath = serviceBinaryFullPath.Substring(0, serviceBinaryFullPath.LastIndexOf('\\'));
            
            // Uninstall the service
            if (!string.IsNullOrEmpty(instanceId))
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", "/instance=" + instanceId, serviceBinaryFullPath });
            }
            else
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", serviceBinaryFullPath });
            }

            Thread.Sleep(3000);

            // Try deleting the files
            System.IO.Directory.Delete(folderFullPath, true);
        }

        private void InvokeScript(string commandScript)
        {
            Process process = new System.Diagnostics.Process();
            ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = @"cmd.exe";
            startInfo.Arguments = "/K " + commandScript;
            process.StartInfo = startInfo;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // Swallow exception
                    }
                }
            }
        }
    }
}
