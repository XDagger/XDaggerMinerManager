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
using XDaggerMinerManager.Configuration;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for LockWindow.xaml
    /// </summary>
    public partial class LockWindow : Window
    {
        private static readonly string LockWindowTitle = "请输入密码解除锁定";

        private Window parentWindow = null;
        private bool passwordConfirmed = false;



        public LockWindow()
        {
            InitializeComponent();
        }

        public LockWindow(Window parent) : this()
        {
            this.parentWindow = parent ?? throw new ArgumentNullException("parent window");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ManagerInfo info = ManagerInfo.Current;

            string password = this.tBxPassword.Password;
            if (!info.IsPasswordMatch(password))
            {
                ShowError("密码错误");
                return;
            }

            this.parentWindow.Show();

            this.passwordConfirmed = true;
            this.Close();
        }

        private void ShowError(string message)
        {
            this.Title = string.Format("{0} - {1}", LockWindowTitle, message);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.passwordConfirmed)
            {
                e.Cancel = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.parentWindow.Hide();
        }

        private void tBxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.Title = LockWindowTitle;
        }
    }
}
