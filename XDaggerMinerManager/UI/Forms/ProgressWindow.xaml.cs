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
        private Action<object> mainActionTemplate = null;

        private List<object> subjectList = null;

        private Action<BackgroundWorkResult> callback = null;

        private bool shouldPromptSummary = false;

        private string message = string.Empty;

        private int completedActionCount = 0;

        private int failedActionCount = 0;

        private int TotalActionCount
        {
            get
            {
                return this.subjectList?.Count ?? 0;
            }
        }

        public ProgressWindow()
        {
            InitializeComponent();
        }

        public ProgressWindow(string message, Action<BackgroundWorkResult> callback, bool shouldPromptSummary)
        {
            this.message = message;
            this.callback = callback;
            this.shouldPromptSummary = shouldPromptSummary;
        }

        public ProgressWindow(string message, Action mainAction, Action<BackgroundWorkResult> callback, bool shouldPromptSummary = true)
            : this(message, callback, shouldPromptSummary)
        {
            this.subjectList = new List<object>() { null };
            this.mainActionTemplate = (obj) => { mainAction(); };
            
            UpdateTitle();
        }

        public ProgressWindow(string message, List<object> list, Action<object> mainActionTemplate, Action<BackgroundWorkResult> callback, bool shouldPromptSummary = true)
            : this(message, callback, shouldPromptSummary)
        {
            this.subjectList = list;
            this.mainActionTemplate = mainActionTemplate ?? throw new ArgumentNullException("Action should not be null.");
            
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            if (TotalActionCount <= 1)
            {
                this.Title = this.message;
            }
            else
            {
                this.Title = string.Format("{0} ({1}/{2})", this.message, this.completedActionCount, this.TotalActionCount);
            }
        }

        private void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.progressBar.IsIndeterminate = true;

        }

        private void BackgroundWorkCompleted(object subject)
        {
            UpdateTitle();

            this.completedActionCount++;

            if (this.completedActionCount >= this.TotalActionCount)
            {
                // Finish this window and close it
                if (shouldPromptSummary)
                {
                    this.PromptSummary();
                }

                this.Close();

                BackgroundWorkResult result = new BackgroundWorkResult(this.TotalActionCount - this.failedActionCount);
                this.callback?.Invoke(result);
            }
        }

        private void PromptSummary()
        {
            int successActionCount = this.TotalActionCount - this.failedActionCount;

            string message = string.Empty;
            if (this.TotalActionCount > 1)
            {
                if (this.failedActionCount > 0)
                {
                    message = string.Format("批量执行任务共计{0}个, 其中成功{1}个，失败{2}个，具体失败信息请查阅系统日志。", TotalActionCount, successActionCount, failedActionCount);
                }
                else
                {
                    message = string.Format("批量执行共计{0}个全部执行成功。", TotalActionCount);
                }
            }
            else
            {
                if (this.failedActionCount > 0)
                {
                    message = "执行任务失败，具体失败信息请查阅系统日志。";
                }
                else
                {
                    message = "执行任务成功。";
                }
            }
        }
    }
}
