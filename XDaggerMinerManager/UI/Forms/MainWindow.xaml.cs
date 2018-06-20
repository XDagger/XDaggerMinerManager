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
            minerListGridData.Add(new MinerDataCell(args.CreatedMiner));
        }

        private void btnOperateMiner_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InitializeUIData()
        {
            foreach(MinerClient client in minerManager.ClientList)
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
            
        }

        private void menuStopMiner_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuUninstallMiner_Click(object sender, RoutedEventArgs e)
        {

        }

        private void minerListGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            List<MinerClient> selectedClients = GetSelectedClientsInDataGrid();

            bool containsStartedMiner = false;
            bool containsStoppedMiner = false;

            foreach(MinerClient client in selectedClients)
            {
                containsStartedMiner |= (client.CurrentServiceStatus == MinerClient.ServiceStatus.Started);
                containsStoppedMiner |= (client.CurrentServiceStatus == MinerClient.ServiceStatus.Stopped);
            }

            this.menuStopMiner.IsEnabled = containsStartedMiner;
            this.menuStartMiner.IsEnabled = containsStoppedMiner;
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
