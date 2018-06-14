using System;
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

        private ObservableCollection<MinerDataCell> minerListGridData = null;

        public MainWindow()
        {
            InitializeComponent();

            minerManager = MinerManager.GetInstance();
            
            
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

            minerManager.ClientList.Add(args.CreatedMiner);
            minerListGridData.Add(new MinerDataCell(args.CreatedMiner));
        }

        private void btnOperateMiner_Click(object sender, RoutedEventArgs e)
        {
        }

        private void minerListGrid_Loaded(object sender, RoutedEventArgs e)
        {
            minerListGridData = new ObservableCollection<MinerDataCell>();
            minerListGrid.ItemsSource = minerListGridData;
            minerListGrid.AllowDrop = false;
            minerListGrid.CanUserAddRows = false;
            minerListGrid.CanUserDeleteRows = false;
            minerListGrid.CanUserResizeRows = false;


            foreach (DataGridColumn col in minerListGrid.Columns)
            {
                col.Header = MinerDataCell.TranslateHeaderName(col.Header.ToString());
                col.IsReadOnly = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }

    
}
