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

        private MinerClient.InstanceTypes selectedMinerClientType;

        private Logger logger = Logger.GetInstance();

        #endregion

        #region Component Methods

        public AddBatchMinerWizardWindow()
        {
            InitializeComponent();

            createdClients = new List<MinerClient>();
            needRefreshMachineConnections = true;

            SwitchUIToStep(1);
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

            if (step == 3)
            {
                StepThree_RetrieveMinerVersions();
            }
            if (step == 4)
            {
                StepFour_RetrieveDeviceList();
            }
        }

        private void btnStepOneNext_Click(object sender, RoutedEventArgs e)
        {
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

        #endregion

        #region Private Methods

        private void StepFour_RetrieveDeviceList()
        {
            
        }

        private void StepThree_RetrieveMinerVersions()
        {
            
        }



        #endregion

    }
}
