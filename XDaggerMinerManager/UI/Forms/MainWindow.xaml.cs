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

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MinerManager minerManager = null;

        private ObservableCollection<MinerDataCell> minerListGridData = new ObservableCollection<MinerDataCell>();

        public MainWindow()
        {
            InitializeComponent();

            minerManager = MinerManager.GetInstance();

            InitializeUIData();
            
            //// clients.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(dataChangedEvent);
        }

        private void dataChangedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            MessageBox.Show("Added item!");
        }

        private void btnAddMiner_Click(object sender, RoutedEventArgs e)
        {
            AddMinerWizardWindow addMinerWizard = new AddMinerWizardWindow();
            addMinerWizard.MinerCreated += OnMinerCreated;
            addMinerWizard.ShowDialog();
        }

        public void OnMinerCreated(object sender, EventArgs e)
        {
            MinerCreatedEventArgs args = e as MinerCreatedEventArgs;

            if (args == null || args.CreatedMiner == null)
            {
                return;
            }

            minerManager.AddClient(args.CreatedMiner);
            RefreshMinerListGrid();
        }

        private void btnOperateMiner_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InitializeUIData()
        {
            RefreshMinerListGrid();
        }

        private void RefreshMinerListGrid()
        {
            minerListGridData.Clear();
            foreach (MinerClient client in minerManager.ClientList)
            {
                minerListGridData.Add(new MinerDataCell(client));
            }
        }

        private void minerListGrid_Loaded(object sender, RoutedEventArgs e)
        {
            minerListGrid.ItemsSource = minerListGridData;
            minerListGrid.AllowDrop = false;
            minerListGrid.CanUserAddRows = false;
            minerListGrid.CanUserDeleteRows = false;
            minerListGrid.CanUserResizeRows = false;
            
            foreach (DataGridColumn col in minerListGrid.Columns)
            {
                if (col.Header.ToString() == "MinerName")
                {
                    col.Visibility = Visibility.Collapsed;
                }

                col.Header = MinerDataCell.TranslateHeaderName(col.Header.ToString());
                col.IsReadOnly = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void menuStartMiner_Click(object sender, RoutedEventArgs e)
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();
            ProgressWindow progress = new ProgressWindow("正在启动矿机...",
                () => {
                    MinerClient c = selectedClients.FirstOrDefault();
                    if (c != null)
                    {
                        ExecutionResult<OKResult> r = c.ExecuteDaemon<OKResult>("-s start");

                        if (!r.HasError)
                        {
                            c.CurrentServiceStatus = MinerClient.ServiceStatus.Started;
                        }
                        else
                        {
                            /// throw new Exception(r.Code + "|" + r.ErrorMessage);
                        }
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

        private void menuStopMiner_Click(object sender, RoutedEventArgs e)
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();
            ProgressWindow progress = new ProgressWindow("正在停止矿机...",
                () => {
                    MinerClient c = selectedClients.FirstOrDefault();
                    if (c != null)
                    {
                        ExecutionResult<OKResult> r = c.ExecuteDaemon<OKResult>("-s stop");

                        if (!r.HasError)
                        {
                            c.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                        }
                        else
                        {
                            /// throw new Exception(r.Code + "|" + r.ErrorMessage);
                        }
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

        private void menuUninstallMiner_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要卸载选定的矿机吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();
            ProgressWindow progress = new ProgressWindow("正在卸载矿机...",
                () => {
                    MinerClient c = selectedClients.FirstOrDefault();
                    if (c != null)
                    {
                        ExecutionResult<OKResult> r = c.ExecuteDaemon<OKResult>("-s uninstall");

                        if (!r.HasError)
                        {
                            c.CurrentDeploymentStatus = MinerClient.DeploymentStatus.Downloaded;
                            c.CurrentServiceStatus = MinerClient.ServiceStatus.Stopped;
                        }
                        else
                        {
                            /// throw new Exception(r.Code + "|" + r.ErrorMessage);
                        }

                        c.DeleteBinaries();
                        minerManager.RemoveClient(c);
                    }
                },
                (result) => {
                    if (result.HasError)
                    {
                        MessageBox.Show("错误：" + result.Exception.ToString());
                    }

                    this.RefreshMinerListGrid(); }
                );
            progress.ShowDialog();
        }

        private void minerListGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();

            bool containsStartedMiner = false;
            bool containsStoppedMiner = false;
            bool containsNotReadyMiner = false;

            foreach(MinerClient client in selectedClients)
            {
                containsStartedMiner |= (client.CurrentServiceStatus == MinerClient.ServiceStatus.Started);
                containsStoppedMiner |= (client.CurrentServiceStatus == MinerClient.ServiceStatus.Stopped);
                containsNotReadyMiner |= (client.CurrentDeploymentStatus != MinerClient.DeploymentStatus.Ready);
            }

            this.menuStopMiner.IsEnabled = !containsNotReadyMiner && containsStartedMiner;
            this.menuStartMiner.IsEnabled = !containsNotReadyMiner && containsStoppedMiner;
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

                MinerClient client = minerManager.ClientList.FirstOrDefault((c) => { return (c.MachineName == cell.MinerName); });
                if (client != null)
                {
                    selectedClients.Add(client);
                }
            }

            return selectedClients;
        }
    }


    
}
