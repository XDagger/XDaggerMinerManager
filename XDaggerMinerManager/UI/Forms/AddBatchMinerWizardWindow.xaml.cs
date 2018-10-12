using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
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
using XDaggerMinerManager.UI.Controls;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.UI.Forms
{
    /// <summary>
    /// Interaction logic for AddBatchMinerWizardWindow.xaml
    /// </summary>
    public partial class AddBatchMinerWizardWindow : Window
    {
        #region Private Members

        private List<MinerClient> createdClients = null;

        private bool needRefreshMachineConnections = true;

        // private Dictionary<string, MachineConnectivity> machineConnectivitiesResult = null;

        private MachineConnectivityBackgroundWork connectivityBackgroundWork = null;

        private List<MachineConnectivity> machineConnectivityCache = null;

        private string deploymentFolderPath = string.Empty;

        private List<Control> freezedControlList = new List<Control>();

        private MinerClient.InstanceTypes selectedMinerClientType;

        private Logger logger = Logger.GetInstance();

        #endregion

        #region Component Methods

        public AddBatchMinerWizardWindow()
        {
            InitializeComponent();

            createdClients = new List<MinerClient>();
            machineConnectivityCache = new List<MachineConnectivity>();

            needRefreshMachineConnections = true;
            
            InitializeUI();

            SwitchUIToStep(1);
        }

        private void InitializeUI()
        {

            dataGridMachines.SetDisplayColumns(MachineDataGrid.Columns.FullName, MachineDataGrid.Columns.IpAddressV4);
            /// dataGridMachines.CanUserEdit = true;
        }

        private void btnAddByName_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("btnAddByName Clicked.");

            InputMachineName inputMachine = new InputMachineName();
            inputMachine.OnFinished += InputMachine_OnFinished;
            inputMachine.ShowDialog();
        }

        private void btnOpenNetwork_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("btnOpenNetwork Clicked.");

            BrowseNetworkWindow browseNetworkWindow = new BrowseNetworkWindow(true);
            browseNetworkWindow.SetResultHandler(
                minerMachines =>
                {
                    AddToMachineList(minerMachines);
                    needRefreshMachineConnections = true;
                });

            browseNetworkWindow.ShowDialog();
        }

        private void btnImportMachineList_Click(object sender, RoutedEventArgs e)
        {
            logger.Trace("btnImportMachineList Clicked.");

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt";

            string importFileFullName = string.Empty;
            bool? result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
            {
                return;
            }

            importFileFullName = dialog.FileName;
            List<MinerMachine> newMachines = new List<MinerMachine>();
            using (StreamReader reader = new StreamReader(importFileFullName))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string machineName = line.Trim().ToUpper();
                    if (machineName.Contains(","))
                    {
                        machineName = machineName.Substring(0, machineName.IndexOf(','));
                    }

                    if (!string.IsNullOrEmpty(machineName))
                    {
                        newMachines.Add(new MinerMachine() { FullName = machineName });
                    }
                }
            }

            AddToMachineList(newMachines);
        }

        private void btnDeleteMachine_Click(object sender, RoutedEventArgs e)
        {
            List<MinerMachine> selectedMachines = dataGridMachines.GetSelectedMachines();
            if (selectedMachines == null || selectedMachines.Count == 0)
            {
                return;
            }

            foreach(MinerMachine machine in selectedMachines)
            {
                dataGridMachines.RemoveItem(machine);
            }

            needRefreshMachineConnections = true;
        }

        private void btnStepOneNext_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtTargetPath.Text))
            {
                MessageBox.Show("请输入安装矿机的有效路径", "提示");
                return;
            }

            deploymentFolderPath = txtTargetPath.Text.Trim();

            List<MinerMachine> machineList = dataGridMachines.GetAllMachines();
            if (machineList.Count == 0)
            {
                MessageBox.Show("请输入机器名称", "提示");
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtTargetUserName.Text))
            {
                string username = txtTargetUserName.Text.Trim();

                if (string.IsNullOrEmpty(txtTargetUserPassword.Password))
                {
                    MessageBox.Show("请输入用户的密码", "提示");
                    return;
                }

                string password = txtTargetUserPassword.Password.Trim();
                MachineCredential credential = new MachineCredential() { UserName = username, LoginPlainPassword = password };

                // Consolidate the credentials
                foreach (MinerMachine m in machineList)
                {
                    m.Credential = credential;
                }
            }

            createdClients.Clear();
            for (int i = machineConnectivityCache.Count - 1; i >= 0; i--)
            {
                MachineConnectivity connectivity = machineConnectivityCache[i];
                if (!machineList.Any(m => m.FullName.Equals(connectivity.Machine.FullName)))
                {
                    machineConnectivityCache.RemoveAt(i);
                }
            }
            
            foreach (MinerMachine machine in machineList)
            {
                createdClients.Add(new MinerClient(machine, deploymentFolderPath));

                if (!machineConnectivityCache.Any(conn => conn.Machine.FullName.Equals(machine.FullName)))
                {
                    machineConnectivityCache.Add(new MachineConnectivity(machine));
                }
            }

            SwitchUIToStep(2);
        }

        private void btnStepTwoNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepTwoBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(1);
        }

        private void btnStepThreeNext_Click(object sender, RoutedEventArgs e)
        {
            selectedMinerClientType = MinerClient.InstanceTypes.XDagger;
            SwitchUIToStep(4);
        }

        private void btnStepThreeBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(2);
        }

        private void btnStepFourXDaggerNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(5);
        }

        private void btnStepFourXDaggerBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFourEthNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(5);
        }

        private void btnStepFourEthBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFiveFinish_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnStepFiveBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(4);
        }

        private void txtTargetUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            needRefreshMachineConnections = true;
        }

        private void txtTargetUserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            needRefreshMachineConnections = true;
        }

        private void txtTargetPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            needRefreshMachineConnections = true;
        }

        #endregion

        #region Private Methods


        private void InputMachine_OnFinished(object sender, MachineNameEventArgs e)
        {
            MinerMachine machine = new MinerMachine() { FullName = e.MachineName };
            AddToMachineList(new List<MinerMachine>() { machine });

            needRefreshMachineConnections = true;
        }

        private void MachineConnectivity_OnUpdated(object sender, MachineConnectivityEventArgs args)
        {
            this.Dispatcher.Invoke( () =>
            {
                this.dataGridMachineConnections.Refresh();
            });
        }

        private void ShowProgressIndicator(string progressMesage, params Control[] controlList)
        {
            logger.Trace("Start ShowProgressIndicator.");

            lblTestConnectionNotice.Content = progressMesage;
            prbIndicator.Visibility = Visibility.Visible;
            prbIndicator.IsIndeterminate = true;

            // Freeze the controls
            if (controlList != null)
            {
                freezedControlList.AddRange(controlList.ToArray());

                foreach (Control control in freezedControlList)
                {
                    control.IsEnabled = false;
                }
            }
        }

        private void HideProgressIndicator()
        {
            logger.Trace("Start HideProgressIndicator.");

            lblTestConnectionNotice.Content = string.Empty;
            prbIndicator.IsIndeterminate = false;
            prbIndicator.Visibility = Visibility.Hidden;

            // Defreeze the controls
            foreach (Control control in freezedControlList)
            {
                control.IsEnabled = true;
            }

            freezedControlList.Clear();
        }

        private void SwitchUIToStep(int step)
        {
            logger.Trace("SwitchUIToStep: " + step);

            grdStepOne.Visibility = Visibility.Hidden;
            grdStepTwo.Visibility = Visibility.Hidden;
            grdStepThree.Visibility = Visibility.Hidden;
            grdStepFourXDagger.Visibility = Visibility.Hidden;
            grdStepFourEth.Visibility = Visibility.Hidden;
            grdStepFive.Visibility = Visibility.Hidden;

            lblStepOne.Background = null;
            lblStepTwo.Background = null;
            lblStepThree.Background = null;
            lblStepFour.Background = null;
            lblStepFive.Background = null;

            lblStepOne.FontWeight = FontWeights.Normal;
            lblStepTwo.FontWeight = FontWeights.Normal;
            lblStepThree.FontWeight = FontWeights.Normal;
            lblStepFour.FontWeight = FontWeights.Normal;
            lblStepFive.FontWeight = FontWeights.Normal;

            switch (step)
            {
                case 1:
                    grdStepOne.Visibility = Visibility.Visible;
                    lblStepOne.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepOne.FontWeight = FontWeights.ExtraBold;
                    break;
                case 2:
                    grdStepTwo.Visibility = Visibility.Visible;
                    lblStepTwo.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepTwo.FontWeight = FontWeights.ExtraBold;
                    break;
                case 3:
                    grdStepThree.Visibility = Visibility.Visible;
                    lblStepThree.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepThree.FontWeight = FontWeights.ExtraBold;
                    break;
                case 4:
                    if (selectedMinerClientType == MinerClient.InstanceTypes.XDagger)
                    {
                        grdStepFourXDagger.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        grdStepFourEth.Visibility = Visibility.Visible;
                    }

                    lblStepFour.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepFour.FontWeight = FontWeights.ExtraBold;
                    break;
                case 5:
                    grdStepFive.Visibility = Visibility.Visible;
                    lblStepFive.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepFive.FontWeight = FontWeights.ExtraBold;

                    break;
            }

            if (step == 2)
            {
                StepTwo_TestMachineConnections();
            }

            if (step == 3)
            {
                StepThree_RetrieveMinerVersions();
            }
            if (step == 4)
            {
                StepFour_RetrieveDeviceList();
            }
        }

        private void StepTwo_TestMachineConnections()
        {
            if (!needRefreshMachineConnections)
            {
                return;
            }

            dataGridMachineConnections.ClearItems();

            MachineConnectivityBackgroundWork connectivityWork = new MachineConnectivityBackgroundWork(this);
            connectivityWork.OnUpdated += MachineConnectivity_OnUpdated;
            connectivityWork.SetTestingFolderPath(deploymentFolderPath);

            foreach (MinerClient client in createdClients)
            {
                MachineConnectivity connectivity = machineConnectivityCache.FirstOrDefault(conn => conn.Machine.FullName.Equals(client.Machine.FullName));
                if (connectivity == null)
                {
                    throw new InvalidDataException($"Null MachineConnectivity found in machineConnectivityCache with machineName=[{ client.Machine.FullName }].");
                }

                connectivity.ResetFailureResults();

                dataGridMachineConnections.AddItem(connectivity);
                connectivityWork.AddConnectivity(connectivity);
            }

            BackgroundWork.CreateWork(
                this,
                () => {
                    ShowProgressIndicator("正在测试目标机器的连接，请稍后...", btnStepTwoNext, btnStepTwoBack);
                },
                () => {
                    connectivityWork.DoWork();
                },
                (taskResult) => {

                    HideProgressIndicator();
                    if (taskResult.HasError)
                    {
                        MessageBox.Show("测试出现错误:" + taskResult.Exception.Message);
                        logger.Error("Error while testing connection: " + taskResult.Exception.ToString());

                        return;
                    }

                    bool hasFailure = machineConnectivityCache.Any(conn => !conn.IsAllSuccess());
                    if (hasFailure)
                    {
                        btnStepTwoNext.IsEnabled = false;
                        MessageBox.Show("连接测试结果中有部分异常，请退回上一步重新选择机器或重新输入连接信息.");

                        logger.Warning("MachineConnectivityWork Finished with some failure, following is the detailed data:");
                        foreach (MachineConnectivity conn in machineConnectivityCache)
                        {
                            logger.Warning(conn.FullResult());
                        }
                    }

                    needRefreshMachineConnections = false;
                }
            ).Execute();
        }

        private void StepFour_RetrieveDeviceList()
        {
            
        }

        private void StepThree_RetrieveMinerVersions()
        {
            
        }
     
        /// <summary>
        /// Add the new list into the current machine list, if there are duplicate, prompt merge
        /// </summary>
        /// <param name="minerMachines"></param>
        private void AddToMachineList(List<MinerMachine> minerMachines)
        {
            bool duplicateFound = false;

            List<MinerMachine> existingMachines = dataGridMachines.GetAllMachines();

            foreach (MinerMachine machine in minerMachines)
            {
                if (existingMachines.Any(m => m.FullName.Equals(machine.FullName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    duplicateFound = true;
                }
                else
                {
                    dataGridMachines.AddItem(machine);
                }
            }
            
            if (duplicateFound)
            {
                MessageBox.Show("检测到机器名中有重复。由于批量创建矿机时每台机器只能同时创建一个矿机，机器列表中已经将重复的机器名去掉。", "提示", MessageBoxButton.OK);
            }
        }





        #endregion

       
    }
}
