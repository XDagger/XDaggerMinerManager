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
    /// Interaction logic for MachineConfigurationDataGrid.xaml
    /// </summary>
    public partial class MachineConfigurationDataGrid : UserControl
    {
        private ObservableCollection<MachineConfigurationDataGridItem> dataGridItems = null;
        
        public MachineConfigurationDataGrid()
        {
            InitializeComponent();

            dataGridItems = new ObservableCollection<MachineConfigurationDataGridItem>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.dataGrid.AutoGenerateColumns = false;
            this.dataGrid.AllowDrop = false;
            this.dataGrid.CanUserAddRows = false;
            this.dataGrid.CanUserReorderColumns = false;
            this.dataGrid.IsReadOnly = false;
            this.dataGrid.CanUserDeleteRows = false;
            this.dataGrid.CanUserResizeRows = false;
            this.dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            
            this.dataGrid.ItemsSource = dataGridItems;
            this.dataGrid.Items.Refresh();

            foreach (DataGridColumn col in dataGrid.Columns)
            {
                col.IsReadOnly = true;
            }
        }

        #region Public Methods

        public void AddItem(MinerClient client)
        {
            dataGridItems.Add(new MachineConfigurationDataGridItem(client));
            this.dataGrid.Items.Refresh();
        }

        public void ClearItems()
        {
            dataGridItems.Clear();
            this.dataGrid.Items.Refresh();
        }

        public void Refresh()
        {
            this.dataGrid.Items.Refresh();

        }

        #endregion

    }
}
