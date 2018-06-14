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

        public MinerDataCell()
        {

        }

        public MinerDataCell(MinerClient clientObject)
        {
            this.MachineName = clientObject.MachineName;
            this.MinerName = clientObject.MachineName;
            this.DeviceName = clientObject.Device?.DisplayName;

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
                case MinerClient.ServiceStatus.Started:
                    this.ServiceStatus = "运行中";
                    break;
                case MinerClient.ServiceStatus.Stopped:
                    this.ServiceStatus = "停止";
                    break;
                case MinerClient.ServiceStatus.Unknown:
                    this.ServiceStatus = "未连接";
                    break;
                default:
                    this.ServiceStatus = "未连接";
                    break;
            }
        }



    }
}
