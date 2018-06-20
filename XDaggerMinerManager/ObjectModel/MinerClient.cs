using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.Utils;
using Newtonsoft.Json;

namespace XDaggerMinerManager.ObjectModel
{
    /// <summary>
    /// MineClient Class
    /// </summary>
    public class MinerClient
    {
        public enum DeploymentStatus
        {
            NotExist,
            Downloaded,
            PrerequisitesInstalled,
            Ready,
            Unknown,
        }

        public enum ServiceStatus
        {
            Started,
            Stopped,
            Unknown,
        }

        public MinerClient(string machineName, string deploymentFolder, string version = "", string instanceName = "")
        {
            this.MachineName = machineName.Trim().ToUpper();
            this.DeploymentFolder = deploymentFolder.Trim().ToLower();
            this.Version = version;
            this.InstanceName = instanceName;

            this.CurrentDeploymentStatus = DeploymentStatus.Unknown;
            this.CurrentServiceStatus = ServiceStatus.Unknown;
        }
        
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(this.InstanceName))
                {
                    return this.MachineName;
                }
                else
                {
                    return string.Format("{0}_{1}", this.MachineName, this.InstanceName);
                }
            }
        }

        [JsonProperty(PropertyName = "machine_name")]
        public string MachineName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "instance_name")]
        public string InstanceName
        {
            get; set;
        }
        
        public string BinaryPath
        {
            get
            {
                return System.IO.Path.Combine(this.DeploymentFolder, WinMinerReleaseBinary.ProjectName);
            }
        }

        [JsonProperty(PropertyName = "deployment_folder")]
        public string DeploymentFolder
        {
            get; private set;
        }

        [JsonProperty(PropertyName = "current_deployment_status")]
        public DeploymentStatus CurrentDeploymentStatus
        {
            get; set;
        }

        [JsonProperty(PropertyName = "current_service_status")]
        public ServiceStatus CurrentServiceStatus
        {
            get; set;
        }

        [JsonProperty(PropertyName = "device")]
        public MinerDevice Device
        {
            get; set;
        }

        [JsonProperty(PropertyName = "version")]
        public string Version
        {
            get; private set;
        }

        public string GetRemoteDeploymentPath()
        {
            if (string.IsNullOrEmpty(MachineName) || string.IsNullOrEmpty(DeploymentFolder))
            {
                return string.Empty;
            }

            return string.Format("\\\\{0}\\{1}", this.MachineName, this.DeploymentFolder.Replace(":", "$"));
        }

        public string GetRemoteBinaryPath()
        {
            if (string.IsNullOrEmpty(MachineName) || string.IsNullOrEmpty(BinaryPath))
            {
                return string.Empty;
            }

            return string.Format("\\\\{0}\\{1}", this.MachineName, this.BinaryPath.Replace(":", "$"));
        }

        public string GetRemoteTempPath()
        {
            if (string.IsNullOrEmpty(MachineName))
            {
                return string.Empty;
            }

            return string.Format("\\\\{0}\\{1}", this.MachineName, System.IO.Path.GetTempPath().Replace(":", "$"));
        }

        #region Private Methods

        /// <summary>
        /// Validate the parameters before the installation:
        ///     1. Check the machine name is good and accessisable;
        ///     2. Check the binary path is valid and accessisable;
        ///     3. Check the version is valid and network is good;
        ///     4. Reset the status and ready for deploy.
        /// </summary>
        private void Validate()
        {

        }

        #endregion




    }
}
