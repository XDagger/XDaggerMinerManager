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
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private Action mainAction = null;

        private Action<BackgroundWorkResult> callback = null;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        public ProgressWindow(string message, Action mainAction, Action<BackgroundWorkResult> callback)
            : this()
        {
            this.Title = message;

            if (mainAction == null)
            {
                throw new ArgumentNullException("Action should not be null.");
            }

            this.mainAction = mainAction;
            this.callback = callback;
        }

        private void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.progressBar.IsIndeterminate = true;
            
            BackgroundWork.CreateWork(
                this,
                () => { },
                this.mainAction,
                (result) => { this.Close(); this.callback?.Invoke(result); }
            ).Execute();
        }
    }
}
