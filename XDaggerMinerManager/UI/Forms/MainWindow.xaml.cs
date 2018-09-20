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

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Properties

        private MinerManager minerManager = null;

        private List<MinerDataCell> minerListGridData = new List<MinerDataCell>();

        private ObservableCollection<MinerDataCell> minerListGridData2 = new ObservableCollection<MinerDataCell>();

        private bool isTimerRefreshingBusy = false;

        private static readonly string MinerStatisticsSummaryTemplate = @"当前矿机数：{0}台  上线：{1}台  下线：{2}台  主算力：{3:0.000} Mps";

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            minerManager = MinerManager.GetInstance();
            InitializeUIData();
        }

        private void dataChangedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            MessageBox.Show("Added item!");
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
            minerListGrid.ItemsSource = minerListGridData2;

            
            minerManager.ClientStatusChanged += MinerListGrid_StatusChanged;
        }

        private void MinerListGrid_StatusChanged(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => RefreshMinerListGrid() );
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up a timer to trigger every second.  
            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimerRefresh);
            timer.Start();
            isTimerRefreshingBusy = false;
        }

        private void btnMinerOperation_Click(object sender, RoutedEventArgs e)
        {
            // Do nothing here
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

        private void menuStartMiner_Click(object sender, RoutedEventArgs e)
        {
            StartSelectedMiner();
        }

        private void menuStopMiner_Click(object sender, RoutedEventArgs e)
        {
            StopSelectedMiner();
        }

        private void menuUninstallMiner_Click(object sender, RoutedEventArgs e)
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

        private void minerListGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            List<MinerDataCell> selectedRows = GetSelectedRowsInDataGrid();
            IEnumerable<MinerClient> selectedClients = selectedRows.Select(row => { return row.Client; });

            bool containsStartedMiner = false;
            bool containsStoppedMiner = false;
            bool containsNotReadyMiner = false;

            foreach (MinerClient client in selectedClients)
            {
                containsStartedMiner |= client.IsServiceStatusRunning();
                containsStoppedMiner |= !client.IsServiceStatusRunning();
                containsNotReadyMiner |= (client.CurrentDeploymentStatus != MinerClient.DeploymentStatus.Ready);
            }

            this.menuStopMiner.IsEnabled = !containsNotReadyMiner && containsStartedMiner;
            this.menuStartMiner.IsEnabled = !containsNotReadyMiner && containsStoppedMiner;
        }
        
        private void btnMinerOperation_Opened(object sender, RoutedEventArgs e)
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();

            if (selectedClients == null || selectedClients.Count == 0)
            {
                this.operStartMiner.IsEnabled = false;
                this.operStopMiner.IsEnabled = false;
                this.operUninstallMiner.IsEnabled = false;
                return;
            }
            
            bool containsStartedMiner = false;
            bool containsStoppedMiner = false;
            bool containsNotReadyMiner = false;

            foreach (MinerClient client in selectedClients)
            {
                containsStartedMiner |= client.IsServiceStatusRunning();
                containsStoppedMiner |= !client.IsServiceStatusRunning();
                containsNotReadyMiner |= (client.CurrentDeploymentStatus != MinerClient.DeploymentStatus.Ready);
            }

            this.operStartMiner.IsEnabled = !containsNotReadyMiner && containsStoppedMiner;
            this.operStopMiner.IsEnabled = !containsNotReadyMiner && containsStartedMiner;
            this.operUninstallMiner.IsEnabled = true;
        }

        #endregion

        #region UI Common Functions

        private void InitializeUIData()
        {
            this.Title = string.Format("XDagger Miner Manager Platform ({0})", minerManager.Version);

            foreach (MinerClient client in minerManager.ClientList)
            {
                minerListGridData.Add(new MinerDataCell(client));
                minerListGridData2.Add(new MinerDataCell(client));
            }

            RefreshMinerListGrid();
            RefreshMinerOperationButtonState();

        }

        private void RefreshMinerListGrid()
        {
            // Ensure to bind the data
            if (minerListGridData.Count > 0 && minerListGrid.ItemsSource == null)
            {
                //minerListGrid.ItemsSource = minerListGridData;
            }
            
            minerListGrid.Items.Refresh();

            /*
            int savedSelectedIndex = minerListGrid.SelectedIndex;

            minerListGridData.Clear();
            //// minerListGrid.Items.Clear();

            foreach (MinerClient client in minerManager.ClientList)
            {
                totalHashrate += client.CurrentHashRate;
                minerListGridData.Add(new MinerDataCell(client));
                //// minerListGrid.Items.Add(new MinerDataCell(client));
            }
            
            if (savedSelectedIndex >= 0)
            {
                minerListGrid.SelectedIndex = savedSelectedIndex;
            }
            */
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
            MinerCreatedEventArgs args = e as MinerCreatedEventArgs;

            if (args == null || args.CreatedMiner == null)
            {
                return;
            }

            minerManager.AddClient(args.CreatedMiner);
            minerListGridData.Add(new MinerDataCell(args.CreatedMiner));

            RefreshMinerListGrid();
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

            }
            finally
            {
                isTimerRefreshingBusy = false;
            }
        }

        private void StartSelectedMiner()
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();
            ProgressWindow progress = new ProgressWindow("正在启动矿机...",
                () => {
                    MinerClient c = selectedClients.FirstOrDefault();
                    if (c != null)
                    {
                        OKResult r = c.ExecuteDaemon<OKResult>("-s start");
                        c.CurrentServiceStatus = MinerClient.ServiceStatus.Disconnected;
                    }
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.ToString());
                    }

                    this.RefreshMinerListGrid();
                }
                );
            progress.ShowDialog();
        }

        private void StopSelectedMiner()
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();
            ProgressWindow progress = new ProgressWindow("正在停止矿机...",
                () => {
                    MinerClient c = selectedClients.FirstOrDefault();
                    if (c != null)
                    {
                        OKResult r = c.ExecuteDaemon<OKResult>("-s stop");
                        c.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                    }
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.ToString());
                    }

                    this.RefreshMinerListGrid();
                }
                );
            progress.ShowDialog();
        }

        private void UninstallSelectedMiner()
        {
            if (MessageBox.Show("确定要卸载选定的矿机吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            //List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();
            List<MinerDataCell> selectedRows = GetSelectedRowsInDataGrid();

            ProgressWindow progress = new ProgressWindow("正在卸载矿机...",
                () => {
                    MinerDataCell row = selectedRows.FirstOrDefault();
                    MinerClient c = row?.Client;
                    if (c != null)
                    {
                        try
                        {
                            OKResult r = c.ExecuteDaemon<OKResult>("-s uninstall");

                            c.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;
                            c.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                        }
                        catch (Exception ex)
                        {
                            c.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Unknown;
                            c.CurrentServiceStatus = MinerClient.ServiceStatus.Unknown;
                            /// throw new Exception(r.Code + "|" + r.ErrorMessage);
                            /// 
                            throw;
                        }
                        finally
                        {
                            // Since sometimes the Windows Service will lock the config file for a while after uninstall, we will wait here
                            System.Threading.Thread.Sleep(5000);

                            // Removing client from ObjectModel first, and then Delete binaries might throw IO exception which should be ignored
                            minerManager.RemoveClient(c);
                            minerListGridData.Remove(row);

                            c.DeleteBinaries();
                        }
                    }
                },
                (result) => {
                    if (result.HasError)
                    {
                        if (result.Exception is IOException)
                        {
                            MessageBox.Show("删除矿机目录错误，请到矿机目录下手动删除矿机文件。详细信息：" + result.Exception.ToString());
                        }
                        else
                        {
                            MessageBox.Show("错误：" + result.Exception.ToString());
                        }
                    }

                    this.RefreshMinerListGrid();
                });
            progress.ShowDialog();
        }


        private void RefreshMinerOperationButtonState()
        {
            List<MinerDataCell> selectedRows = GetSelectedRowsInDataGrid();

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

        private List<MinerDataCell> GetSelectedRowsInDataGrid()
        {
            return this.minerListGridData2.Where(row => row.IsSelected).ToList();
        }

        private List<MinerClient> GetSelectedClientsInDataGrid()
        {
            List<MinerClient> selectedClients = new List<MinerClient>();
            Collection.IList selectedItems = this.minerListGrid.SelectedItems;
            
            foreach (object obj in selectedItems)
            {
                MinerDataCell cell = (MinerDataCell)obj;
                if (cell == null)
                {
                    continue;
                }

                MinerClient client = minerManager.ClientList.FirstOrDefault((c) => { return (c.Machine?.FullMachineName + c.InstanceName == cell.MinerName); });
                if (client != null)
                {
                    selectedClients.Add(client);
                }
            }

            return selectedClients;
        }

        #endregion

        private void cbxSelectMiners_Checked(object sender, RoutedEventArgs e)
        {
            foreach(MinerDataCell cell in minerListGridData2)
            {
                cell.IsSelected = true;
            }
            this.minerListGrid.Items.Refresh();

            RefreshMinerOperationButtonState();
        }

        private void cbxSelectMiners_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (MinerDataCell cell in minerListGridData2)
            {
                cell.IsSelected = false;
            }
            this.minerListGrid.Items.Refresh();

            RefreshMinerOperationButtonState();
        }

        private void minerListGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            RefreshMinerOperationButtonState();
        }
    }
}
