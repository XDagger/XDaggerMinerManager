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
using XDaggerMinerManager.Network;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for ModifyMinerWindow.xaml
    /// </summary>
    public partial class ModifyMinerWindow : Window
    {
        private List<object> minerClients = null;

        private Logger logger = Logger.GetInstance();

        private ModifyMinerProperties properties = null;

        private Dictionary<string, bool> newInstanceIdDictionary = null;

        private bool missingXDaggerPoolAddress = false;

        private bool missingXDaggerWalletAddress = false;

        private bool missingEthWalletAddress = false;

        private bool missingEthPoolAddress = false;


        public ModifyMinerWindow(List<MinerClient> minerClients)
        {
            InitializeComponent();

            this.grdEthConfig.Visibility = Visibility.Hidden;
            this.grdXDaggerConfig.Visibility = Visibility.Hidden;

            InitializeEthPoolAddresses();

            properties = new ModifyMinerProperties();

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
                configParameters = ComposeXDaggerConfigParameters();
            }
            else if (properties.InstanceType == MinerClient.InstanceTypes.Ethereum && properties.EthConfig.IsEmptyConfig())
            {
                configParameters = ComposeEthConfigParameters();
            }
            logger.Trace($"Composed configure command parameter: [{ configParameters }]");

            ProgressWindow progress = new ProgressWindow("正在修改矿机配置...",
                this.minerClients,
                (obj) => {
                    MinerClient client = (MinerClient)obj;

                    if (properties.InstanceType == MinerClient.InstanceTypes.Ethereum && !properties.EthConfig.IsEmptyConfig())
                    {
                        configParameters = ComposeEthConfigParameters(client);
                    }
                    logger.Trace($"Composed configure command parameter: [{ configParameters }] on machine [{ client.MachineFullName }]");
                    ConfigureOutput exeResult = client.ExecuteDaemon<ConfigureOutput>(configParameters);

                    // If instance type changed, we need to decide the new instanceId
                    if(client.InstanceTypeEnum != properties.InstanceType && exeResult.InstanceId != null)
                    {
                        int finalInstanceId = AssignInstanceId(client.MachineFullName, properties.InstanceType, exeResult.InstanceId.Value);
                        if (finalInstanceId != exeResult.InstanceId)
                        {
                            client.ExecuteDaemon<ConfigureOutput>("-c \"{ 'InstanceId':'" + finalInstanceId.ToString() + "' }\"");
                        }

                        client.InstanceId = finalInstanceId;
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

            if (string.IsNullOrEmpty(poolAddress) && missingXDaggerPoolAddress)
            {
                MessageBox.Show("由于有些矿机没有设置矿池地址，必须填写矿池地址");
                return false;
            }

            properties.XDaggerPoolAddress = poolAddress;

            string walletAddress = txtXDaggerWallet.Text.Trim();
            if (!string.IsNullOrWhiteSpace(walletAddress) && walletAddress.Length != 32)
            {
                MessageBox.Show("钱包必须为长度32位的字母与数字组合");
                return false;
            }

            if (string.IsNullOrEmpty(walletAddress) && missingXDaggerWalletAddress)
            {
                MessageBox.Show("由于有些矿机没有设置钱包地址，必须填写钱包地址");
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
            
            bool ethPoolTotallyBlank = (cbxTargetEthPool.SelectedIndex < 0)
                                    && (cbxTargetEthPoolHost.SelectedIndex < 0)
                                    && (string.IsNullOrEmpty(txtEmailAddress.Text));

            if (missingEthWalletAddress && string.IsNullOrEmpty(ethWalletAddress))
            {
                MessageBox.Show("由于有些矿机没有设置钱包地址，必须填写钱包地址");
                return false;
            }

            if (cbxTargetEthPool.SelectedIndex >= 0 && cbxTargetEthPoolHost.SelectedIndex < 0)
            {
                MessageBox.Show("请选择矿池的地址");
                return false;
            }

            if (missingEthPoolAddress)
            {
                if (cbxTargetEthPool.SelectedIndex < 0)
                {
                    MessageBox.Show("由于有些矿机没有设置矿池，请选择矿池类型");
                    return false;
                }
            }

            EthConfig updatedConfig = new EthConfig();
            updatedConfig.PoolIndex = (cbxTargetEthPool.SelectedIndex >= 0) ? (EthConfig.PoolIndexes)cbxTargetEthPool.SelectedIndex : (EthConfig.PoolIndexes?)null;
            updatedConfig.PoolHostIndex = cbxTargetEthPoolHost.SelectedIndex;
            updatedConfig.EmailAddress = txtEmailAddress.Text.Trim();
            updatedConfig.WalletAddress = ethWalletAddress;
            updatedConfig.WorkerName = "{MACHINE_NAME}_{INSTANCE_ID}";
            properties.EthConfig = updatedConfig;

            if (cbxEthDevice.SelectedIndex >= 0)
            {
                properties.DeviceName = cbxEthDevice.SelectedItem.ToString();
            }

            return true;
        }

        private string ComposeXDaggerConfigParameters()
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

        private string ComposeEthConfigParameters(MinerClient client = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(" -c \"{ ");

            bool isFirstParameter = true;
            if (!string.IsNullOrWhiteSpace(properties.DeviceName))
            {
                builder.AppendFormat(" 'DeviceName':'{0}' ", properties.DeviceName);
                isFirstParameter = false;
            }
            
            if (client != null && properties.EthConfig != null)
            {
                EthConfig mergedConfig = client.EthConfig.CloneWithUpdate(properties.EthConfig);
                if (!isFirstParameter)
                {
                    builder.Append(", ");
                }
                builder.AppendFormat(" 'EthPoolAddress':'{0}' ", mergedConfig.PoolFullAddress);
            }

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
            foreach (string ethPoolName in EthConfig.PoolDisplayNames)
            {
                cbxTargetEthPool.Items.Add(ethPoolName);
            }

            cbxTargetEthPoolHost.Items.Clear();
        }

        private void InitializeCommonFields()
        {
            logger.Trace("InitializeCommonFields.");

            MinerClient firstClient = (MinerClient)this.minerClients.FirstOrDefault();

            // Initialize the Common Devices
            List<string> commonDevices = new List<string>();
            commonDevices.AddRange(firstClient.Machine.Devices.Select(d => d.DisplayName));
            
            foreach(string cDevice in commonDevices)
            {
                logger.Trace("Common Device: " + cDevice);
            }

            foreach (MinerClient client in this.minerClients)
            {
                for(int i = commonDevices.Count - 1; i >= 0; i--)
                {
                    if (!client.Machine.Devices.Any(device => (string.Equals(device.DisplayName, commonDevices[i]))))
                    {
                        logger.Trace($"Cannot find device [{commonDevices[i]}], remove it.");
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
            string ethEmail = firstClient.EthConfig?.EmailAddress;
            string ethWorkerName = firstClient.EthConfig?.WorkerName;
            EthConfig.PoolIndexes? ethPoolIndex = firstClient.EthConfig?.PoolIndex;
            int? ethPoolHostIndex = firstClient.EthConfig?.PoolHostIndex;

            foreach (MinerClient client in this.minerClients)
            {
                xdaggerWalletAddress = CheckMatchAndIgnoreEmpty(client.XDaggerConfig?.WalletAddress, xdaggerWalletAddress);
                xdaggerPoolAddress = CheckMatchAndIgnoreEmpty(client.XDaggerConfig?.PoolAddress, xdaggerPoolAddress);
                ethWalletAddress = CheckMatchAndIgnoreEmpty(client.EthConfig?.WalletAddress, ethWalletAddress);
                ethEmail = CheckMatchAndIgnoreEmpty(client.EthConfig?.EmailAddress, ethEmail);
                ethWorkerName = CheckMatchAndIgnoreEmpty(client.EthConfig?.WorkerName, ethWorkerName);
                
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

            txtWorkerName.Text = ethWorkerName;
            txtWorkerName.Foreground = new SolidColorBrush(Colors.Gray);

            if (ethPoolIndex.HasValue)
            {
                cbxTargetEthPool.SelectedIndex = ethPoolIndex.GetHashCode();
            }

            if (ethPoolHostIndex.HasValue)
            {
                cbxTargetEthPoolHost.SelectedIndex = ethPoolHostIndex.Value;
            }

            // Check the missing fields that must be filled by user
            missingEthPoolAddress = false;
            missingEthWalletAddress = false;
            missingXDaggerPoolAddress = false;
            missingXDaggerWalletAddress = false;

            foreach (MinerClient client in this.minerClients)
            {
                if (string.IsNullOrEmpty(client.XDaggerConfig?.PoolAddress))
                {
                    missingXDaggerPoolAddress = true;
                }

                if (string.IsNullOrEmpty(client.XDaggerConfig?.WalletAddress))
                {
                    missingXDaggerWalletAddress = true;
                }

                if (client.EthConfig?.PoolIndex == null || client.EthConfig?.PoolHostIndex == null)
                {
                    missingEthPoolAddress = true;
                }

                if (string.IsNullOrEmpty(client.EthConfig?.WalletAddress))
                {
                    missingEthWalletAddress = true;
                }
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
            if (cbxTargetEthPool.SelectedIndex < 0 || cbxTargetEthPool.SelectedIndex >= EthConfig.PoolHostUrls.Count)
            {
                return;
            }

            foreach (string ethPoolHost in EthConfig.PoolHostUrls[cbxTargetEthPool.SelectedIndex])
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

        private void txtWorkerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtWorkerName.Foreground = new SolidColorBrush(Colors.Black);
        }

        private string CheckMatchAndIgnoreEmpty(string newValue, string originValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return originValue;
            }

            if (string.Equals(newValue, originValue))
            {
                return originValue;
            }

            return string.Empty;
        }
    }
}
