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

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for InputMachineName.xaml
    /// </summary>
    public partial class InputMachineName : Window
    {
        public event EventHandler<EventArgs> OnFinished;

        public InputMachineName()
        {
            InitializeComponent();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMachineName.Text))
            {
                MessageBox.Show("请输入机器名称");
                return;
            }

            string machineName = txtMachineName.Text.Trim().ToUpper();

            OnFinished?.Invoke(this, new MachineNameEventArgs(machineName));
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class MachineNameEventArgs : EventArgs
    {
        public MachineNameEventArgs(string machineName)
        {
            this.MachineName = machineName;
        }

        public string MachineName;
    }
}
