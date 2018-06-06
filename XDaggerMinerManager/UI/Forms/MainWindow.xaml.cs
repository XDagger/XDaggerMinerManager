using System;
using System.Collections.Generic;
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



        public MainWindow()
        {
            InitializeComponent();

            minerManager = MinerManager.GetInstance();
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
            RefreshMinerList();

        }

        private void RefreshMinerList()
        {
            minerListGrid.ItemsSource = minerManager?.ClientList;
        }
    }
}
