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
using XDaggerMinerManager.Configuration;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for UpdateMinerWindow.xaml
    /// </summary>
    public partial class UpdateMinerWindow : Window
    {
        private List<object> minerClients = null;

        private Logger logger = Logger.GetInstance();

        private UpdateMinerProperties properties = null;

        public UpdateMinerWindow(List<MinerClient> minerClients)
        {
            InitializeComponent();

            InitializeEthPoolAddresses();

            properties = new UpdateMinerProperties();

            if (minerClients == null || minerClients.Count == 0)
            {
                throw new ArgumentNullException("MinerClients is null or empty.");
            }

            this.minerClients = new List<object>();
            this.minerClients.AddRange(minerClients);

            lblSelectedMinerCount.Content = minerClients.Count.ToString();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if(cbxInstanceType.SelectedIndex < 0)
            {
                MessageBox.Show("请选择一个矿机类型.", "提示");
                return;
            }

            // Validate the config
            if (properties.InstanceType == MinerClient.InstanceTypes.XDagger)
            {
                if (!ValidateXDaggerConfig())
                {
                    return;
                }
            }
            else if (properties.InstanceType == MinerClient.InstanceTypes.Ethereum)
            {
                if (!ValidateEthConfig())
                {
                    return;
                }
            }
            
            MessageBoxResult mresult = MessageBox.Show($"确认要修改选定的{ minerClients.Count }个矿机的配置吗？", "确认", MessageBoxButton.YesNo);
            if (mresult == MessageBoxResult.No)
            {
                return;
            }

            // Execute configure command
            string configParameters = string.Empty;
            if (properties.InstanceType == MinerClient.InstanceTypes.XDagger)
            {
                configParameters = ComposeXDaggerConfig();
            }
            else if (properties.InstanceType == MinerClient.InstanceTypes.Ethereum)
            {
                configParameters = ComposeEthConfig();
            }

            logger.Trace("Composed configure command parameter: " + configParameters);
            
            ProgressWindow progress = new ProgressWindow("正在修改矿机配置...",
                this.minerClients,
                (obj) => {
                    MinerClient client = (MinerClient)obj;
                    OKResult exeResult = client.ExecuteDaemon<OKResult>(configParameters);

                    //After the config command success, update the status of the client
                    properties.UpdateClient(client);
                },
                (result) => {

                    this.HandleProgressResult(result.Result);
                }
                );
            progress.ShowDialog();
        }

        private string GetConfigInstanceType()
        {
            ComboBoxItem item = (ComboBoxItem)cbxInstanceType.SelectedItem;
            return item?.Content.ToString();
        }

        private void HandleProgressResult(int failureCount)
        {
            MessageBoxResult mresult = MessageBox.Show("修改配置完成，需要重启矿机服务后才能生效。需要现在重启所有修改过的矿机吗？", "确认", MessageBoxButton.YesNo);
            if (mresult == MessageBoxResult.No)
            {
                MinerManager.GetInstance().SaveCurrentInfo();
                this.Close();
                return;
            }

            // Restart the selected miners
            ProgressWindow progress = new ProgressWindow("正在重新启动矿机...",
                this.minerClients,
                (obj) => {
                    MinerClient client = (MinerClient)obj;
                    OKResult exeResult = client.ExecuteDaemon<OKResult>("-s stop");
                    exeResult = client.ExecuteDaemon<OKResult>("-s start");
                },
                (result) => {
                    MinerManager.GetInstance().SaveCurrentInfo();
                    this.Close();
                },
                shouldPromptSummary: false
                );
            progress.ShowDialog();
        }

        private bool ValidateXDaggerConfig()
        {
            string poolAddress = txtXDaggerPoolAddress.Text.Trim();
            if (!string.IsNullOrWhiteSpace(poolAddress) && !poolAddress.Contains(":"))
            {
                MessageBox.Show("矿池地址格式错误");
                return false;
            }

            properties.XDaggerPoolAddress = poolAddress;

            string walletAddress = txtXDaggerWallet.Text.Trim();
            if (!string.IsNullOrWhiteSpace(walletAddress) && walletAddress.Length != 32)
            {
                MessageBox.Show("钱包必须为长度32位的字母与数字组合");
                return false;
            }

            properties.XDaggerWalletAddress = walletAddress;

            return true;
        }

        private bool ValidateEthConfig()
        {
            string ethWalletAddress = txtEthWallet.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ethWalletAddress) && !ethWalletAddress.StartsWith("0x"))
            {
                MessageBox.Show("钱包必须是以0x开头的32位字符串");
                return false;
            }

            if (cbxTargetEthPool.SelectedIndex < 0)
            {
                MessageBox.Show("请选择矿池类型");
                return false;
            }

            if (cbxTargetEthPoolHost.SelectedIndex < 0)
            {
                MessageBox.Show("请选择矿池地址");
                return false;
            }

            properties.EthFullPoolAddress = ethWalletAddress;

            return true;
        }

        private string ComposeXDaggerConfig()
        {
            StringBuilder builder = new StringBuilder();
            bool isFirstParameter = true;

            builder.Append(" -c \"{ ");

            if(!string.IsNullOrWhiteSpace(properties.XDaggerWalletAddress))
            {
                builder.AppendFormat(" 'XDaggerWallet':'{0}' ", properties.XDaggerWalletAddress);
                isFirstParameter = false;
            }

            if (!string.IsNullOrWhiteSpace(properties.XDaggerPoolAddress))
            {
                if (!isFirstParameter)
                {
                    builder.Append(", ");
                }
                builder.AppendFormat(" 'XDaggerPoolAddress':'{0}' ", properties.XDaggerPoolAddress);
            }

            builder.Append(" }\"");

            return builder.ToString();
        }

        private string ComposeEthConfig()
        {
            EthMinerPoolHelper ethPoolHelper = new EthMinerPoolHelper();
            ethPoolHelper.Index = (EthMinerPoolHelper.PoolIndex)cbxTargetEthPool.SelectedIndex;
            ethPoolHelper.HostIndex = cbxTargetEthPoolHost.SelectedIndex;
            ethPoolHelper.EthWalletAddress = txtEthWallet.Text.Trim();
            ethPoolHelper.EmailAddress = txtEmailAddress.Text.Trim();
            ethPoolHelper.WorkerName = "XDaggerMinerBatchWorkerName";       // TODO: Should update to worker name template

            properties.EthFullPoolAddress = ethPoolHelper.GeneratePoolAddress();

            StringBuilder builder = new StringBuilder();
            builder.Append(" -c \"{ ");
            builder.AppendFormat(" 'EthPoolAddress':'{0}' ", properties.EthFullPoolAddress);
            builder.Append(" }\"");

            return builder.ToString();
        }

        private void InitializeEthPoolAddresses()
        {
            logger.Trace("InitializeEthPoolAddresses.");

            cbxTargetEthPool.Items.Clear();
            foreach (string ethPoolName in EthMinerPoolHelper.PoolDisplayNames)
            {
                cbxTargetEthPool.Items.Add(ethPoolName);
            }

            cbxTargetEthPoolHost.Items.Clear();
        }

        private void cbxInstanceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetConfigInstanceType() == "XDagger")
            {
                properties.InstanceType = MinerClient.InstanceTypes.XDagger;

                grdXDaggerConfig.Visibility = Visibility.Visible;
                grdEthConfig.Visibility = Visibility.Hidden;
            }
            else
            {
                properties.InstanceType = MinerClient.InstanceTypes.Ethereum;

                grdXDaggerConfig.Visibility = Visibility.Hidden;
                grdEthConfig.Visibility = Visibility.Visible;
            }
        }

        private void cbxTargetEthPool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbxTargetEthPoolHost.Items.Clear();
            if (cbxTargetEthPool.SelectedIndex < 0 || cbxTargetEthPool.SelectedIndex >= EthMinerPoolHelper.PoolHostUrls.Count)
            {
                return;
            }

            foreach (string ethPoolHost in EthMinerPoolHelper.PoolHostUrls[cbxTargetEthPool.SelectedIndex])
            {
                cbxTargetEthPoolHost.Items.Add(ethPoolHost);
            }
        }
    }
}
