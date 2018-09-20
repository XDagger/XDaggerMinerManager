using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class MinerDataCell : INotifyPropertyChanged
    {
        private MinerClient clientObject = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public MinerClient Client
        {
            get
            {
                return clientObject;
            }
        }

        public string MachineName
        {
            get
            {
                return clientObject.Machine?.FullMachineName;
            }
        }

        private bool isSelcted = false;
        public bool IsSelected
        {
            get
            {
                return isSelcted;
            }
            set
            {
                PropertyChanged(this, new PropertyChangedEventArgs("IsSelected"));
                isSelcted = value;
            }
        }

        public string InstanceType
        {
            get
            {
                return clientObject.InstanceType;
            }
        }

        public string MinerName
        {
            get
            {
                return clientObject.Machine?.FullMachineName + clientObject.InstanceName;
            }
        }

        public string DeviceName
        {
            get
            {
                return clientObject.Device?.DisplayName;
            }
        }

        public string WalletAddress
        {
            get
            {
                return (this.InstanceType == "Ethereum") ? clientObject.EthFullPoolAddress : clientObject.XDaggerWalletAddress;
            }
        }

        public string DeploymentStatus
        {
            get
            {
                switch (clientObject.CurrentDeploymentStatus)
                {
                    case MinerClient.DeploymentStatus.Ready:
                        return "已安装";
                    case MinerClient.DeploymentStatus.NotExist:
                        return "未安装";
                    default:
                        return "错误";
                }
            }
        }

        public void Update()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ServiceStatus"));
        }

        public string ServiceStatus
        {
            get
            {
                switch (clientObject.CurrentServiceStatus)
                {
                    case MinerClient.ServiceStatus.Mining:
                        return "运行中";
                    case MinerClient.ServiceStatus.Connected:
                        return "已连接矿池";
                    case MinerClient.ServiceStatus.Disconnected:
                        return "未连接矿池";
                    case MinerClient.ServiceStatus.Stopped:
                        return "停止";
                    case MinerClient.ServiceStatus.NotInstalled:
                        return "未安装";
                    case MinerClient.ServiceStatus.Error:
                        return "错误";
                    case MinerClient.ServiceStatus.Unknown:
                        return "未知";
                    default:
                        return "未知";
                }
            }
        }

        public string HashRate
        {
            get
            {
                return string.Format("{0:0.000} Mhps", clientObject.CurrentHashRate / 1000000.0f);
            }
        }

        public MinerDataCell()
        {

        }

        public MinerDataCell(MinerClient clientObject)
        {
            this.clientObject = clientObject ?? throw new ArgumentNullException("MinerClient");
        }

    }
}
