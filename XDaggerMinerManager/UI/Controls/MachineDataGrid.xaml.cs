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
using System.Windows.Navigation;
using System.Windows.Shapes;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI.Controls
{
    /// <summary>
    /// Interaction logic for MachineDataGrid.xaml
    /// </summary>
    public partial class MachineDataGrid : UserControl
    {
        public enum Columns
        {
            Selection,
            FullName,
            IpAddressV4,
        }

        public event EventHandler<EventArgs> SelectionChanged = null;

        private List<Columns> displayColumns = null;
        
        private ObservableCollection<MachineDataGridItem> dataGridItems = null;

        public MachineDataGrid()
        {
            InitializeComponent();

            displayColumns = new List<Columns>();
            dataGridItems = new ObservableCollection<MachineDataGridItem>();
        }

        public MachineDataGrid(params Columns[] columns)
        {
            InitializeComponent();

            displayColumns.AddRange(columns);

            dataGridItems = new ObservableCollection<MachineDataGridItem>();
        }

        public MachineDataGrid(ObservableCollection<MachineDataGridItem> items, params Columns[] columns)
        {
            InitializeComponent();

            displayColumns.AddRange(columns);

            dataGridItems = items ?? new ObservableCollection<MachineDataGridItem>();
        }

        public void SetDisplayColumns(params Columns[] columns)
        {
            displayColumns.AddRange(columns);
        }

        /*
        public bool CanUserEdit
        {
            get
            {
                return false;
            }
            set
            {
                dataGrid.Columns[1].IsReadOnly = !value;
                this.dataGrid.CanUserAddRows = value;
                this.dataGrid.CanUserDeleteRows = value;
            }
        }
        */

        private void usercontrol_Loaded(object sender, RoutedEventArgs e)
        {
            this.dataGrid.AutoGenerateColumns = false;
            this.dataGrid.AllowDrop = false;
            this.dataGrid.CanUserAddRows = false;
            this.dataGrid.CanUserReorderColumns = false;
            this.dataGrid.IsReadOnly = false;
            this.dataGrid.CanUserDeleteRows = false;
            this.dataGrid.CanUserResizeRows = false;
            this.dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;

            /// this.dataGridMachineList.Items.Clear();
            this.dataGrid.ItemsSource = dataGridItems;
            this.dataGrid.Items.Refresh();

            // Show the columns
            if (!displayColumns.Contains(Columns.Selection))
            {
                dataGrid.Columns[0].Visibility = Visibility.Hidden;
            }
            if (!displayColumns.Contains(Columns.FullName))
            {
                dataGrid.Columns[1].Visibility = Visibility.Hidden;
            }
            if (!displayColumns.Contains(Columns.IpAddressV4))
            {
                dataGrid.Columns[2].Visibility = Visibility.Hidden;
            }

            foreach (DataGridColumn col in dataGrid.Columns)
            {
                col.IsReadOnly = true;
            }
        }

        private void selection_CheckChanged(object sender, RoutedEventArgs e)
        {
            SelectionChanged?.Invoke(this, new EventArgs());
        }

        public List<MinerMachine> GetAllMachines()
        {
            return dataGridItems.Select(item => item.GetMachine()).ToList();
        }

        public List<MinerMachine> GetSelectedMachines()
        {
            if (displayColumns.Contains(Columns.Selection))
            {
                // Multiple selection
                return dataGridItems.Where(item => item.IsSelected).Select(item => item.GetMachine()).ToList();
            }
            else
            {
                // Single selection
                List<MinerMachine> selectedList = new List<MinerMachine>();
                foreach(MachineDataGridItem item in dataGrid.SelectedItems)
                {
                    if (item != null)
                    {
                        selectedList.Add(item.GetMachine());
                    }
                }

                return selectedList;
            }
        }

        public void AddItem(MinerMachine machine)
        {
            dataGridItems.Add(new MachineDataGridItem(machine));
            this.dataGrid.Items.Refresh();
        }

        public void RemoveItem(MinerMachine machine)
        {
            for(int i = 0; i < dataGridItems.Count; i++)
            {
                if (dataGridItems[i].FullName.Equals(machine.FullName))
                {
                    dataGridItems.RemoveAt(i);
                    return;
                }
            }
            this.dataGrid.Items.Refresh();
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!displayColumns.Contains(Columns.Selection))
            {
                SelectionChanged?.Invoke(this, new EventArgs());
            }
        }
    }
}
