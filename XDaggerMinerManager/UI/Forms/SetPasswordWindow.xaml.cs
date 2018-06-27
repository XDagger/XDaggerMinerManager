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
    /// Interaction logic for SetPasswordWindow.xaml
    /// </summary>
    public partial class SetPasswordWindow : Window
    {
        private Action<string> setPasswordCallback = null;

        public string PasswordValue
        {
            get; private set;
        }

        public SetPasswordWindow()
        {
            InitializeComponent();

            this.PasswordValue = string.Empty;
        }

        public SetPasswordWindow(Action<string> callback)
        {
            this.setPasswordCallback = callback;
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string password = tBxPassword.Password;
            string confirmPassword = tBxConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("密码不可以为空");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("两次输入密码不一致");
                return;
            }

            this.PasswordValue = password;
            this.Close();
        }

        private void passwordWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void ShowError(string message)
        {
            this.lblErrorMessage.Content = message;
        }

        private void tBxConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.lblErrorMessage.Content = string.Empty;
        }

        private void tBxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.lblErrorMessage.Content = string.Empty;
        }
    }
}
