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
using XDaggerMinerManager.UI.Controls;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for BrowseNetworkWindow.xaml
    /// </summary>
    public partial class BrowseNetworkWindow : Window
    {
        private ObservableCollection<BrowseNetworkMachine> networkMachineData = new ObservableCollection<BrowseNetworkMachine>();

        private Action<List<MinerMachine>> resultHandler = null;

        private bool allowMultipleSelection = false;

        private List<MinerMachine> resultMachines = null;

        public BrowseNetworkWindow(bool allowMultipleSelection = false)
        {
            InitializeComponent();

            this.allowMultipleSelection = allowMultipleSelection;

            if (this.allowMultipleSelection)
            {
                dataGridMachines.SetDisplayColumns(MachineDataGrid.Columns.Selection, MachineDataGrid.Columns.FullName);
            }
            else
            {
                dataGridMachines.SetDisplayColumns(MachineDataGrid.Columns.FullName);
            }

            dataGridMachines.SelectionChanged += DataGridMachines_SelectionChanged;

            /*
            this.dataGridMachineList.AutoGenerateColumns = false;
            this.dataGridMachineList.AllowDrop = false;
            this.dataGridMachineList.CanUserAddRows = false;
            this.dataGridMachineList.CanUserReorderColumns = false;
            this.dataGridMachineList.IsReadOnly = false;
            this.dataGridMachineList.CanUserDeleteRows = false;
            this.dataGridMachineList.CanUserResizeRows = false;
            this.dataGridMachineList.DataContext = this;
            this.dataGridMachineList.SelectionUnit = DataGridSelectionUnit.FullRow;

            /// this.dataGridMachineList.Items.Clear();
            this.dataGridMachineList.ItemsSource = networkMachineData;
            this.dataGridMachineList.Items.Refresh();
            */

            /*
            if (!allowMultipleSelection)
            {
                resultMachines = new List<MinerMachine>();

                DataGridCheckBoxColumn selection = new DataGridCheckBoxColumn();
                selection.Header = "选择";
                selection.IsReadOnly = false;
                selection.IsThreeState = false;

                ///  this.dataGridMachineList.Columns.Add(selection);
            }
            */

        }

        private void DataGridMachines_SelectionChanged(object sender, EventArgs e)
        {
            List<MinerMachine> selectedMachines = dataGridMachines.GetSelectedMachines();
            this.btnConfirm.IsEnabled = (selectedMachines != null && selectedMachines.Count > 0);
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
            List<MinerMachine> selectedMachines = dataGridMachines.GetSelectedMachines();
            if (selectedMachines != null && selectedMachines.Count > 0)
            {
                this.resultHandler?.Invoke(selectedMachines);
                this.Close();
            }
            else
            {
                MessageBox.Show("请在列表中选择机器");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /*
            foreach (DataGridColumn col in dataGridMachineList.Columns)
            {
                if (col is DataGridTemplateColumn)
                {
                    col.Visibility = (allowMultipleSelection ? Visibility.Visible : Visibility.Hidden);
                }

                col.Header = BrowseNetworkMachine.TranslateHeaderName(col.Header.ToString());
                if (string.IsNullOrEmpty(col.Header.ToString()))
                {
                    col.Visibility = Visibility.Hidden;
                }

                col.IsReadOnly = true;
            }
            */

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
                        dataGridMachines.AddItem(machine);
                    }
                    
                }
            ).Execute();
        }

        private void dataGridMachineList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (!allowMultipleSelection)
            {
                ////this.btnConfirm.IsEnabled = (this.dataGridMachineList.SelectedItems.Count == 1);
            }
        }

        private void dataGridMachineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!allowMultipleSelection)
            {
                ////this.btnConfirm.IsEnabled = (this.dataGridMachineList.SelectedItems.Count == 1);
            }
        }

        private void dataGridMachineList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (allowMultipleSelection)
            {
                return;
            }

            ////if (this.dataGridMachineList.SelectedItem != null)
            {
                //// BrowseNetworkMachine machine = this.dataGridMachineList.SelectedItem as BrowseNetworkMachine;
                ////this.resultHandler?.Invoke(new List<MinerMachine>() { machine.GetMachine() });
                ////this.Close();
            }
        }

        private void dataGridMachineList_IsSelected_CheckChanged(object sender, RoutedEventArgs e)
        {
            resultMachines.Clear();

            /// this.dataGridMachineList..Items[0]

        }
    }
}
