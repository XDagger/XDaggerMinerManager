using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
            this.txtTargetUserPassword.Password = managerConfig.DefaultPassword;

            InitializeEthPoolAddresses();
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
            string targetMachineName = txtMachineName.Text?.Trim();
            string targetMachinePath = txtTargetPath.Text?.Trim();
            string targetMachineUserName = txtTargetUserName.Text?.Trim();
            string targetMachinePassword = txtTargetUserPassword.Password?.Trim();

            MinerMachine machine = new MinerMachine() {
                FullMachineName = targetMachineName,
                LoginUserName = targetMachineUserName
            };

            machine.SetLoginPassword(targetMachinePassword);
            createdClient = new MinerClient(machine, targetMachinePath);

            // Check whether this target is already in Miner Manager Client list

            // Check the machine name and path is accessasible
            StepOne_ValidateTargetMachine();
        }

        
        private void btnStepTwoNext_Click(object sender, RoutedEventArgs e)
        {
            StepTwo_DownloadPackage();
        }

        private void btnStepThreeXDaggerNext_Click(object sender, RoutedEventArgs e)
        {
            // Choose the selected device and update the client config
            StepThree_ConfigXDaggerMiner();
        }

        private void btnStepThreeEthNext_Click(object sender, RoutedEventArgs e)
        {
            // Choose the selected device and update the client config
            StepThree_ConfigEthMiner();
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

        private void btnStepOneBrowse_Click(object sender, RoutedEventArgs e)
        {
            BrowseNetworkWindow browseNetworkWindow = new BrowseNetworkWindow();
            browseNetworkWindow.SetResultHandler(minerMachine =>
                { this.txtMachineName.Text = minerMachine?.FullMachineName; });

            browseNetworkWindow.ShowDialog();

        }

        private void cBxTargetEthPool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cBxTargetEthPoolHost.Items.Clear();
            if (cBxTargetEthPool.SelectedIndex < 0 || cBxTargetEthPool.SelectedIndex >= EthMinerPoolHelper.PoolHostUrls.Count)
            {
                return;
            }

            foreach (string ethPoolHost in EthMinerPoolHelper.PoolHostUrls[cBxTargetEthPool.SelectedIndex])
            {
                cBxTargetEthPoolHost.Items.Add(ethPoolHost);
            }
        }

        private void InitializeEthPoolAddresses()
        {
            cBxTargetEthPool.Items.Clear();
            foreach(string ethPoolName in EthMinerPoolHelper.PoolDisplayNames)
            {
                cBxTargetEthPool.Items.Add(ethPoolName);
            }

            cBxTargetEthPoolHost.Items.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        private void SwitchUIToStep(int step)
        {
            grdStepOne.Visibility = Visibility.Hidden;
            grdStepTwo.Visibility = Visibility.Hidden;
            grdStepThreeXDagger.Visibility = Visibility.Hidden;
            grdStepThreeEth.Visibility = Visibility.Hidden;
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
                    if (createdClient?.InstanceType == "XDagger")
                    {
                        grdStepThreeXDagger.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        grdStepThreeEth.Visibility = Visibility.Visible;
                    }
                    
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
                    return pingSender.Send(createdClient.Machine?.FullMachineName, timeout, buffer, options);
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("无法连接到目标机器:" + createdClient.Machine?.FullMachineName + taskResult.Exception.ToString());
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

                    // Enable the UI
                    btnStepOneNext.IsEnabled = true;
                    return;
                }
            }

            if (System.IO.Directory.Exists(createdClient.GetRemoteBinaryPath()))
            {
                MessageBox.Show("目标路径下已经存在矿机，请删除或更新");

                // Enable the UI
                btnStepOneNext.IsEnabled = true;
                return;
            }

            BackgroundWork<bool>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在扫描已存在矿机", btnStepOneNext);
                },
                () => {
                    return ServiceUtils.HasExistingService(createdClient.Machine?.FullMachineName);
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("扫描目标机器错误：" + taskResult.Exception.ToString());
                        return;
                    }
                    bool hasExistingService = taskResult.Result;

                    if (hasExistingService)
                    {
                        MessageBoxResult result = MessageBox.Show("检测到目标机器上已有矿机，确定要装新的矿机吗？", "确认", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No)
                        {
                            btnStepOneNext.IsEnabled = true;
                            return;
                        }
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

            createdClient.Version = version;
            createdClient.InstanceType = this.cBxTargetInstanceType.Text;

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
                    if (!Directory.Exists(createdClient.GetRemoteBinaryPath()))
                    {
                        winMinerBinary.CopyBinaryToTargetPath(createdClient.GetRemoteBinaryPath());
                    }

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
            txtWalletAddress.Text = ManagerConfig.Current.DefaultXDaggerWallet;
            txtWalletAddressEth.Text = ManagerConfig.Current.DefaultEthWallet;
            txtEmailAddressEth.Text = ManagerConfig.Current.DefaultEthEmail;
            txtEthWorkerName.Text = ManagerConfig.Current.DefaultEthWorkerName;
            if (ManagerConfig.Current.DefaultEthPoolIndex != null)
            {
                cBxTargetEthPool.SelectedIndex = ManagerConfig.Current.DefaultEthPoolIndex.Value;
            }
            if (ManagerConfig.Current.DefaultEthPoolHostIndex != null)
            {
                cBxTargetEthPoolHost.SelectedIndex = ManagerConfig.Current.DefaultEthPoolHostIndex.Value;
            }

            TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(createdClient.Machine);
            string daemonFullPath = IO.Path.Combine(createdClient.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);

            BackgroundWork<List<DeviceOutput>>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在获取硬件信息", btnStepThreeNext, btnStepThreeBack);
                },
                () => {
                    return executor.ExecuteCommandAndThrow<List<DeviceOutput>>(daemonFullPath, "-l");
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
                    cBxTargetDeviceEth.Items.Clear();
                    foreach (DeviceOutput deviceOut in devices)
                    {
                        MinerDevice device = new MinerDevice(deviceOut.DeviceId, deviceOut.DisplayName, deviceOut.DeviceVersion, deviceOut.DriverVersion);
                        displayedDeviceList.Add(device);
                        cBxTargetDevice.Items.Add(device.DisplayName);
                        cBxTargetDeviceEth.Items.Add(device.DisplayName);
                    }
                }
            ).Execute();
        }

        /// <summary>
        /// 
        /// </summary>
        private void StepThree_ConfigXDaggerMiner()
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

            BackgroundWork<int?>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在配置矿机", btnStepThreeNext, btnStepThreeBack);
                },
                () => {

                    string commandParameters = string.Format(" -c \"{{ 'DeviceId':'{0}', 'XDaggerWallet':'{1}', 'AutoDecideInstanceId':true }}\"", 
                        selectedDevice.DeviceId, 
                        walletAddress);

                    ConfigureOutput exeResult = createdClient.ExecuteDaemon<ConfigureOutput>(commandParameters);
                    return exeResult.InstanceId;
                },
                (taskResult) => {

                    HideProgressIndicator();

                    if (taskResult.HasError)
                    {
                        MessageBox.Show("配置矿机出现错误：" + taskResult.Exception.ToString());
                        return;
                    }

                    int? instanceId = taskResult.Result;
                    createdClient.InstanceName = instanceId?.ToString();

                    // Save the currnet config into cache.
                    createdClient.Device = selectedDevice;
                    createdClient.XDaggerWalletAddress = walletAddress;
                    
                    if (cKbWalletSaveToDefault.IsChecked ?? false)
                    {
                        ManagerConfig.Current.DefaultXDaggerWallet = walletAddress;
                        ManagerConfig.Current.SaveToFile();
                    }

                    SwitchUIToStep(4);
                }
                ).Execute();
        }

        /// <summary>
        /// 
        /// </summary>
        private void StepThree_ConfigEthMiner()
        {
            MinerDevice selectedDevice = (cBxTargetDeviceEth.SelectedIndex >= 0) ? displayedDeviceList.ElementAt(cBxTargetDeviceEth.SelectedIndex) : null;
            if (selectedDevice == null)
            {
                MessageBox.Show("请选择一个硬件设备");
                return;
            }

            string ethWalletAddress = txtWalletAddressEth.Text;
            ethWalletAddress = ethWalletAddress.Trim();

            if (string.IsNullOrWhiteSpace(ethWalletAddress))
            {
                MessageBox.Show("请输入钱包地址");
                return;
            }

            if (!ethWalletAddress.StartsWith("0x"))
            {
                MessageBox.Show("钱包必须是以0x开头的32位字符串");
                return;
            }
            
            if (cBxTargetEthPool.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个ETH矿池");
                return;
            }

            if (cBxTargetEthPoolHost.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个ETH矿池地址");
                return;
            }

            EthMinerPoolHelper ethPoolHelper = new EthMinerPoolHelper();
            ethPoolHelper.Index = (EthMinerPoolHelper.PoolIndex)cBxTargetEthPool.SelectedIndex;
            ethPoolHelper.HostIndex = cBxTargetEthPoolHost.SelectedIndex;
            ethPoolHelper.EthWalletAddress = txtWalletAddressEth.Text;
            ethPoolHelper.EmailAddress = txtEmailAddressEth.Text;
            ethPoolHelper.WorkerName = txtEthWorkerName.Text;

            string ethFullPoolAddress = string.Empty;

            try
            {
                ethFullPoolAddress = ethPoolHelper.GeneratePoolAddress();
            }
            catch (Exception ex)
            {
                MessageBox.Show("配置矿机出现错误：" + ex.ToString());
                return;
            }

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在配置矿机", btnStepThreeNext, btnStepThreeBack);
                },
                () => {

                    string commandParameters = string.Format(" -c \"{{ 'DeviceId':'{0}', 'InstanceId':'{1}', 'EthPoolAddress':'{2}' }}\"",
                        selectedDevice.DeviceId,
                        createdClient.InstanceName,
                        ethFullPoolAddress);
                    OKResult exeResult = createdClient.ExecuteDaemon<OKResult>(commandParameters);
                    return 0;
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
                    createdClient.EthFullPoolAddress = ethFullPoolAddress;

                    if (cKbWalletSaveToDefault.IsChecked ?? false)
                    {
                        ManagerConfig.Current.DefaultEthPoolIndex = ethPoolHelper.Index.GetHashCode();
                        ManagerConfig.Current.DefaultEthPoolHostIndex = ethPoolHelper.HostIndex;
                        ManagerConfig.Current.DefaultEthWallet = ethPoolHelper.EthWalletAddress;
                        ManagerConfig.Current.DefaultEthWorkerName = ethPoolHelper.WorkerName;
                        ManagerConfig.Current.DefaultEthWorkerPassword = ethPoolHelper.WorkerPassword;
                        ManagerConfig.Current.DefaultEthEmail = ethPoolHelper.EmailAddress;

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
                    OKResult exeResult = createdClient.ExecuteDaemon<OKResult>("-s Install");
                    
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
                    OKResult exeResult = createdClient.ExecuteDaemon<OKResult>("-s Start");
                    
                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();

                    if (taskResult.HasError)
                    {
                        MessageBox.Show("启动矿机出现错误，请稍后手动启动：" + taskResult.Exception.ToString());
                        createdClient.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                    }
                    else
                    {
                        createdClient.CurrentServiceStatus = MinerClient.ServiceStatus.Disconnected;
                    }

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
