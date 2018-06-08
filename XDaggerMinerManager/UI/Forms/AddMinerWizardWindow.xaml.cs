using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Threading;
using XDaggerMinerManager.ObjectModel;
using System.ComponentModel;
using IO = System.IO;
using XDaggerMinerManager.Utils;
using System.Diagnostics;

namespace XDaggerMinerManager.UI.Forms
{

    /// <summary>
    /// Interaction logic for AddMinerWizardWindow.xaml
    /// </summary>
    public partial class AddMinerWizardWindow : Window
    {
        public enum AddMinerWizardStatus
        {
            Initial,
            StepOne,
            StepTwo,
            StepThree,
            StepFour,
            Finished
        }

        private AddMinerWizardStatus wizardStatus = AddMinerWizardStatus.Initial;

        private MinerClient createdClient = null;

        private WinMinerReleaseBinary winMinerBinary = null;

        private List<MinerDevice> displayedDeviceList = new List<MinerDevice>();
        
        private void OnMinerCreated(EventArgs e)
        {
            EventHandler handler = MinerCreated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler MinerCreated;


        public AddMinerWizardWindow()
        {
            InitializeComponent();
        }

        public MinerClient CreatedClient
        {
            get
            {
                return createdClient;
            }
        }

        private void addMinerWizard_Loaded(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (wizardStatus == AddMinerWizardStatus.Finished)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show("确定要离开此向导吗？", "确认", MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void btnStepOneNext_Click(object sender, RoutedEventArgs e)
        {
            string targetMachineName = txtMachineName.Text;
            string targetMachinePath = txtTargetPath.Text;
            createdClient = new MinerClient(targetMachineName, targetMachinePath);

            // Check whether this target is already in Miner Manager Client list

            // Check the machine name and path is accessasible
            StepOne_ValidateTargetMachine();
        }

        
        private void btnStepTwoNext_Click(object sender, RoutedEventArgs e)
        {
            // Should Check all of the versions first and then select LATEST by default
            StepTwo_QueryMinerVersions();
        }

        private void btnStepThreeNext_Click(object sender, RoutedEventArgs e)
        {
            // Choose the selected device and update the client config
            StepThree_ConfigMiner();
        }

        private void btnStepTwoBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(1);
        }
        private void btnStepThreeBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(2);
        }

        private void btnStepFourBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFourFinish_Click(object sender, RoutedEventArgs e)
        {
            // Config the miner and start
            StepFour_SetupMiner();

            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        private void SwitchUIToStep(int step)
        {
            grdStepOne.Visibility = Visibility.Hidden;
            grdStepTwo.Visibility = Visibility.Hidden;
            grdStepThree.Visibility = Visibility.Hidden;
            grdStepFour.Visibility = Visibility.Hidden;

            lblStepOne.Background = null;
            lblStepTwo.Background = null;
            lblStepThree.Background = null;
            lblStepFour.Background = null;

            lblStepOne.FontWeight = FontWeights.Normal;
            lblStepTwo.FontWeight = FontWeights.Normal;
            lblStepThree.FontWeight = FontWeights.Normal;
            lblStepFour.FontWeight = FontWeights.Normal;

            switch (step)
            {
                case 1:
                    grdStepOne.Visibility = Visibility.Visible;
                    lblStepOne.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepOne.FontWeight = FontWeights.ExtraBold;
                    break;
                case 2:
                    grdStepTwo.Visibility = Visibility.Visible;
                    lblStepTwo.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepTwo.FontWeight = FontWeights.ExtraBold;
                    break;
                case 3:
                    grdStepThree.Visibility = Visibility.Visible;
                    lblStepThree.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepThree.FontWeight = FontWeights.ExtraBold;
                    break;
                case 4:
                    grdStepFour.Visibility = Visibility.Visible;
                    lblStepFour.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepFour.FontWeight = FontWeights.ExtraBold;
                    
                    break;
            }

            if (step ==3 && (displayedDeviceList == null || displayedDeviceList.Count == 0))
            {
                StepThree_RetrieveDeviceList();
            }

        }

        #region Private Component Level Methods

        private void StepOne_ValidateTargetMachine()
        {
            if (createdClient == null)
            {
                return;
            }

            btnStepOneNext.IsEnabled = false;

            Ping pingSender = new Ping();
            AutoResetEvent waiter = new AutoResetEvent(false);

            // When the PingCompleted event is raised,
            // the PingCompletedCallback method is called.
            pingSender.PingCompleted += new PingCompletedEventHandler(StepOne_ValidateTargetMachine_Completed);

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "test";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 10 seconds for a reply.
            int timeout = 10000;

            // Set options for transmission:
            // The data can go through 64 gateways or routers
            // before it is destroyed, and the data packet
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            Console.WriteLine("Time to live: {0}", options.Ttl);
            Console.WriteLine("Don't fragment: {0}", options.DontFragment);

            // Send the ping asynchronously.
            // Use the waiter as the user token.
            // When the callback completes, it can wake up this thread.
            pingSender.SendAsync(createdClient.MachineName, timeout, buffer, options, waiter);
        }

        private void StepOne_ValidateTargetMachine_Completed(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");
                MessageBox.Show("无法连接到目标机器:" + createdClient.MachineName);

                // Let the main thread resume. 
                // UserToken is the AutoResetEvent object that the main thread 
                // is waiting for.
                ((AutoResetEvent)e.UserState).Set();
                btnStepOneNext.IsEnabled = true;
                return;
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());
                MessageBox.Show("无法连接到目标机器:" + createdClient.MachineName);
                btnStepOneNext.IsEnabled = true;

                // Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
                return;
            }

            PingReply reply = e.Reply;
            
            StepOne_ValidateTargetPath();



            // Enable the UI
            btnStepOneNext.IsEnabled = true;
        }

