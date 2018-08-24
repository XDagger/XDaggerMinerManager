using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using XDaggerMinerManager.ObjectModel;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for BrowseNetworkWindow.xaml
    /// </summary>
    public partial class BrowseNetworkWindow : Window
    {
        private ObservableCollection<MinerMachine> networkMachineData = new ObservableCollection<MinerMachine>();

        private Action<MinerMachine> resultHandler = null;

        public BrowseNetworkWindow()
        {
            InitializeComponent();

            this.dataGridMachineList.ItemsSource = networkMachineData;
            this.dataGridMachineList.AllowDrop = false;
            this.dataGridMachineList.CanUserAddRows = false;
            this.dataGridMachineList.CanUserReorderColumns = false;
            this.dataGridMachineList.IsReadOnly = true;
            this.dataGridMachineList.CanUserDeleteRows = false;
            this.dataGridMachineList.CanUserResizeRows = false;
            
        }

        public void SetResultHandler(Action<MinerMachine> resultHandler)
        {
            this.resultHandler = resultHandler;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataGridMachineList.SelectedItem != null)
            {
                this.resultHandler?.Invoke(this.dataGridMachineList.SelectedItem as MinerMachine);
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (DataGridColumn col in dataGridMachineList.Columns)
            {
                col.Header = MinerMachine.TranslateHeaderName(col.Header.ToString());
                col.IsReadOnly = true;
            }

            BackgroundWork<List<MinerMachine>>.CreateWork(
                this,
                () => {
                    prbIndicator.IsIndeterminate = true;
                    prbIndicator.Visibility = Visibility.Visible;
                },
                () => {
                    // Starting to query Machines in the local network
                    return NetworkUtils.DetectMachinesInLocalNetwork();
                },
                (taskResult) => {

                    prbIndicator.IsIndeterminate = false;
                    prbIndicator.Visibility = Visibility.Hidden;

                    if (taskResult.HasError)
                    {
                        MessageBox.Show("检测局域网错误: " + taskResult.Exception.ToString());
                        return;
                    }

                    foreach(MinerMachine machine in taskResult.Result)
                    {
                        this.networkMachineData.Add(machine);
                    }
                }
            ).Execute();
        }

        private void dataGridMachineList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            this.btnConfirm.IsEnabled = (this.dataGridMachineList.SelectedItems.Count == 1);
        }

        private void dataGridMachineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnConfirm.IsEnabled = (this.dataGridMachineList.SelectedItems.Count == 1);
        }

        private void dataGridMachineList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.dataGridMachineList.SelectedItem != null)
            {
                this.resultHandler?.Invoke(this.dataGridMachineList.SelectedItem as MinerMachine);
                this.Close();
            }

           
        }
    }
}
