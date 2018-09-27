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
using System.Windows.Shapes;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for UpdateMinerWindow.xaml
    /// </summary>
    public partial class UpdateMinerWindow : Window
    {
        private List<MinerClient> minerClients = null;

        public UpdateMinerWindow(List<MinerClient> minerClients)
        {
            InitializeComponent();

            if (minerClients == null || minerClients.Count == 0)
            {
                throw new ArgumentNullException("MinerClients is null or empty.");
            }

            this.minerClients = minerClients;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"确认要修改选定的{ minerClients.Count }个矿机的配置吗？", "确认", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            // Execute configure command

            result = MessageBox.Show("修改配置完成，需要重启矿机服务后才能生效。需要现在重启所有修改过的矿机吗？", "确认", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

        }
    }
}
