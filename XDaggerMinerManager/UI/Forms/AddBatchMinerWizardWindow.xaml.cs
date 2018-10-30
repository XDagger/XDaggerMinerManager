using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
using System.Windows.Shapes;
using XDaggerMinerManager.Configuration;
using XDaggerMinerManager.Networking;
using XDaggerMinerManager.ObjectModel;
using XDaggerMinerManager.UI.Controls;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for AddBatchMinerWizardWindow.xaml
    /// </summary>
    public partial class AddBatchMinerWizardWindow : Window
    {
        #region Private Members

        private List<MinerClient> createdClients = null;
        private Logger logger = Logger.GetInstance();
        private MinerClient.InstanceTypes selectedMinerClientType;

        private bool needRefreshMachineConnections = true;

        private bool isWizardStatusFinished = false;

        // private Dictionary<string, MachineConnectivity> machineConnectivitiesResult = null;

        private MachineConnectivityBackgroundWork connectivityBackgroundWork = null;

        private List<MachineConnectivity> machineConnectivityCache = null;

        private string deploymentFolderPath = string.Empty;

        private List<Control> freezedControlList = new List<Control>();

        private WinMinerReleaseBinary winMinerBinary = null;

        private List<MinerDevice> displayeDeviceList = null;

        private void OnMinerCreated(MinerCreatedEventArgs e)
        {
            EventHandler<MinerCreatedEventArgs> handler = MinerCreated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<MinerCreatedEventArgs> MinerCreated;


        #endregion

        #region Component Methods

        public AddBatchMinerWizardWindow()
        {
            InitializeComponent();

            createdClients = new List<MinerClient>();
            machineConnectivityCache = new List<MachineConnectivity>();

            needRefreshMachineConnections = true;
            
            InitializeUI();

            SwitchUIToStep(1);
        }

        private void InitializeUI()
        {

            dataGridMachines.SetDisplayColumns(MachineDataGrid.Columns.FullName, MachineDataGrid.Columns.IpAddressV4);
            /// dataGridMachines.CanUserEdit = true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isWizardStatusFinished)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show("确定要离开此向导吗？", "确认", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }

            logger.Trace("AddBatchMinerWizardWindow Closed by user.");
        }

        private void btnAddByName_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("btnAddByName Clicked.");

            InputMachineName inputMachine = new InputMachineName();
            inputMachine.OnFinished += InputMachine_OnFinished;
            inputMachine.ShowDialog();
        }

        private void btnOpenNetwork_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("btnOpenNetwork Clicked.");

            BrowseNetworkWindow browseNetworkWindow = new BrowseNetworkWindow(true);
            browseNetworkWindow.SetResultHandler(
                minerMachines =>
                {
                    AddToMachineList(minerMachines);
                    needRefreshMachineConnections = true;
                });

            browseNetworkWindow.ShowDialog();
        }

        private void btnImportMachineList_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("btnImportMachineList Clicked.");

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt";

            string importFileFullName = string.Empty;
            bool? result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
            {
                return;
            }

            importFileFullName = dialog.FileName;
            List<MinerMachine> newMachines = new List<MinerMachine>();
            using (StreamReader reader = new StreamReader(importFileFullName))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string machineName = line.Trim().ToUpper();
                    if (machineName.Contains(","))
                    {
                        machineName = machineName.Substring(0, machineName.IndexOf(','));
                    }

                    if (!string.IsNullOrEmpty(machineName))
                    {
                        newMachines.Add(new MinerMachine() { FullName = machineName });
                    }
                }
            }

            AddToMachineList(newMachines);
        }

        private void btnDeleteMachine_Click(object sender, RoutedEventArgs e)
        {
            List<MinerMachine> selectedMachines = dataGridMachines.GetSelectedMachines();
            if (selectedMachines == null || selectedMachines.Count == 0)
            {
                return;
            }

            foreach(MinerMachine machine in selectedMachines)
            {
                dataGridMachines.RemoveItem(machine);
            }

            needRefreshMachineConnections = true;
        }

        private void btnStepOneNext_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtTargetPath.Text))
            {
                MessageBox.Show("请输入安装矿机的有效路径", "提示");
                return;
            }

            deploymentFolderPath = txtTargetPath.Text.Trim();

            List<MinerMachine> machineList = dataGridMachines.GetAllMachines();
            if (machineList.Count == 0)
            {
                MessageBox.Show("请输入机器名称", "提示");
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtTargetUserName.Text))
            {
                string username = txtTargetUserName.Text.Trim();

                if (string.IsNullOrEmpty(txtTargetUserPassword.Password))
                {
                    MessageBox.Show("请输入用户的密码", "提示");
                    return;
                }

                string password = txtTargetUserPassword.Password.Trim();
                MachineCredential credential = new MachineCredential() { UserName = username, LoginPlainPassword = password };

                // Consolidate the credentials
                foreach (MinerMachine m in machineList)
                {
                    m.Credential = credential;
                }
            }

            createdClients.Clear();
            for (int i = machineConnectivityCache.Count - 1; i >= 0; i--)
            {
                MachineConnectivity connectivity = machineConnectivityCache[i];
                if (!machineList.Any(m => m.FullName.Equals(connectivity.Machine.FullName)))
                {
                    machineConnectivityCache.RemoveAt(i);
                }
            }
            
            foreach (MinerMachine machine in machineList)
            {
                createdClients.Add(new MinerClient(machine, deploymentFolderPath));

                if (!machineConnectivityCache.Any(conn => conn.Machine.FullName.Equals(machine.FullName)))
                {
                    machineConnectivityCache.Add(new MachineConnectivity(machine));
                }
            }

            SwitchUIToStep(2);
        }

        private void btnStepTwoNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepTwoBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(1);
        }

        private void btnStepThreeNext_Click(object sender, RoutedEventArgs e)
        {
            StepThree_DownloadPackage();


            ///selectedMinerClientType = MinerClient.InstanceTypes.XDagger;
            ///SwitchUIToStep(4);
        }

        private void btnStepThreeBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(2);
        }

        private void btnStepThreeStatusNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(4);
        }

        private void btnStepThreeStatusBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFourXDaggerNext_Click(object sender, RoutedEventArgs e)
        {
            StepFour_ConfigureClients();
        }

        private void btnStepFourXDaggerBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFourEthNext_Click(object sender, RoutedEventArgs e)
        {
            StepFour_ConfigureClients();
        }

        private void btnStepFourEthBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFourStatusNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(5);
        }

        private void btnStepFourStatusBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFiveFinish_Click(object sender, RoutedEventArgs e)
        {
            if (cbxSetStartTarget.IsChecked.HasValue && cbxSetStartTarget.IsChecked.Value)
            {
                StepFive_StartService();
            }
            else
            {
                StepFive_Finish();
            }
        }

        private void btnStepFiveBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(4);
        }

        private void txtTargetUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            needRefreshMachineConnections = true;
        }

        private void txtTargetUserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            needRefreshMachineConnections = true;
        }

        private void txtTargetPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            needRefreshMachineConnections = true;
        }

        #endregion

        #region Private Methods


        private void InputMachine_OnFinished(object sender, MachineNameEventArgs e)
        {
            MinerMachine machine = new MinerMachine() { FullName = e.MachineName };
            AddToMachineList(new List<MinerMachine>() { machine });

            needRefreshMachineConnections = true;
        }

        private void MachineConnectivity_OnUpdated(object sender, MachineConnectivityEventArgs args)
        {
            this.Dispatcher.Invoke( () =>
            {
                this.dataGridMachineConnections.Refresh();
            });
        }

        private void ShowProgressIndicator(string message, params Control[] controlList)
        {
            logger.Trace("Start ShowProgressIndicator.");

            lblProgressIndicator.Visibility = Visibility.Visible;
            lblProgressIndicator.Content = message;

            prbIndicator.Visibility = Visibility.Visible;
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
            logger.Trace("Start HideProgressIndicator.");

            lblProgressIndicator.Visibility = Visibility.Hidden;
            lblProgressIndicator.Content = string.Empty;

            prbIndicator.IsIndeterminate = false;
            prbIndicator.Visibility = Visibility.Hidden;

            // Defreeze the controls
            foreach (Control control in freezedControlList)
            {
                control.IsEnabled = true;
            }

            freezedControlList.Clear();
        }

        private void SwitchUIToStep(int step)
        {
            logger.Trace("SwitchUIToStep: " + step);

            grdStepOne.Visibility = Visibility.Hidden;
            grdStepTwo.Visibility = Visibility.Hidden;
            grdStepThree.Visibility = Visibility.Hidden;
            grdStepThreeStatus.Visibility = Visibility.Hidden;
            grdStepFourXDagger.Visibility = Visibility.Hidden;
            grdStepFourEth.Visibility = Visibility.Hidden;
            grdStepFourStatus.Visibility = Visibility.Hidden;
            grdStepFive.Visibility = Visibility.Hidden;

            lblStepOne.Background = null;
            lblStepTwo.Background = null;
            lblStepThree.Background = null;
            lblStepFour.Background = null;
            lblStepFive.Background = null;

            lblStepOne.FontWeight = FontWeights.Normal;
            lblStepTwo.FontWeight = FontWeights.Normal;
            lblStepThree.FontWeight = FontWeights.Normal;
            lblStepFour.FontWeight = FontWeights.Normal;
            lblStepFive.FontWeight = FontWeights.Normal;

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
                    if (selectedMinerClientType == MinerClient.InstanceTypes.XDagger)
                    {
                        grdStepFourXDagger.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        grdStepFourEth.Visibility = Visibility.Visible;
                    }

                    lblStepFour.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepFour.FontWeight = FontWeights.ExtraBold;
                    break;
                case 5:
                    grdStepFive.Visibility = Visibility.Visible;
                    lblStepFive.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepFive.FontWeight = FontWeights.ExtraBold;

                    break;
            }

            if (step == 2)
            {
                StepTwo_TestMachineConnections();
            }

            if (step == 3)
            {
                StepThree_RetrieveMinerVersions();
            }
            if (step == 4)
            {
                StepFour_RetrieveDeviceList();
            }
        }

        private void StepTwo_TestMachineConnections()
        {
            if (!needRefreshMachineConnections)
            {
                return;
            }

            dataGridMachineConnections.ClearItems();

            MachineConnectivityBackgroundWork connectivityWork = new MachineConnectivityBackgroundWork(this);
            connectivityWork.OnUpdated += MachineConnectivity_OnUpdated;
            connectivityWork.SetTestingFolderPath(deploymentFolderPath);

            foreach (MinerClient client in createdClients)
            {
                MachineConnectivity connectivity = machineConnectivityCache.FirstOrDefault(conn => conn.Machine.FullName.Equals(client.Machine.FullName));
                if (connectivity == null)
                {
                    throw new InvalidDataException($"Null MachineConnectivity found in machineConnectivityCache with machineName=[{ client.Machine.FullName }].");
                }

                connectivity.ResetFailureResults();

                dataGridMachineConnections.AddItem(connectivity);
                connectivityWork.AddConnectivity(connectivity);
            }

            BackgroundWork.CreateWork(
                this,
                () => {
                    /// lblTestConnectionNotice.Content = "正在测试目标机器的连接，请稍后...";
                    ShowProgressIndicator("正在测试目标机器的连接，请稍后...", btnStepTwoNext, btnStepTwoBack);
                },
                () => {
                    connectivityWork.DoWork();
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("测试出现错误:" + taskResult.Exception.Message);
                        logger.Error("Error while testing connection: " + taskResult.Exception.ToString());

                        return;
                    }

                    bool hasFailure = machineConnectivityCache.Any(conn => !conn.IsAllTestingSuccess());
                    if (hasFailure)
                    {
                        btnStepTwoNext.IsEnabled = false;
                        MessageBox.Show("连接测试结果中有部分异常，请退回上一步重新选择机器或重新输入连接信息.");

                        logger.Warning("MachineConnectivityWork Finished with some failure, following is the detailed data:");
                        foreach (MachineConnectivity conn in machineConnectivityCache)
                        {
                            logger.Warning(conn.FullResult());
                        }
                    }

                    needRefreshMachineConnections = false;
                }
            ).Execute();
        }

        private void StepThree_DownloadPackage()
        {
            logger.Trace("Start StepThree_DownloadPackage.");

            string version = this.cbxMinerClientVersions.Text;
            if (string.IsNullOrEmpty(version))
            {
                MessageBox.Show("请选择一个版本");
                logger.Warning("Need to select one version to proceed.");

                return;
            }

            string instanceType = cbxMinerInstanceType.Text;
            selectedMinerClientType = (MinerClient.InstanceTypes)(this.cbxMinerInstanceType.SelectedIndex + 1);

            logger.Information($"Selected version: { version }.");
            logger.Information($"Selected instance type: { selectedMinerClientType }.");

            foreach(MinerClient client in createdClients)
            {
                client.Version = version;
                client.InstanceType = this.cbxMinerInstanceType.Text;
                client.InstanceTypeEnum = selectedMinerClientType;
            }

            winMinerBinary = new WinMinerReleaseBinary(version);

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在下载安装包......", btnStepThreeNext, btnStepThreeBack);
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
                        logger.Error("Got error while downloading package: " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        StepThree_ExtractPackage();
                    }
                }
            ).Execute();
        }

        private void StepThree_ExtractPackage()
        {
            logger.Trace("Start StepThree_ExtractPackage.");

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在解压缩安装包......", btnStepThreeNext, btnStepThreeBack);
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
                        logger.Error("Got error while extracting: " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        StepThree_CopyBinary();
                    }
                }
            ).Execute();

        }

        private void StepThree_CopyBinary()
        {
            logger.Trace("Start StepThree_CopyBinary.");

            grdStepThree.Visibility = Visibility.Hidden;
            grdStepThreeStatus.Visibility = Visibility.Visible;

            dataGridMachineDeployment.ClearItems();
            foreach(MinerClient client in createdClients)
            {
                dataGridMachineDeployment.AddItem(client);
            }

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在部署到目标机器......", btnStepThreeStatusNext, btnStepThreeStatusBack);
                },
                () => {
                    StepThree_CopyBinary_Sync();
                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        HideProgressIndicator();
                        MessageBox.Show("部署过程出现错误: " + taskResult.Exception.ToString());
                        logger.Error("Got error while copying binary: " + taskResult.Exception.ToString());

                        btnStepThreeStatusNext.IsEnabled = false;
                        return;
                    }

                    List<MinerClient> failedClients = new List<MinerClient>();
                    foreach(MinerClient client in createdClients)
                    {
                        if (client.CurrentDeploymentStatus != MinerClient.DeploymentStatus.Downloaded)
                        {
                            failedClients.Add(client);
                        }
                    }

                    if (failedClients.Count > 0)
                    {
                        MessageBox.Show("有部分矿机部署失败，请退回上一步重试.", "提示");
                        btnStepThreeStatusNext.IsEnabled = false;
                    }
                    
                }
            ).Execute();

        }

        private void StepThree_CopyBinary_Sync()
        {
            int startedWorkCount = 0;
            int finishedWorkCount = 0;

            foreach(MinerClient client in createdClients)
            {
                if (client.CurrentDeploymentStatus == MinerClient.DeploymentStatus.Downloaded)
                {
                    continue;
                }

                string username = client.Machine?.Credential?.UserName;
                string password = client.Machine?.Credential?.LoginPlainPassword;

                NetworkFileAccess fileAccess = new NetworkFileAccess(client.MachineFullName, username, password);


                startedWorkCount++;
                BackgroundWork<int>.CreateWork(this, () => { },
                () => {
                    if (fileAccess.DirectoryExists(client.BinaryPath))
                    {
                        if (client.HasFolderSuffix && client.CurrentDeploymentStatus == MinerClient.DeploymentStatus.Downloaded)
                        {
                            logger.Information($"Directory {client.BinaryPath} on machine {client.MachineFullName} already exists, so skip copying.");
                            return 0;
                        }
                        else
                        {
                            client.GenerateFolderSuffix();
                        }
                    }

                    fileAccess.DirectoryCopy(winMinerBinary.ExtractedBinaryPath, client.BinaryPath);
                    return 0;
                },
                (taskResult) => {

                    finishedWorkCount++;
                    if (taskResult.HasError)
                    {
                        logger.Error("Got error while copying binaries: " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        client.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;
                        dataGridMachineDeployment.Refresh();
                    }
                }
                ).Execute();
            }

            while(finishedWorkCount < startedWorkCount)
            {
                Thread.Sleep(30);
            }
        }

        private void StepThree_RetrieveMinerVersions()
        {
            logger.Trace("Start StepThree_RetrieveMinerVersions.");

            WinMinerReleaseVersions releaseVersions = null;

            // Check all Versions
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在获取矿机版本...", btnStepTwoNext, btnStepTwoBack);
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
                        logger.Error("GetVersionInfo failed with exception: " + taskResult.Exception.ToString());
                        return;
                    }

                    logger.Information($"GetVersionInfo got release version with lastest={ releaseVersions.Latest }.");

                    // Update the version list
                    cbxMinerClientVersions.Items.Clear();
                    foreach (string availableVersion in releaseVersions.AvailableVersions)
                    {
                        cbxMinerClientVersions.Items.Add(availableVersion);
                    }

                    cbxMinerClientVersions.SelectedValue = releaseVersions.Latest;

                    HideProgressIndicator();
                }
            ).Execute();
        }
        
        private void StepFour_RetrieveDeviceList()
        {
            logger.Trace("Start StepFour_RetrieveDeviceList.");

            txtXDaggerWalletAddress.Text = ManagerConfig.Current.DefaultXDagger.WalletAddress;
            txtXDaggerPoolAddress.Text = ManagerConfig.Current.DefaultXDagger.PoolAddress;
            txtWalletAddressEth.Text = ManagerConfig.Current.DefaultEth.WalletAddress;
            txtEmailAddressEth.Text = ManagerConfig.Current.DefaultEth.EmailAddress;
            txtEthWorkerName.Text = ManagerConfig.Current.DefaultEth.WorkerName;
            if (ManagerConfig.Current.DefaultEth.PoolIndex != null)
            {
                cbxTargetEthPool.SelectedIndex = ManagerConfig.Current.DefaultEth.PoolIndex.GetHashCode();
            }
            if (ManagerConfig.Current.DefaultEth.PoolHostIndex != null)
            {
                cbxTargetEthPoolHost.SelectedIndex = ManagerConfig.Current.DefaultEth.PoolHostIndex.Value;
            }

            BackgroundWork.CreateWork(
                    this,
                    () =>
                    {
                        ShowProgressIndicator("正在获取硬件信息...", btnStepFourXDaggerNext, btnStepFourXDaggerBack);
                    },
                    () =>
                    {
                        StepFour_RetrieveDeviceList_Sync();
                        return 0;
                    },
                    (taskResult) =>
                    {

                        HideProgressIndicator();
                        if (taskResult.HasError)
                        {
                            logger.Error("ExecuteCommand failed: " + taskResult.Exception.ToString());
                        }

                        displayeDeviceList = new List<MinerDevice>();
                        displayeDeviceList.AddRange(createdClients.First().Machine.Devices);

                        foreach (MinerClient client in createdClients)
                        {
                            // Note: If a machine has null devices, we consider there is temporary issue while retrieving devices so ignore it
                            List<MinerDevice> machineDevices = client.Machine.Devices;
                            if (machineDevices == null)
                            {
                                continue;
                            }

                            for(int i = displayeDeviceList.Count - 1; i >= 0; i--)
                            {
                                if (!machineDevices.Any(device => device.DisplayName.Equals(displayeDeviceList[i].DisplayName)))
                                {
                                    displayeDeviceList.RemoveAt(i);
                                }
                            }
                        }
                        
                        if (displayeDeviceList.Count == 0)
                        {
                            MessageBox.Show("没有找到在所选择机器上共有的硬件，请检查目标机器配置");
                            logger.Warning("没有找到在所选择机器上共有的硬件，请检查目标机器配置");
                            return;
                        }

                        cbxTargetDevice.Items.Clear();
                        cbxTargetDeviceEth.Items.Clear();
                        logger.Trace("Got Devices count: " + displayeDeviceList.Count);
                        foreach (MinerDevice device in displayeDeviceList)
                        {
                            cbxTargetDevice.Items.Add(device.DisplayName);
                            cbxTargetDeviceEth.Items.Add(device.DisplayName);
                        }
                    }
                ).Execute();
        }

        private void StepFour_RetrieveDeviceList_Sync()
        {
            int startedWorkCount = 0;
            int finishedWorkCount = 0;

            foreach(MinerClient client in createdClients)
            {
                if (client.Machine.Devices != null && client.Machine.Devices.Count > 0)
                {
                    continue;
                }

                MinerMachine existingMachine = ManagerInfo.Current.Machines.FirstOrDefault(m => m.FullName.Equals(client.Machine.FullName));
                if (existingMachine != null && existingMachine.Devices != null && existingMachine.Devices.Count > 0)
                {
                    // This machine has been queried before and the devices are saved in the ManagerInfo cache, read it
                    client.Machine.Devices = existingMachine.Devices;
                }
                else
                {
                    // Didn't find the machine in cache, use Executor to retrieve it
                    startedWorkCount++;
                    BackgroundWork<List<DeviceOutput>>.CreateWork(this, () => { },
                        () =>
                        {
                            return client.ExecuteDaemon<List<DeviceOutput>>("-l");
                        },
                        (taskResult) =>
                        {
                            finishedWorkCount++;
                            if (taskResult.HasError)
                            {
                                logger.Error($"ExecuteCommand on machine [{ client.Machine.FullName }] failed: " + taskResult.Exception.ToString());
                                return;
                            }

                            client.Machine.Devices = new List<MinerDevice>();
                            List<DeviceOutput> deviceOutputs = taskResult.Result;
                            if (deviceOutputs == null || deviceOutputs.Count == 0)
                            {
                                logger.Warning("没有找到任何满足条件的硬件，请检查目标机器配置");
                                return;
                            }

                            foreach(DeviceOutput output in deviceOutputs)
                            {
                                MinerDevice device = new MinerDevice(output.DeviceId, output.DisplayName, output.DeviceVersion, output.DriverVersion);
                                client.Machine.Devices.Add(device);
                            }
                        }
                    ).Execute();
                }
            }

            while(finishedWorkCount < startedWorkCount)
            {
                Thread.Sleep(30);
            }
        }

        private void StepFour_ConfigureClients()
        {
            logger.Trace("Start StepFour_ConfigureClients.");

            string configureParameters = string.Empty;
            string deviceName = string.Empty;

            XDaggerConfig xDaggerConfig = null;
            EthConfig ethConfig = null;

            if (selectedMinerClientType == MinerClient.InstanceTypes.XDagger)
            {
                xDaggerConfig = ValidateXDaggerConfig();
                if (xDaggerConfig == null)
                {
                    return;
                }

                deviceName = (string)cbxTargetDevice.SelectedValue;
                configureParameters = string.Format(" -c \"{{ 'DeviceName':'{0}', 'XDaggerWallet':'{1}', 'XDaggerPoolAddress':'{2}', 'AutoDecideInstanceId':true }}\"",
                        deviceName,
                        xDaggerConfig.WalletAddress,
                        xDaggerConfig.PoolAddress);
            }
            else if (selectedMinerClientType == MinerClient.InstanceTypes.Ethereum)
            {
                ethConfig = ValidateEthConfig();
                if (ethConfig == null)
                {
                    return;
                }

                deviceName = (string)cbxTargetDeviceEth.SelectedValue;
                configureParameters = string.Format(" -c \"{{ 'DeviceName':'{0}', 'EthPoolAddress':'{1}', 'AutoDecideInstanceId':true }}\"",
                        deviceName,
                        ethConfig.PoolFullAddress);
            }

            grdStepFourXDagger.Visibility = Visibility.Hidden;
            grdStepFourEth.Visibility = Visibility.Hidden;
            grdStepFourStatus.Visibility = Visibility.Visible;

            dataGridMachineConfiguration.ClearItems();
            foreach (MinerClient client in createdClients)
            {
                client.Device = client.Machine.Devices.FirstOrDefault(d => d.DisplayName.Equals(deviceName));
                if (selectedMinerClientType == MinerClient.InstanceTypes.XDagger)
                {
                    client.XDaggerConfig = xDaggerConfig;
                }
                else if(selectedMinerClientType == MinerClient.InstanceTypes.Ethereum)
                {
                    client.EthConfig = ethConfig;
                }
                
                dataGridMachineConfiguration.AddItem(client);
            }

            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在配置目标机器......", btnStepFourStatusNext, btnStepFourStatusBack);
                },
                () => {
                    StepFour_ConfigureClients_Sync(configureParameters);
                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        HideProgressIndicator();
                        MessageBox.Show("配置过程出现错误: " + taskResult.Exception.ToString());
                        logger.Error("Got error while copying binary: " + taskResult.Exception.ToString());

                        btnStepFourStatusNext.IsEnabled = false;
                        return;
                    }

                    List<MinerClient> failedClients = new List<MinerClient>();
                    foreach (MinerClient client in createdClients)
                    {
                        if (client.CurrentDeploymentStatus != MinerClient.DeploymentStatus.Ready)
                        {
                            failedClients.Add(client);
                        }
                    }

                    if (failedClients.Count > 0)
                    {
                        MessageBox.Show("有部分矿机配置失败，请退回上一步重试.", "提示");
                        btnStepFourStatusNext.IsEnabled = false;
                    }

                }
            ).Execute();

        }

        private void StepFour_ConfigureClients_Sync(string configureParameters)
        {
            int startedWorkCount = 0;
            int finishedWorkCount = 0;

            foreach (MinerClient client in createdClients)
            {
                if (client.CurrentDeploymentStatus == MinerClient.DeploymentStatus.Ready)
                {
                    continue;
                }

                startedWorkCount++;
                BackgroundWork<int>.CreateWork(this, () => { },
                () => {
                    OKResult exeResult = client.ExecuteDaemon<OKResult>(configureParameters);
                    exeResult = client.ExecuteDaemon<OKResult>("-s install");

                    return 0;
                },
                (taskResult) => {

                    finishedWorkCount++;
                    if (taskResult.HasError)
                    {
                        logger.Error($"Got error while configuring client on machine [{ client.Machine.FullName }] : " + taskResult.Exception.ToString());
                    }
                    else
                    {
                        client.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Ready;
                        dataGridMachineConfiguration.Refresh();
                    }
                }
                ).Execute();
            }

            while (finishedWorkCount < startedWorkCount)
            {
                Thread.Sleep(30);
            }
        }

        private XDaggerConfig ValidateXDaggerConfig()
        {
            if (cbxTargetDevice.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个硬件设备");
                return null;
            }

            XDaggerConfig xDaggerConfig = new XDaggerConfig();

            xDaggerConfig.PoolAddress = txtXDaggerPoolAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(xDaggerConfig.PoolAddress))
            {
                MessageBox.Show("请输入矿池地址");
                return null;
            }

            xDaggerConfig.WalletAddress = txtXDaggerWalletAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(xDaggerConfig.WalletAddress))
            {
                MessageBox.Show("请输入钱包地址");
                return null;
            }

            if (xDaggerConfig.WalletAddress.Length != 32)
            {
                MessageBox.Show("钱包必须为长度32位的字母与数字组合");
                return null;
            }

            return xDaggerConfig;
        }

        private EthConfig ValidateEthConfig()
        {
            if (cbxTargetDeviceEth.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个硬件设备");
                return null;
            }

            string ethWalletAddress = txtWalletAddressEth.Text;
            ethWalletAddress = ethWalletAddress.Trim();

            if (string.IsNullOrWhiteSpace(ethWalletAddress))
            {
                MessageBox.Show("请输入钱包地址");
                return null;
            }

            if (!ethWalletAddress.StartsWith("0x"))
            {
                MessageBox.Show("钱包必须是以0x开头的32位字符串");
                return null;
            }

            if (cbxTargetEthPool.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个ETH矿池");
                return null;
            }

            if (cbxTargetEthPoolHost.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个ETH矿池地址");
                return null;
            }

            EthConfig ethConfig = new EthConfig();
            ethConfig.PoolIndex = (EthConfig.PoolIndexes)cbxTargetEthPool.SelectedIndex;
            ethConfig.PoolHostIndex = cbxTargetEthPoolHost.SelectedIndex;
            ethConfig.WalletAddress = txtWalletAddressEth.Text.Trim();
            ethConfig.EmailAddress = txtEmailAddressEth.Text;
            ethConfig.WorkerName = txtEthWorkerName.Text;

            try
            {
                ethConfig.ValidateProperties();
                return ethConfig;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eth配置数据有误：" + ex.ToString());
                logger.Error("ValidateProperties failed: " + ex.ToString());
                return null;
            }
        }

        private void StepFive_StartService()
        {
            BackgroundWork<int>.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在启动目标矿机......", btnStepFiveFinish, btnStepFiveBack);
                },
                () => {
                    StepFive_StartService_Sync();
                    return 0;
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("启动矿机出现错误，请稍后手动启动：" + taskResult.Exception.ToString());
                        logger.Error("Got error while starting miner: " + taskResult.Exception.ToString());
                    }

                    StepFive_Finish();
                }
            ).Execute();

        }

        private void StepFive_StartService_Sync()
        {
            int startedWorkCount = 0;
            int finishedWorkCount = 0;

            foreach (MinerClient client in createdClients)
            {
                startedWorkCount++;
                BackgroundWork<int>.CreateWork(this, () => { },
                () =>
                {
                    OKResult exeResult = client.ExecuteDaemon<OKResult>("-s start");
                    return 0;
                },
                (taskResult) =>
                {
                    finishedWorkCount++;
                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        logger.Error("Got error while starting miner: " + taskResult.Exception.ToString());
                    }
                }
                ).Execute();
            }

            while(finishedWorkCount < startedWorkCount)
            {
                Thread.Sleep(30);
            }
        }

        private void StepFive_Finish()
        {
            logger.Trace("Start StepFive_Finish.");

            MinerCreatedEventArgs ev = new MinerCreatedEventArgs(createdClients);
            this.OnMinerCreated(ev);

            isWizardStatusFinished = true;
            this.Close();
        }

        /// <summary>
        /// Add the new list into the current machine list, if there are duplicate, prompt merge
        /// </summary>
        /// <param name="minerMachines"></param>
        private void AddToMachineList(List<MinerMachine> minerMachines)
        {
            bool duplicateFound = false;

            List<MinerMachine> existingMachines = dataGridMachines.GetAllMachines();

            foreach (MinerMachine machine in minerMachines)
            {
                if (existingMachines.Any(m => m.FullName.Equals(machine.FullName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    duplicateFound = true;
                }
                else
                {
                    dataGridMachines.AddItem(machine);
                }
            }
            
            if (duplicateFound)
            {
                MessageBox.Show("检测到机器名中有重复。由于批量创建矿机时每台机器只能同时创建一个矿机，机器列表中已经将重复的机器名去掉。", "提示", MessageBoxButton.OK);
            }
        }






        #endregion

        
    }
}
