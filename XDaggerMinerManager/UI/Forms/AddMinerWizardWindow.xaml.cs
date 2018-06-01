using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Threading;
using XDaggerMinerManager.ObjectModel;
using System.ComponentModel;
using IO = System.IO;
using XDaggerMinerManager.Utils;
using System.Diagnostics;

namespace XDaggerMinerManager.UI.Forms
{

    /// <summary>
    /// Interaction logic for AddMinerWizardWindow.xaml
    /// </summary>
    public partial class AddMinerWizardWindow : Window
    {
        public enum AddMinerWizardStatus
        {
            Initial,
            StepOne,
            StepTwo,
            StepThree,
            StepFour,
            Finished
        }

        private AddMinerWizardStatus wizardStatus = AddMinerWizardStatus.Initial;

        private MinerClient createdClient = null;

        private WinMinerReleaseBinary winMinerBinary = null;

        private List<DisplayedMinerDevice> displayedDeviceList = null;

        public AddMinerWizardWindow()
        {
            InitializeComponent();
        }

        public MinerClient CreatedClient
        {
            get
            {
                return createdClient;
            }
        }


        private void addMinerWizard_Loaded(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (wizardStatus == AddMinerWizardStatus.Finished)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show("确定要离开此向导吗？", "确认", MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void btnStepOneNext_Click(object sender, RoutedEventArgs e)
        {
            string targetMachineName = txtMachineName.Text;
            string targetMachinePath = txtTargetPath.Text;
            createdClient = new MinerClient(targetMachineName, targetMachinePath);

            // Check whether this target is already in Miner Manager Client list


            // Check the machine name and path is accessasible
            StepOne_ValidateTargetMachine();


            



            

            // Download the package and unzip to the folder



            
        }

        
        private void btnStepTwoNext_Click(object sender, RoutedEventArgs e)
        {
            // Should Check all of the versions first and then select LATEST by default
            StepTwo_QueryMinerVersions();
        }

        private void btnStepThreeNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(4);
        }

        private void btnStepTwoBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(1);
        }
        private void btnStepThreeBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(2);
        }

        private void btnStepFourBack_Click(object sender, RoutedEventArgs e)
        {
            SwitchUIToStep(3);
        }

        private void btnStepFourFinish_Click(object sender, RoutedEventArgs e)
        {
            wizardStatus = AddMinerWizardStatus.Finished;
            this.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        private void SwitchUIToStep(int step)
        {
            grdStepOne.Visibility = Visibility.Hidden;
            grdStepTwo.Visibility = Visibility.Hidden;
            grdStepThree.Visibility = Visibility.Hidden;
            grdStepFour.Visibility = Visibility.Hidden;

            lblStepOne.Background = null;
            lblStepTwo.Background = null;
            lblStepThree.Background = null;
            lblStepFour.Background = null;

            lblStepOne.FontWeight = FontWeights.Normal;
            lblStepTwo.FontWeight = FontWeights.Normal;
            lblStepThree.FontWeight = FontWeights.Normal;
            lblStepFour.FontWeight = FontWeights.Normal;

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
                    grdStepFour.Visibility = Visibility.Visible;
                    lblStepFour.Background = (SolidColorBrush)Application.Current.FindResource("WizardStepButton");
                    lblStepFour.FontWeight = FontWeights.ExtraBold;
                    
                    break;
            }

            if (step ==3 && displayedDeviceList == null)
            {
                StepThree_RetrieveDeviceList();
            }

        }

        #region Private Component Level Methods

