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

        #region UI Control Operation

        private void btnAddMiner_Click(object sender, RoutedEventArgs e)
        {
            AddMinerWizardWindow addMinerWizard = new AddMinerWizardWindow();
            addMinerWizard.MinerCreated += OnMinerCreated;
            addMinerWizard.ShowDialog();
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up a timer to trigger every second.  
            Timer timer = new Timer();
            timer.Interval = 2000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimerRefresh);
            timer.Start();
            isTimerRefreshingBusy = false;
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

        public void OnMinerCreated(object sender, EventArgs e)
        {
            logger.Trace("Start OnMinerCreated.");

            MinerCreatedEventArgs args = e as MinerCreatedEventArgs;

            if (args == null || args.CreatedMiner == null)
            {
                return;
            }

            minerManager.AddClient(args.CreatedMiner);
            minerListGridItems.Add(new MinerDataGridItem(args.CreatedMiner));

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

            this.Dispatcher.Invoke(() =>
            {
                RefreshMinerOperationButtonState();
            });

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

            MinerClient selectedClient = minerDataGridItems.Select(row => row.Client).FirstOrDefault();
            if (selectedClient == null)
            {
                return;
            }

            ProgressWindow progress = new ProgressWindow("正在启动矿机...",
                () => {
                    OKResult r = selectedClient.ExecuteDaemon<OKResult>("-s start");
                    selectedClient.CurrentServiceStatus = MinerClient.ServiceStatus.Disconnected;
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.ToString());
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

            MinerClient selectedClient = minerDataGridItems.Select(row => row.Client).FirstOrDefault();
            if (selectedClient == null)
            {
                return;
            }

            ProgressWindow progress = new ProgressWindow("正在停止矿机...",
                () => {
                    OKResult r = selectedClient.ExecuteDaemon<OKResult>("-s stop");
                    selectedClient.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.ToString());
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

        private void UninstallSelectedMiner()
        {
            logger.Trace("Start StopSelectedMiner.");
            List<MinerDataGridItem> minerDataGridItems = GetSelectedRowsInDataGrid();
            if (minerDataGridItems.Count == 0)
            {
                return;
            }

            MinerClient selectedClient = minerDataGridItems.Select(r => r.Client).FirstOrDefault();
            if (selectedClient == null)
            {
                return;
            }

            if (MessageBox.Show("确定要卸载选定的矿机吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            MinerDataGridItem row = minerDataGridItems.FirstOrDefault();
            ProgressWindow progress = new ProgressWindow("正在卸载矿机...",
                () => {
                    try
                    {
                        OKResult r = selectedClient.ExecuteDaemon<OKResult>("-s uninstall");

                        selectedClient.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;
                        selectedClient.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                    }
                    catch (Exception ex)
                    {
                        selectedClient.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Unknown;
                        selectedClient.CurrentServiceStatus = MinerClient.ServiceStatus.Unknown;

                        logger.Error("Got Error while uninstalling miner service: " + ex.ToString());

                        throw;
                    }
                    finally
                    {
                        // Since sometimes the Windows Service will lock the config file for a while after uninstall, we will wait here
                        System.Threading.Thread.Sleep(5000);
                        selectedClient.DeleteBinaries();
                    }
                },
                (result) => {
                    if (result.HasError)
                    {
                        if (result.Exception is IOException)
                        {
                            MessageBox.Show("删除矿机目录错误，请到矿机目录下手动删除矿机文件。详细信息：" + result.Exception.ToString());
                            logger.Error("Got error while uninstalling miner with IOException: " + result.Exception.ToString());
                        }
                        else
                        {
                            MessageBox.Show("错误：" + result.Exception.ToString());
                            logger.Error("Something wrong while uninstalling miner: " + result.Exception.ToString());
                        }
                    }

                    // Removing client from ObjectModel first, and then Delete binaries might throw IO exception which should be ignored
                    minerManager.RemoveClient(selectedClient);
                    minerListGridItems.Remove(row);

                    this.RefreshMinerListGrid();
                });
            progress.ShowDialog();
        }

        private void RefreshMinerOperationButtonState()
        {
            List<MinerDataGridItem> selectedRows = GetSelectedRowsInDataGrid();

            this.btnMinerOperation.IsEnabled = (selectedRows.Count > 0);
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
    }
}
