using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
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

namespace XDaggerMinerNuke
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
            try
            {
                UninstallAllServices();
                DeleteBinaryFiles();

                MessageBox.Show("Finished!");
            }
            catch (Exception ex)
            {
                //Swallow anything

                MessageBox.Show("Finished but something still need to cleanup!");
            }
        }

        private void UninstallAllServices()
        {
            string[] serviceNames = new string[] { "XDaggerMinerService", "XDaggerEthMinerService" };
            string[] serviceNameTemplates = new string[] { "XDaggerMinerService_{0}", "XDaggerEthMinerService_{0}" };

            try
            {
                foreach (string serviceName in serviceNames)
                {
                    TryUninstallService(serviceName);
                }

                foreach (string serviceNameTemplate in serviceNameTemplates)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        TryUninstallService(string.Format(serviceNameTemplate, i), i.ToString());
                    }
                }
            }
            catch(Exception ex)
            {

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

    }
}
