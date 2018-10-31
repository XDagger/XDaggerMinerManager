using System;
using Collection = System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
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
using XDaggerMinerManager.ObjectModel;
using XDaggerMinerManager.Configuration;
using XDaggerMinerManager.Utils;
using System.Timers;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Web;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Properties

        private MinerManager minerManager = null;

        private ObservableCollection<MinerDataGridItem> minerListGridItems = new ObservableCollection<MinerDataGridItem>();

        private bool isTimerRefreshingBusy = false;

        private Logger logger = Logger.GetInstance();

        private static readonly string MinerStatisticsSummaryTemplate = @"当前矿机数：{0}台  上线：{1}台  下线：{2}台  主算力：{3:0.000} Mps";

        #endregion

        public MainWindow()
        {
            logger.Trace("Initializing MainWindow.");

            InitializeComponent();
            minerManager = MinerManager.GetInstance();
            InitializeUIData();

            logger.Trace("Initialized MainWindow.");
        }

        #region UI Control Methods

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeInformationTextBox();

            // Set up a timer to trigger every second.  
            Timer timer = new Timer();
            timer.Interval = 2000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimerRefresh);
            timer.Start();
            isTimerRefreshingBusy = false;
        }

        private void minerListGrid_Loaded(object sender, RoutedEventArgs e)
        {
            minerListGrid.AutoGenerateColumns = false;
            minerListGrid.AllowDrop = false;
            minerListGrid.CanUserAddRows = false;
            minerListGrid.CanUserDeleteRows = false;
            minerListGrid.CanUserResizeRows = false;
            minerListGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            minerListGrid.DataContext = this;
            minerListGrid.ItemsSource = minerListGridItems;
        }

        private void btnAddMiner_Click(object sender, RoutedEventArgs e)
        {
            AddMinerWizardWindow addMinerWizard = new AddMinerWizardWindow();
            addMinerWizard.MinerCreated += OnMinerCreated;
            addMinerWizard.ShowDialog();
        }

        private void btnAddBatchMiner_Click(object sender, RoutedEventArgs e)
        {
            AddBatchMinerWizardWindow addBatchMinerWizard = new AddBatchMinerWizardWindow();
            addBatchMinerWizard.MinerCreated += OnMinerCreated;
            addBatchMinerWizard.ShowDialog();
        }

        private void operStartMiner_Click(object sender, RoutedEventArgs e)
        {
            StartSelectedMiner();
        }

        private void operStopMiner_Click(object sender, RoutedEventArgs e)
        {
            StopSelectedMiner();
        }

        private void operUninstallMiner_Click(object sender, RoutedEventArgs e)
        {
            UninstallSelectedMiner();
        }
        
        private void operModifyMiner_Click(object sender, RoutedEventArgs e)
        {
            ModifySelectedMiner();
        }

        private void btnLockScreen_Click(object sender, RoutedEventArgs e)
        {
            ManagerInfo info = ManagerInfo.Current;
            if (!info.HasLockPassword())
            {
                SetPasswordWindow passwordWindow = new SetPasswordWindow();

                passwordWindow.ShowDialog();

                if (string.IsNullOrWhiteSpace(passwordWindow.PasswordValue))
                {
                    return;
                }

                info.SetLockPassword(passwordWindow.PasswordValue);
            }

            LockWindow lockWindow = new LockWindow(this);
            lockWindow.Show();
        }

        private void cbxSelectMiners_Checked(object sender, RoutedEventArgs e)
        {
            logger.Trace("cbxSelectMiners_Checked");

            cbxSelectMiners.IsThreeState = false;
            foreach (MinerDataGridItem cell in minerListGridItems)
            {
                cell.IsSelected = true;
            }
            this.minerListGrid.Items.Refresh();

            RefreshMinerOperationButtonState();
        }

        private void cbxSelectMiners_Unchecked(object sender, RoutedEventArgs e)
        {
            logger.Trace("cbxSelectMiners_Unchecked");

            cbxSelectMiners.IsThreeState = false;
            foreach (MinerDataGridItem cell in minerListGridItems)
            {
                cell.IsSelected = false;
            }
            this.minerListGrid.Items.Refresh();

            RefreshMinerOperationButtonState();
        }

        private void minerListGridItems_IsSelected_CheckChanged(object sender, RoutedEventArgs e)
        {
            logger.Trace("minerListGridItems_IsSelected_CheckChanged");

            bool isAllSelected = true;
            bool isAllUnselected = true;

            foreach (MinerDataGridItem cell in minerListGridItems)
            {
                isAllSelected &= cell.IsSelected;
                isAllUnselected &= !cell.IsSelected;
            }

            if (isAllSelected || isAllUnselected)
            {
                cbxSelectMiners.IsThreeState = false;
                cbxSelectMiners.IsChecked = isAllSelected;
            }
            else
            {
                cbxSelectMiners.IsThreeState = true;
                cbxSelectMiners.IsChecked = null;
            }

            RefreshMinerOperationButtonState();
        }

        private void minerListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MinerDataGridItem cell = (MinerDataGridItem)minerListGrid.SelectedItem;
            UpdateInformationPanel(cell?.Client);
        }
        private void minerListGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MinerDataGridItem cell = (MinerDataGridItem)minerListGrid.SelectedItem;
            UpdateInformationPanel(cell?.Client);
        }

        private void cbxSelectMiners_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("cbxSelectMiners_Click");

            cbxSelectMiners.IsThreeState = false;
        }

        private void btnSendWalsonReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("确定要发送错误日志信息给开发人员吗？（不会泄露个人信息）", "确认", MessageBoxButton.YesNo);
            if (r == MessageBoxResult.No)
            {
                return;
            }

            WatsonWindow watsonWindow = new WatsonWindow();
            watsonWindow.ShowDialog();
        }

        
        #endregion

        #region UI Common Functions

        private void InitializeUIData()
        {
            logger.Trace("Start InitializeUIData.");

            this.Title = string.Format("XDagger Miner Manager Platform ({0})", minerManager.Version);

            logger.Trace($"Retrieving MinerClients from MinerManager. Totally { minerManager.ClientList.Count } clients.");
            foreach (MinerClient client in minerManager.ClientList)
            {
                minerListGridItems.Add(new MinerDataGridItem(client));
            }

            RefreshMinerListGrid();
            RefreshMinerOperationButtonState();
            

            logger.Trace("End InitializeUIData.");
        }

        private void RefreshMinerListGrid()
        {   
            minerListGrid.Items.Refresh();

            double totalHashrate = 0;
            foreach (MinerClient client in minerManager.ClientList)
            {
                totalHashrate += client.CurrentHashRate;
            }

            int totalClient = minerManager.ClientList.Count;
            int runningClient = minerManager.ClientList.Count((client) => { return client.CurrentServiceStatus == MinerClient.ServiceStatus.Mining; });
            int stoppedClient = totalClient - runningClient;

            this.tBxClientStatisticsSummary.Text = string.Format(MinerStatisticsSummaryTemplate, totalClient, runningClient, stoppedClient, totalHashrate / 1000000.0f);
        }

        private void UpdateInformationPanel(MinerClient client)
        {
            if (client == null)
            {
                lblMinerClientName.Content = string.Empty;
                lblMinerMachineName.Content = string.Empty;
                lblMinerDeviceName.Content = string.Empty;
                lblMinerType.Content = string.Empty;
                lblPoolAddress.Content = string.Empty;
                lblWalletAddress.Content = string.Empty;
                return;
            }

            lblMinerClientName.Content = client.Name;
            lblMinerMachineName.Content = client.Machine?.FullName;
            lblMinerDeviceName.Content = client.Device?.DisplayName;
            lblMinerType.Content = client.InstanceTypeEnum.ToString();

            if (client.InstanceTypeEnum == MinerClient.InstanceTypes.XDagger)
            {
                lblWalletAddress.Content = client.XDaggerConfig.WalletAddress;
                lblPoolAddress.Content = client.XDaggerConfig.PoolAddress;
            }
            else
            {
                lblWalletAddress.Content = client.EthConfig.WalletAddress;
                lblPoolAddress.Content = client.EthConfig.PoolFullAddress;
            }
        }

        private void InitializeInformationTextBox()
        {
            // Miner Information
            StringBuilder builder = new StringBuilder();
            builder.Append("XDaggerMinerManager Version: \r\n");
            builder.AppendFormat("{0}\r\n", ManagerConfig.Current.Version);
            builder.Append("\r\n");

            builder.Append("Latest Miner Version:\r\n");
            WinMinerReleaseVersions versions = WinMinerReleaseBinary.GetVersionInfo();
            builder.AppendFormat("{0}\r\n", versions.Latest);

            tbxMinerInformation.Text = builder.ToString();

            // OS Information
            StringBuilder builder2 = new StringBuilder();
            builder2.Append("Windows Version: \r\n");
            builder2.AppendFormat("{0}\r\n", SystemInformation.GetWindowsVersion());
            builder2.Append("\r\n");

            tbxWinInformation.Text = builder2.ToString();
        }

        public void OnMinerCreated(object sender, MinerCreatedEventArgs e)
        {
            logger.Trace("Start OnMinerCreated.");

            MinerCreatedEventArgs args = e as MinerCreatedEventArgs;

            if (args == null || args.CreatedClients == null)
            {
                return;
            }

            foreach (MinerClient client in args.CreatedClients)
            {
                minerManager.AddClient(client);
                minerListGridItems.Add(new MinerDataGridItem(client));
            }

            RefreshMinerListGrid();

            logger.Trace("End OnMinerCreated.");
        }

        private void OnTimerRefresh(object sender, ElapsedEventArgs e)
        {
            if (isTimerRefreshingBusy)
            {
                return;
            }

            isTimerRefreshingBusy = true;

            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    RefreshMinerOperationButtonState();
                });
            }
            catch(Exception)
            {
                // TODO: Temporary swallow evernthing
            }

            try
            {
                    bool shouldRefreshUI = false;
                    foreach (MinerClient client in this.minerManager.ClientList)
                    {
                        shouldRefreshUI |= client.RefreshStatus();
                        client.ResetStatusChanged();
                    }

                if (shouldRefreshUI)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        RefreshMinerListGrid();
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception while refreshing client status: " + ex.ToString());
                // Temporary swallow all exceptions
            }
            finally
            {
                isTimerRefreshingBusy = false;
            }
        }

        private void StartSelectedMiner()
        {
            logger.Trace("Start StartSelectedMiner.");

            List<MinerDataGridItem> minerDataGridItems = GetSelectedRowsInDataGrid();
            if(minerDataGridItems.Count == 0)
            {
                return;
            }

            List<object> selectedClients = new List<object>();
            foreach (MinerDataGridItem minerItem in minerDataGridItems)
            {
                selectedClients.Add(minerItem.Client);
            }
            
            ProgressWindow progress = new ProgressWindow("正在启动矿机...",
                selectedClients, 
                (obj) => {
                    MinerClient client = (MinerClient)obj;
                    OKResult r = client.ExecuteDaemon<OKResult>("-s start");
                    client.CurrentServiceStatus = MinerClient.ServiceStatus.Disconnected;
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.Message);
                        logger.Error("Got Error while starting miner: " + result.Exception.ToString());
                    }
                    else
                    {
                        logger.Trace("Miner started.");
                    }

                    this.RefreshMinerListGrid();
                }
                );
            progress.ShowDialog();
        }

        private void StopSelectedMiner()
        {
            logger.Trace("Start StopSelectedMiner.");

            List<MinerDataGridItem> minerDataGridItems = GetSelectedRowsInDataGrid();
            if (minerDataGridItems.Count == 0)
            {
                return;
            }

            List<object> selectedClients = new List<object>();
            foreach (MinerDataGridItem minerItem in minerDataGridItems)
            {
                selectedClients.Add(minerItem.Client);
            }

            ProgressWindow progress = new ProgressWindow("正在停止矿机...",
                selectedClients,
                (obj) => {
                    MinerClient client = (MinerClient)obj;
                    OKResult r = client.ExecuteDaemon<OKResult>("-s stop");
                    client.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.Message);
                        logger.Error("Got Error while stoping miner: " + result.Exception.ToString());
                    }
                    else
                    {
                        logger.Trace("Miner stopped.");
                    }

                    this.RefreshMinerListGrid();
                }
                );
            progress.ShowDialog();
        }

        private void ModifySelectedMiner()
        {
            logger.Trace("Start ModifySelectedMiner.");
            List<MinerDataGridItem> minerDataGridItems = GetSelectedRowsInDataGrid();
            if (minerDataGridItems.Count == 0)
            {
                return;
            }

            ModifyMinerWindow updateWindow = new ModifyMinerWindow(
                minerDataGridItems.Select((item) => item.Client).ToList()
                );
            updateWindow.ShowDialog();
        }

        private void UninstallSelectedMiner()
        {
            logger.Trace("Start UninstallSelectedMiner.");
            List<MinerDataGridItem> minerDataGridItems = GetSelectedRowsInDataGrid();
            if (minerDataGridItems.Count == 0)
            {
                return;
            }

            List<object> selectedClients = new List<object>();
            foreach (MinerDataGridItem minerItem in minerDataGridItems)
            {
                selectedClients.Add(minerItem.Client);
            }

            if (MessageBox.Show("确定要卸载选定的矿机吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            
            ProgressWindow progress = new ProgressWindow("正在卸载矿机...",
                selectedClients,
                (obj) => {

                    MinerClient client = (MinerClient)obj;
                    try
                    {
                        OKResult r = client.ExecuteDaemon<OKResult>("-s uninstall");

                        client.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;
                        client.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                    }
                    catch (Exception ex)
                    {
                        client.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Unknown;
                        client.CurrentServiceStatus = MinerClient.ServiceStatus.Unknown;

                        logger.Error("Got Error while uninstalling miner service: " + ex.ToString());

                        throw;
                    }
                    finally
                    {
                        // Since sometimes the Windows Service will lock the config file for a while after uninstall, we will wait here
                        System.Threading.Thread.Sleep(5000);
                        client.DeleteBinaries();
                    }
                },
                (result) => {
                    if (result.HasError)
                    {
                        if (result.Exception is IOException)
                        {
                            /// MessageBox.Show("删除矿机目录错误，请到矿机目录下手动删除矿机文件。详细信息：" + result.Exception.Message);
                            logger.Error("Got error while uninstalling miner with IOException: " + result.Exception.ToString());
                        }
                        else
                        {
                            /// MessageBox.Show("错误：" + result.Exception.Message);
                            logger.Error("Something wrong while uninstalling miner: " + result.Exception.ToString());
                        }
                    }

                    // Removing client from ObjectModel first, and then Delete binaries might throw IO exception which should be ignored
                    foreach (MinerDataGridItem item in minerDataGridItems)
                    {
                        minerManager.RemoveClient(item.Client);
                        minerListGridItems.Remove(item);
                    }

                    this.RefreshMinerListGrid();
                },
                false);
            progress.ShowDialog();
        }

        private void RefreshMinerOperationButtonState()
        {
            List<MinerDataGridItem> selectedRows = GetSelectedRowsInDataGrid();

            this.btnMinerOperation.IsEnabled = (selectedRows.Count > 0);
            this.operModifyMiner.IsEnabled = (selectedRows.Count > 0);
            this.operUninstallMiner.IsEnabled = (selectedRows.Count > 0);

            if (selectedRows.Count <= 0)
            {
                this.operStartMiner.IsEnabled = false;
                this.operStopMiner.IsEnabled = false;
                return;
            }
            
            bool containsStartedMiner = false;
            bool containsStoppedMiner = false;
            bool containsNotReadyMiner = false;

            foreach (MinerClient client in selectedRows.Select(row => row.Client))
            {
                containsStartedMiner |= client.IsServiceStatusRunning();
                containsStoppedMiner |= !client.IsServiceStatusRunning();
                containsNotReadyMiner |= (client.CurrentDeploymentStatus != MinerClient.DeploymentStatus.Ready);
            }

            this.operStartMiner.IsEnabled = !containsNotReadyMiner && containsStoppedMiner;
            this.operStopMiner.IsEnabled = !containsNotReadyMiner && containsStartedMiner;
        }

        private List<MinerDataGridItem> GetSelectedRowsInDataGrid()
        {
            return this.minerListGridItems.Where(row => row.IsSelected).ToList();
        }



        #endregion

        
    }
}