        /// <summary>
        /// Check the existance of the client, and check version/config if exists
        /// </summary>
        private void StepOne_ValidateTargetPath()
        {
            if (!System.IO.Directory.Exists(createdClient.GetRemoteDeploymentPath()))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(createdClient.GetRemoteDeploymentPath());
                }
                catch (UnauthorizedAccessException unauthException)
                {
                    // TODO Handle Exception
                }
            }

            if (System.IO.Directory.Exists(createdClient.GetRemoteBinaryPath()))
            {
                MessageBox.Show("目标路径下已经存在矿机，请先删除");
                // Enable the UI
                btnStepOneNext.IsEnabled = true;
                return;
            }

            btnStepOneNext.IsEnabled = true;
            SwitchUIToStep(2);
        }

        private void StepTwo_QueryMinerVersions()
        {
            // TODO: Check all Versions

            StepTwo_DownloadPackage();
        }

        private void StepTwo_DownloadPackage()
        {
            string version = this.cBxTargetVersion.Text;
            if (string.IsNullOrEmpty(version))
            {
                MessageBox.Show("请选择一个版本");
                return;
            }

            winMinerBinary = new WinMinerReleaseBinary(version);

            try
            {
                // Update UI
                btnStepTwoNext.IsEnabled = false;
                btnStepTwoBack.IsEnabled = false;
                lblProgressMessage.Content = "正在下载安装包......";
                prbIndicator.IsIndeterminate = true;

                winMinerBinary.DownloadPackage(StepTwo_DownloadPackage_Completed);
            }
            catch (WebException webEx)
            {
                // TODO: Add handler
            }
            catch (InvalidOperationException invalidOper)
            {
                // TODO: Add handler
            }
        }

        private void StepTwo_DownloadPackage_Completed(object sender, AsyncCompletedEventArgs eventArg)
        {
            if (eventArg.Cancelled)
            {
                MessageBox.Show("Download Cancelled.");
                btnStepTwoNext.IsEnabled = true;
                btnStepTwoBack.IsEnabled = true;
                lblProgressMessage.Content = "下载失败";
                prbIndicator.IsIndeterminate = false;
                return;
            }

            if (eventArg.Error != null)
            {
                MessageBox.Show("Download Failed:" + eventArg.Error);
                btnStepTwoNext.IsEnabled = true;
                btnStepTwoBack.IsEnabled = true;
                lblProgressMessage.Content = "下载失败";
                prbIndicator.IsIndeterminate = false;
                return;
            }

            lblProgressMessage.Content = "";
            prbIndicator.IsIndeterminate = false;

            winMinerBinary.ExtractPackage();

            ////
            winMinerBinary.CopyBinaryToTargetPath(createdClient.GetRemoteBinaryPath());

            createdClient.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;

            displayedDeviceList.Clear();
            SwitchUIToStep(3);
        }

        private void StepThree_RetrieveDeviceList()
        {
            TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(createdClient.MachineName);
            string daemonFullPath = IO.Path.Combine(createdClient.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);

            try
            {
                List<DeviceOutput> devices = executor.ExecuteCommand<List<DeviceOutput>>(daemonFullPath, "-l");

                if (devices == null || devices.Count == 0)
                {
                    MessageBox.Show("没有找到任何满足条件的硬件，请检查目标机器配置");
                    return;
                }

                cBxTargetDevice.Items.Clear();

                foreach (DeviceOutput deviceOut in devices)
                {
                    MinerDevice device = new MinerDevice(deviceOut.DeviceId, deviceOut.DisplayName);
                    displayedDeviceList.Add(device);
                    cBxTargetDevice.Items.Add(device.DisplayName);
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            finally
            {
                btnStepThreeBack.IsEnabled = true;
                btnStepThreeNext.IsEnabled = true;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void StepThree_ConfigMiner()
        {
            btnStepThreeBack.IsEnabled = false;
            btnStepThreeNext.IsEnabled = false;

            // Set the selected device to the client config file

            MinerDevice selectedDevice = (cBxTargetDevice.SelectedIndex >= 0) ? displayedDeviceList.ElementAt(cBxTargetDevice.SelectedIndex) : null;
            if (selectedDevice == null)
            {
                MessageBox.Show("请选择一个硬件设备");
                return;
            }

            TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(createdClient.MachineName);
            string daemonFullPath = IO.Path.Combine(createdClient.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);

            try
            {
                executor.ExecuteCommand(daemonFullPath, string.Format(" -d {0}", selectedDevice.DeviceId));

                SwitchUIToStep(4);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            finally
            {
                btnStepThreeBack.IsEnabled = true;
                btnStepThreeNext.IsEnabled = true;
            }
            
        }

        private void StepFour_SetupMiner()
        {
            // Install the Service
            RemoteExecutor remote = new RemoteExecutor(createdClient.MachineName);
            string daemonScriptFullPath = IO.Path.Combine(createdClient.BinaryPath, WinMinerReleaseBinary.DaemonScriptFileName);
            try
            {
                remote.ExecuteCommand(daemonScriptFullPath + " -Operation InstallService");

                createdClient.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Ready;
                createdClient.CurrentServiceStatus = MinerClient.ServiceStatus.Started;

                StepFour_Finish();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            finally
            {
                btnStepFourBack.IsEnabled = true;
                btnStepFourFinish.IsEnabled = true;
            }

        }

        private void StepFour_Finish()
        {
            MinerCreatedEventArgs ev = new MinerCreatedEventArgs(createdClient);
            this.OnMinerCreated(ev);

            wizardStatus = AddMinerWizardStatus.Finished;
            this.Close();
        }

        #endregion

    }


    public class MinerCreatedEventArgs : EventArgs
    {
        public MinerClient CreatedMiner
        {
            get; private set;
        }

        public MinerCreatedEventArgs(MinerClient client)
        {
            this.CreatedMiner = client;
        }
    }

}
