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

        private Action<List<MinerMachine>> resultHandler = null;

        private bool allowMultipleSelection = true;

        private List<MinerMachine> resultMachines = null;

        public BrowseNetworkWindow(bool allowMultipleSelection = false)
        {
            InitializeComponent();

            this.allowMultipleSelection = allowMultipleSelection;

            if (allowMultipleSelection)
            {
                resultMachines = new List<MinerMachine>();

                DataGridCheckBoxColumn selection = new DataGridCheckBoxColumn();
                selection.Header = "选择";
                selection.IsReadOnly = false;
                selection.IsThreeState = false;

                this.dataGridMachineList.Columns.Add(selection);
            }

            this.dataGridMachineList.ItemsSource = networkMachineData;
            this.dataGridMachineList.AllowDrop = false;
            this.dataGridMachineList.CanUserAddRows = false;
            this.dataGridMachineList.CanUserReorderColumns = false;
            this.dataGridMachineList.IsReadOnly = false;
            this.dataGridMachineList.CanUserDeleteRows = false;
            this.dataGridMachineList.CanUserResizeRows = false;
        }

        public void SetResultHandler(Action<List<MinerMachine>> resultHandler)
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
                MinerMachine machine = this.dataGridMachineList.SelectedItem as MinerMachine;
                this.resultHandler?.Invoke(new List<MinerMachine>() { machine });
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (DataGridColumn col in dataGridMachineList.Columns)
            {
                if (col is DataGridCheckBoxColumn)
                {
                    continue;
                }

                col.Header = MinerMachine.TranslateHeaderName(col.Header.ToString());
                if (string.IsNullOrEmpty(col.Header.ToString()))
                {
                    col.Visibility = Visibility.Hidden;
                }

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
            if (!allowMultipleSelection)
            {
                this.btnConfirm.IsEnabled = (this.dataGridMachineList.SelectedItems.Count == 1);
            }
        }

        private void dataGridMachineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!allowMultipleSelection)
            {
                this.btnConfirm.IsEnabled = (this.dataGridMachineList.SelectedItems.Count == 1);
            }
        }

        private void dataGridMachineList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (allowMultipleSelection)
            {
                return;
            }

            if (this.dataGridMachineList.SelectedItem != null)
            {
                MinerMachine machine = this.dataGridMachineList.SelectedItem as MinerMachine;
                this.resultHandler?.Invoke(new List<MinerMachine>() { machine });
                this.Close();
            }
        }

        private void dataGridMachineList_IsSelected_CheckChanged(object sender, RoutedEventArgs e)
        {
            resultMachines.Clear();

        }
    }
}