        private void StepOne_ValidateTargetMachine()
        {
            if (createdClient == null)
            {
                return;
            }

            btnStepOneNext.IsEnabled = false;

            Ping pingSender = new Ping();
            AutoResetEvent waiter = new AutoResetEvent(false);

            // When the PingCompleted event is raised,
            // the PingCompletedCallback method is called.
            pingSender.PingCompleted += new PingCompletedEventHandler(StepOne_ValidateTargetMachine_Completed);

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "test";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 10 seconds for a reply.
            int timeout = 10000;

            // Set options for transmission:
            // The data can go through 64 gateways or routers
            // before it is destroyed, and the data packet
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            Console.WriteLine("Time to live: {0}", options.Ttl);
            Console.WriteLine("Don't fragment: {0}", options.DontFragment);

            // Send the ping asynchronously.
            // Use the waiter as the user token.
            // When the callback completes, it can wake up this thread.
            pingSender.SendAsync(createdClient.MachineName, timeout, buffer, options, waiter);
        }

        private void StepOne_ValidateTargetMachine_Completed(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");
                MessageBox.Show("无法连接到目标机器:" + createdClient.MachineName);

                // Let the main thread resume. 
                // UserToken is the AutoResetEvent object that the main thread 
                // is waiting for.
                ((AutoResetEvent)e.UserState).Set();
                btnStepOneNext.IsEnabled = true;
                return;
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());
                MessageBox.Show("无法连接到目标机器:" + createdClient.MachineName);
                btnStepOneNext.IsEnabled = true;

                // Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
                return;
            }

            PingReply reply = e.Reply;
            
            StepOne_ValidateTargetPath();



            // Enable the UI
            btnStepOneNext.IsEnabled = true;
        }

        /// <summary>
        /// Check the existance of the client, and check version/config if exists
        /// </summary>
        private void StepOne_ValidateTargetPath()
        {
            if (!System.IO.Directory.Exists(createdClient.GetRemoteDeploymentPath()))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(createdClient.GetRemoteDeploymentPath());
                }
                catch (UnauthorizedAccessException unauthException)
                {
                    // TODO Handle Exception
                }
            }

            if (System.IO.Directory.Exists(createdClient.GetRemoteBinaryPath()))
            {
                MessageBox.Show("目标路径下已经存在矿机，请先删除");
                // Enable the UI
                btnStepOneNext.IsEnabled = true;
                return;
            }

            btnStepOneNext.IsEnabled = true;
            SwitchUIToStep(2);
        }

        private void StepTwo_QueryMinerVersions()
        {
            // TODO: Check all Versions

            StepTwo_DownloadPackage();
        }

        private void StepTwo_DownloadPackage()
        {
            string version = this.cBxTargetVersion.Text;
            winMinerBinary = new WinMinerReleaseBinary(version);

            try
            {
                winMinerBinary.DownloadPackage(StepTwo_DownloadPackage_Completed);
            }
            catch (WebException webEx)
            {
                // TODO: Add handler
            }
            catch (InvalidOperationException invalidOper)
            {
                // TODO: Add handler
            }
        }

        private void StepTwo_DownloadPackage_Completed(object sender, AsyncCompletedEventArgs eventArg)
        {
            if (eventArg.Cancelled)
            {
                MessageBox.Show("Download Cancelled.");
                btnStepOneNext.IsEnabled = true;
                return;
            }

            if (eventArg.Error != null)
            {
                MessageBox.Show("Download Failed:" + eventArg.Error);
                btnStepOneNext.IsEnabled = true;
                return;
            }

            winMinerBinary.ExtractPackage();

            ////
            //// winMinerBinary.CopyBinaryToTargetPath(createdClient.GetRemoteBinaryPath());

            SwitchUIToStep(3);
        }

        private void StepThree_RetrieveDeviceList()
        {
            string daemonFullPath = IO.Path.Combine(createdClient.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);
            
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.FileName = @"D:\Toney\Personal\Git\xdagger\XDaggerMinerManager\XDaggerMinerManager\External\PsExec64.exe";
            p.StartInfo.Arguments = string.Format(@"\\{0} {1} -l", createdClient.MachineName, daemonFullPath);
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string errormessage = p.StandardError.ReadToEnd();
            p.WaitForExit();

            MessageBox.Show("Output: " + output);


            

        }

        #endregion

    }
}
