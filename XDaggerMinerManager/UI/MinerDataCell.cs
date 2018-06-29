using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class MinerDataCell
    {
        public static string TranslateHeaderName(string headerName)
        {
            switch (headerName)
            {
                case "MachineName": return "机器名";
                case "MinerName": return "矿机名称";
                case "DeploymentStatus": return "安装状态";
                case "ServiceStatus": return "运行状态";
                case "DeviceName": return "矿卡";
                case "HashRate": return "当前算力";
                default:
                    return headerName;
            }
        }

        public string MachineName
        {
            get; set;
        }

        public string MinerName
        {
            get; set;
        }

        public string DeviceName
        {
            get; set;
        }

        public string DeploymentStatus
        {
            get; set;
        }

        public string ServiceStatus
        {
            get; set;
        }

        public string HashRate
        {
            get; set;
        }

        public MinerDataCell()
        {

        }

        public MinerDataCell(MinerClient clientObject)
        {
            this.MachineName = clientObject.MachineName;
            this.MinerName = clientObject.MachineName;
            this.DeviceName = clientObject.Device?.DisplayName;
            this.HashRate = string.Format("{0}Mhps", clientObject.CurrentHashRate);

            switch(clientObject.CurrentDeploymentStatus)
            {
                case MinerClient.DeploymentStatus.Ready:
                    this.DeploymentStatus = "已安装";
                    break;
                case MinerClient.DeploymentStatus.NotExist:
                    this.DeploymentStatus = "未安装";
                    break;
                default:
                    this.DeploymentStatus = "错误";
                    break;
            }
            
            switch(clientObject.CurrentServiceStatus)
            {
                case MinerClient.ServiceStatus.Mining:
                    this.ServiceStatus = "运行中";
                    break;
                case MinerClient.ServiceStatus.Connected:
                    this.ServiceStatus = "已连接矿池";
                    break;
                case MinerClient.ServiceStatus.Disconnected:
                    this.ServiceStatus = "未连接矿池";
                    break;
                case MinerClient.ServiceStatus.Stopped:
                    this.ServiceStatus = "停止";
                    break;
                case MinerClient.ServiceStatus.NotInstalled:
                    this.ServiceStatus = "未安装";
                    break;
                case MinerClient.ServiceStatus.Unknown:
                    this.ServiceStatus = "未知";
                    break;
                default:
                    this.ServiceStatus = "未知";
                    break;
            }
        }



    }
}
