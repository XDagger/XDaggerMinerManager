using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
    /// Interaction logic for WatsonWindow.xaml
    /// </summary>
    public partial class WatsonWindow : Window
    {
        public WatsonWindow()
        {
            InitializeComponent();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            WatsonDump watsonReport = new WatsonDump();
            try
            {
                if (!string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    watsonReport.Description = txtDescription.Text;
                }

                watsonReport.SendReport();
                MessageBox.Show("发送错误报告成功。", "提示", MessageBoxButton.OK);
            }
            catch (HttpException)
            {
                MessageBox.Show("发送错误报告失败，请检查网络设置。", "错误", MessageBoxButton.OK);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("发送错误报告失败，打包报告文件出现异常。", "错误", MessageBoxButton.OK);
            }
            finally
            {
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
