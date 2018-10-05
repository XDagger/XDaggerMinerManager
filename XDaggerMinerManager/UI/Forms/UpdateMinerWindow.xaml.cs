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

        private Dictionary<string, bool> newInstanceIdDictionary = null;

        public UpdateMinerWindow(List<MinerClient> minerClients)
        {
            InitializeComponent();

            this.grdEthConfig.Visibility = Visibility.Hidden;
            this.grdXDaggerConfig.Visibility = Visibility.Hidden;

            InitializeEthPoolAddresses();

            properties = new UpdateMinerProperties();

            if (minerClients == null || minerClients.Count == 0)
            {
                throw new ArgumentNullException("MinerClients is null or empty.");
            }

            this.newInstanceIdDictionary = new Dictionary<string, bool>();

            this.minerClients = new List<object>();
            this.minerClients.AddRange(minerClients);

            lblSelectedMinerCount.Content = minerClients.Count.ToString();

            InitializeCommonFields();
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
                    ConfigureOutput exeResult = client.ExecuteDaemon<ConfigureOutput>(configParameters);

                    // If instance type changed, we need to decide the new instanceId
                    if(client.InstanceTypeEnum != properties.InstanceType && exeResult.InstanceId != null)
                    {
                        int finalInstanceId = AssignInstanceId(client.MachineFullName, properties.InstanceType, exeResult.InstanceId.Value);
                        if (finalInstanceId != exeResult.InstanceId)
                        {
                            client.ExecuteDaemon<ConfigureOutput>("-c \"{ 'InstanceId':'" + finalInstanceId.ToString() + "' }\"");
                            client.InstanceId = finalInstanceId;
                        }
                    }

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

            if (cbxXDaggerDevice.SelectedIndex >= 0)
            {
                properties.DeviceName = cbxXDaggerDevice.SelectedItem.ToString();
            }

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

            if (cbxEthDevice.SelectedIndex >= 0)
            {
                properties.DeviceName = cbxEthDevice.SelectedItem.ToString();
            }

            return true;
        }

        private string ComposeXDaggerConfig()
        {
            StringBuilder builder = new StringBuilder();
            bool isFirstParameter = true;

            builder.Append(" -c \"{ ");

            if (!string.IsNullOrWhiteSpace(properties.DeviceName))
            {
                builder.AppendFormat(" 'DeviceName':'{0}' ", properties.DeviceName);
                isFirstParameter = false;
            }

            if (!string.IsNullOrWhiteSpace(properties.XDaggerWalletAddress))
            {
                if (!isFirstParameter)
                {
                    builder.Append(", ");
                }
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

            bool isFirstParameter = true;
            if (!string.IsNullOrWhiteSpace(properties.DeviceName))
            {
                builder.AppendFormat(" 'DeviceName':'{0}' ", properties.DeviceName);
                isFirstParameter = false;
            }
            
            if (!isFirstParameter)
            {
                builder.Append(", ");
            }
            builder.AppendFormat(" 'EthPoolAddress':'{0}' ", properties.EthFullPoolAddress);

            builder.Append(" }\"");

            return builder.ToString();
        }

        /// <summary>
        /// When changing instanceType for 2+ clients on the same machine, it might has a situation that they will have the same instanceId,
        /// So in this step the MinerManager would detect the conflicts then assign new instanceId when necessary.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="returnedInstanceId"></param>
        /// <returns></returns>
        private int AssignInstanceId(string machineName, MinerClient.InstanceTypes instanceType, int newInstanceId)
        {
            int assignedInstanceId = newInstanceId;
            string key = machineName + instanceType.ToString() + assignedInstanceId.ToString();

            MinerManager manager = MinerManager.GetInstance();
            while (this.newInstanceIdDictionary.ContainsKey(key) 
                || manager.IsInstanceIdExists(machineName, instanceType, assignedInstanceId))
            {
                assignedInstanceId ++;
                key = machineName + instanceType.ToString() + assignedInstanceId.ToString();
            }

            this.newInstanceIdDictionary[key] = true;
            return assignedInstanceId;
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

        private void InitializeCommonFields()
        {
            MinerClient firstClient = (MinerClient)this.minerClients.FirstOrDefault();

            // Initialize the Common Devices
            List<string> commonDevices = new List<string>();
            commonDevices.AddRange(firstClient.Machine.Devices.Select(d => d.DisplayName));

            foreach(MinerClient client in this.minerClients)
            {
                for(int i = commonDevices.Count - 1; i >= 0; i--)
                {
                    if (!client.Machine.Devices.Any(device => device.DisplayName == commonDevices[i]))
                    {
                        commonDevices.RemoveAt(i);
                    }
                }
            }

            cbxXDaggerDevice.Items.Clear();
            cbxEthDevice.Items.Clear();
            foreach(string device in commonDevices)
            {
                cbxXDaggerDevice.Items.Add(device);
                cbxEthDevice.Items.Add(device);
            }

            // Initialize Text Fields
            string xdaggerWalletAddress = firstClient.XDaggerConfig?.WalletAddress;
            string xdaggerPoolAddress = firstClient.XDaggerConfig?.PoolAddress;
            string ethWalletAddress = firstClient.EthConfig?.WalletAddress;
            string ethEmail = firstClient.EthConfig?.Email;
            string ethWorkerName = firstClient.EthConfig?.WorkerName;
            int? ethPoolIndex = firstClient.EthConfig?.PoolIndex;
            int? ethPoolHostIndex = firstClient.EthConfig?.PoolHostIndex;

            foreach (MinerClient client in this.minerClients)
            {
                if(!string.Equals(client.XDaggerConfig?.WalletAddress, xdaggerWalletAddress))
                {
                    xdaggerWalletAddress = string.Empty;
                }
                if (!string.Equals(client.XDaggerConfig?.PoolAddress, xdaggerPoolAddress))
                {
                    xdaggerPoolAddress = string.Empty;
                }
                if (!string.Equals(client.EthConfig?.WalletAddress, ethWalletAddress))
                {
                    ethWalletAddress = string.Empty;
                }
                if (!string.Equals(client.EthConfig?.Email, ethEmail))
                {
                    ethEmail = string.Empty;
                }
                if (!string.Equals(client.EthConfig?.WorkerName, ethWorkerName))
                {
                    ethWorkerName = string.Empty;
                }

                if (client.EthConfig?.PoolIndex != ethPoolIndex)
                {
                    ethPoolIndex = null;
                }
                if (client.EthConfig?.PoolHostIndex != ethPoolHostIndex)
                {
                    ethPoolHostIndex = null;
                }
            }

            txtXDaggerWallet.Text = xdaggerWalletAddress;
            txtXDaggerWallet.Foreground = new SolidColorBrush(Colors.Gray);

            txtXDaggerPoolAddress.Text = xdaggerPoolAddress;
            txtXDaggerPoolAddress.Foreground = new SolidColorBrush(Colors.Gray);

            txtEthWallet.Text = ethWalletAddress;
            txtEthWallet.Foreground = new SolidColorBrush(Colors.Gray);

            txtEmailAddress.Text = ethEmail;
            txtEmailAddress.Foreground = new SolidColorBrush(Colors.Gray);

            if (ethPoolIndex.HasValue)
            {
                cbxTargetEthPool.SelectedIndex = ethPoolIndex.Value;
            }

            if (ethPoolHostIndex.HasValue)
            {
                cbxTargetEthPoolHost.SelectedIndex = ethPoolHostIndex.Value;
            }
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

        private void txtXDaggerWallet_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtXDaggerWallet.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void txtXDaggerPoolAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtXDaggerPoolAddress.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void txtEthWallet_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtEthWallet.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void txtEmailAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtEmailAddress.Foreground = new SolidColorBrush(Colors.Black);
        }
    }
}
