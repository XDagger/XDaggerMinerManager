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
        public event EventHandler<MachineNameEventArgs> OnFinished;

        public InputMachineName()
        {
            InitializeComponent();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            FinishDialog();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtMachineName.Focus();
        }

        private void txtMachineName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FinishDialog();
            }
        }

        private void FinishDialog()
        {
            if (string.IsNullOrWhiteSpace(txtMachineName.Text))
            {
                MessageBox.Show("请输入机器名称");
                return;
            }

            string machineName = txtMachineName.Text.Trim().ToUpper();
            this.Close();

            OnFinished?.Invoke(this, new MachineNameEventArgs(machineName));
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
