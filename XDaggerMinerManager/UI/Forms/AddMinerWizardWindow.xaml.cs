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
        
        private List<Control> freezedControlList = new List<Control>();

        private ManagerConfig managerConfig = ManagerConfig.Current;

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
            
            this.txtTargetPath.Text = managerConfig.DefaultInstallationPath;
            this.txtTargetUserName.Text = managerConfig.DefaultUserName;
            this.txtTargetUserPassword.Text = managerConfig.DefaultPassword;
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
            StepTwo_DownloadPackage();
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

            if (step == 2)
            {
                StepTwo_RetrieveMinerVersions();
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

            BackgroundWork<PingReply>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在连接目标机器......", btnStepOneNext);
                },
                () => {
                    Ping pingSender = new Ping();
                    AutoResetEvent waiter = new AutoResetEvent(false);
                    
                    string data = "test";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);
                    int timeout = 10000;

                    // Set options for transmission:
                    // The data can go through 64 gateways or routers
                    // before it is destroyed, and the data packet
                    // cannot be fragmented.
                    PingOptions options = new PingOptions(64, true);

                    // Send the ping asynchronously.
                    // Use the waiter as the user token.
                    // When the callback completes, it can wake up this thread.
                    return pingSender.Send(createdClient.MachineName, timeout, buffer, options);
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("无法连接到目标机器:" + createdClient.MachineName + taskResult.Exception.ToString());
                        return;
                    }

                    PingReply reply = taskResult.Result;

                    StepOne_ValidateTargetPath();
                }
            ).Execute();
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
                catch (Exception ex)
                {
                    MessageBox.Show("目标路径错误：" + ex.ToString());
                }
            }

            if (System.IO.Directory.Exists(createdClient.GetRemoteBinaryPath()))
            {
                MessageBox.Show("目标路径下已经存在矿机，请删除或更新");

                // Enable the UI
                btnStepOneNext.IsEnabled = true;
                return;
            }

            BackgroundWork<string>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在扫描已存在矿机", btnStepOneNext);
                },
                () => {
                    return ServiceUtils.DetectAvailableInstanceId(createdClient.MachineName);
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("扫描目标机器错误：" + taskResult.Exception.ToString());
                        return;
                    }
                    string instanceName = taskResult.Result;

                    if (!string.IsNullOrEmpty(instanceName))
                    {
                        MessageBoxResult result = MessageBox.Show("检测到目标机器上已有矿机，确定要装新的矿机吗？", "确认", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No)
                        {
                            btnStepOneNext.IsEnabled = true;
                            return;
                        }

                        createdClient.InstanceName = instanceName;
                    }

                    btnStepOneNext.IsEnabled = true;
                    SwitchUIToStep(2);
                }
                ).Execute();

            
        }

        private void StepTwo_RetrieveMinerVersions()
        {
            WinMinerReleaseVersions releaseVersions = null;

            // Check all Versions
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在查询矿机版本信息......", btnStepTwoNext, btnStepTwoBack);
                },
                () => {
                    releaseVersions = WinMinerReleaseBinary.GetVersionInfo();
                    return 0;
                },
                (taskResult) => {

                    if (taskResult.HasError || releaseVersions == null)
                    {
                        HideProgressIndicator();
                        MessageBox.Show("查询矿机版本错误: " + taskResult.Exception.ToString());
                        return;
                    }

                    // Update the version list
                    cBxTargetVersion.Items.Clear();
                    foreach(string availableVersion in releaseVersions.AvailableVersions)
                    {
                        cBxTargetVersion.Items.Add(availableVersion);
                    }

                    cBxTargetVersion.SelectedValue = releaseVersions.Latest;

                    HideProgressIndicator();
                }
            ).Execute();
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

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在下载安装包......", btnStepTwoNext, btnStepTwoBack);
                },
                () => {
                    winMinerBinary.DownloadPackage();
                    return 0;
                },
                (taskResult) => {

                    if (taskResult.HasError)
                    {
                        HideProgressIndicator();
                        MessageBox.Show("下载过程出现错误: " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        StepTwo_ExtractPackage();
                    }
                }
            ).Execute();
        }

        private void StepTwo_ExtractPackage()
        {
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在解压缩安装包......", btnStepTwoNext, btnStepTwoBack);
                },
                () => {
                    winMinerBinary.ExtractPackage();
                    return 0;
                },
                (taskResult) => {

                    if (taskResult.HasError)
                    {
                        HideProgressIndicator();
                        MessageBox.Show("解压缩过程出现错误: " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        StepTwo_CopyBinary();
                    }
                }
            ).Execute();
        }

        private void StepTwo_CopyBinary()
        {
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在拷贝文件到目标目录......", btnStepTwoNext, btnStepTwoBack);
                },
                () => {
                    winMinerBinary.CopyBinaryToTargetPath(createdClient.GetRemoteBinaryPath());
                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("拷贝过程出现错误: " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        createdClient.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;
                        displayedDeviceList.Clear();
                        SwitchUIToStep(3);
                    }
                }
            ).Execute();
        }

        private void StepThree_RetrieveDeviceList()
        {
            txtWalletAddress.Text = ManagerConfig.Current.DefaultWallet;


            TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(createdClient.MachineName);
            string daemonFullPath = IO.Path.Combine(createdClient.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);

            BackgroundWork<List<DeviceOutput>>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在获取硬件信息", btnStepThreeNext, btnStepThreeBack);
                },
                () => {
                    ExecutionResult<List<DeviceOutput>> getDevicesResult = executor.ExecuteCommand<List<DeviceOutput>>(daemonFullPath, "-l");
                    return getDevicesResult.Data;
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("查询系统硬件信息错误：" + taskResult.Exception.ToString());
                        return;
                    }
                    List<DeviceOutput> devices = taskResult.Result;

                    if (devices == null || devices.Count == 0)
                    {
                        MessageBox.Show("没有找到任何满足条件的硬件，请检查目标机器配置");
                        return;
                    }

                    cBxTargetDevice.Items.Clear();
                    foreach (DeviceOutput deviceOut in devices)
                    {
                        MinerDevice device = new MinerDevice(deviceOut.DeviceId, deviceOut.DisplayName, deviceOut.DeviceVersion, deviceOut.DriverVersion);
                        displayedDeviceList.Add(device);
                        cBxTargetDevice.Items.Add(device.DisplayName);
                    }
                }
                ).Execute();
        }

        /// <summary>
        /// 
        /// </summary>
        private void StepThree_ConfigMiner()
        {
            MinerDevice selectedDevice = (cBxTargetDevice.SelectedIndex >= 0) ? displayedDeviceList.ElementAt(cBxTargetDevice.SelectedIndex) : null;
            if (selectedDevice == null)
            {
                MessageBox.Show("请选择一个硬件设备");
                return;
            }

            string walletAddress = txtWalletAddress.Text;

            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                MessageBox.Show("请输入钱包地址");
                return;
            }

            walletAddress = walletAddress.Trim();
            if (walletAddress.Length != 32)
            {
                MessageBox.Show("钱包必须为长度32位的字母与数字组合");
                return;
            }

            

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在配置矿机", btnStepThreeNext, btnStepThreeBack);
                },
                () => {

                    string commandParameters = string.Format(" -c \"{{ 'DeviceId':'{0}', 'InstanceId':'{1}', 'Wallet':'{2}' }}\"", 
                        selectedDevice.DeviceId, 
                        createdClient.InstanceName,
                        walletAddress);
                    ExecutionResult<OKResult> exeResult = createdClient.ExecuteDaemon<OKResult>(commandParameters);

                    if (exeResult.HasError)
                    {
                        throw new InvalidOperationException(exeResult.ErrorMessage);
                    }
                    else
                    {
                        return 0;
                    }
                },
                (taskResult) => {

                    HideProgressIndicator();

                    if (taskResult.HasError)
                    {
                        MessageBox.Show("配置矿机出现错误：" + taskResult.Exception.ToString());
                        return;
                    }

                    // Save the currnet config into cache.
                    createdClient.Device = selectedDevice;
                    createdClient.WalletAddress = walletAddress;
                    
                    if (cKbWalletSaveToDefault.IsChecked ?? false)
                    {
                        ManagerConfig.Current.DefaultWallet = walletAddress;
                        ManagerConfig.Current.SaveToFile();
                    }

                    SwitchUIToStep(4);
                }
                ).Execute();
        }

        private void StepFour_SetupMiner()
        {
            // Install the Service
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在安装矿机服务", btnStepFourFinish, btnStepFourBack);
                },
                () => {
                    ExecutionResult<OKResult> exeResult = createdClient.ExecuteDaemon<OKResult>("-s Install");

                    if (exeResult.HasError)
                    {
                        throw new InvalidOperationException(exeResult.ErrorMessage);
                    }

                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();

                    if (taskResult.HasError)
                    {
                        MessageBox.Show("安装矿机出现错误：" + taskResult.Exception.ToString());
                        return;
                    }

                    createdClient.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Ready;

                    if (cKbSetStartTarget.IsChecked ?? false)
                    {
                        StepFour_StartMiner();
                    }
                    else
                    {
                        StepFour_Finish();
                    }
                }
            ).Execute();
        }

        private void StepFour_StartMiner()
        {
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在启动矿机服务", btnStepFourFinish, btnStepFourBack);
                },
                () => {
                    ExecutionResult<OKResult> exeResult = createdClient.ExecuteDaemon<OKResult>("-s Start");

                    if (exeResult.HasError)
                    {
                        throw new InvalidOperationException(exeResult.ErrorMessage);
                    }

                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();

                    if (taskResult.HasError)
                    {
                        MessageBox.Show("启动矿机出现错误：" + taskResult.Exception.ToString());
                        return;
                    }

                    createdClient.CurrentServiceStatus = MinerClient.ServiceStatus.Disconnected;
                    StepFour_Finish();
                }
            ).Execute();
        }

        private void StepFour_Finish()
        {
            MinerCreatedEventArgs ev = new MinerCreatedEventArgs(createdClient);
            this.OnMinerCreated(ev);

            wizardStatus = AddMinerWizardStatus.Finished;
            this.Close();
        }

        private void ShowProgressIndicator(string progressMesage, params Control[] controlList)
        {
            lblProgressMessage.Content = progressMesage;
            prbIndicator.IsIndeterminate = true;

            // Freeze the controls
            if (controlList != null)
            {
                freezedControlList.AddRange(controlList.ToArray());

                foreach (Control control in freezedControlList)
                {
                    control.IsEnabled = false;
                }
            }
        }

        private void HideProgressIndicator()
        {
            lblProgressMessage.Content = string.Empty;
            prbIndicator.IsIndeterminate = false;

            // Defreeze the controls
            foreach (Control control in freezedControlList)
            {
                control.IsEnabled = true;
            }

            freezedControlList.Clear();
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
