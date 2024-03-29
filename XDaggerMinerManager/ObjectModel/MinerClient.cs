﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.Utils;
using Newtonsoft.Json;
using System.IO;
using XDaggerMinerManager.Configuration;
using XDaggerMinerManager.Networking;

namespace XDaggerMinerManager.ObjectModel
{
    /// <summary>
    /// MineClient Class
    /// </summary>
    public class MinerClient
    {
        private bool hasStatusChanged = false;

        public enum InstanceTypes
        {
            Unset = 0,
            XDagger = 1,
            Ethereum = 2,
        }

        public enum DeploymentStatus
        {
            Unknown = -1,
            NotExist = 1,
            Downloaded = 2,
            PrerequisitesInstalled = 3,
            Ready = 4,
        }

        public enum ServiceStatus
        {
            Unknown = 0,
            NotInstalled = 10,
            Stopped = 20,
            Initializing = 30,
            Disconnected = 40,
            Connected = 50,
            Mining = 60,
            Error = 99,
        }

        public event EventHandler StatusChanged;
        
        public void ResetStatusChanged()
        {
            this.hasStatusChanged = false;
        }

        public MinerClient()
        {
        }

        public MinerClient(string machineName, string deploymentFolder, string version = "", int instanceId = 0)
            : this()
        {
            this.MachineFullName = machineName;
            
            this.DeploymentFolder = deploymentFolder.Trim().ToLower();
            this.Version = version;
            this.InstanceId = instanceId;

            this.CurrentDeploymentStatus = DeploymentStatus.Unknown;
            this.CurrentServiceStatus = ServiceStatus.Unknown;
        }
        
        public MinerClient(MinerMachine machine, string deploymentFolder, string version = "", int instanceId = 0)
            : this()
        {
            this.MachineFullName = machine.FullName;
            this.Machine = machine;

            this.DeploymentFolder = deploymentFolder.Trim().ToLower();
            this.Version = version;
            this.InstanceId = instanceId;

            this.CurrentDeploymentStatus = DeploymentStatus.Unknown;
            this.CurrentServiceStatus = ServiceStatus.Unknown;
        }

        public string Name
        {
            get
            {
                if (this.InstanceId == 0)
                {
                    return this.MachineFullName;
                }
                else
                {
                    return string.Format("{0}_{1}", this.MachineFullName, this.InstanceId);
                }
            }
        }

        /// <summary>
        /// This is the randomly generated folder suffix for the multiple client instances
        /// </summary>
        [JsonProperty(PropertyName = "folder_suffix")]
        public string FolderSuffix
        {
            get; set;
        }

        [JsonProperty(PropertyName = "machine_full_name")]
        public string MachineFullName
        {
            get; set;
        }

        [JsonIgnore]
        public MinerMachine Machine
        {
            get; set;
        }

        private XDaggerConfig xdaggerConfig = null;
        [JsonProperty(PropertyName = "xdagger_config")]
        public XDaggerConfig XDaggerConfig
        {
            get
            {
                if (xdaggerConfig == null)
                {
                    xdaggerConfig = new XDaggerConfig();
                }

                return xdaggerConfig;
            }
            set
            {
                if (value != null)
                {
                    xdaggerConfig = value;
                }
            }
        }

        private EthConfig ethConfig = null;
        [JsonProperty(PropertyName = "eth_config")]
        public EthConfig EthConfig
        {
            get
            {
                if (ethConfig == null)
                {
                    ethConfig = new EthConfig();
                }

                return ethConfig;
            }
            set
            {
                if (value != null)
                {
                    ethConfig = value;
                }
            }
        }
        
        /*
        [JsonProperty(PropertyName = "xdagger_wallet_address")]
        public string XDaggerWalletAddress
        {
            get; set;
        }

        [JsonProperty(PropertyName = "xdagger_pool_address")]
        public string XDaggerPoolAddress
        {
            get; set;
        }

        [JsonProperty(PropertyName = "eth_full_pool_address")]
        public string EthFullPoolAddress
        {
            get; set;
        }
        */

        /// <summary>
        /// This is the InstanceId of this client
        /// </summary>
        [JsonProperty(PropertyName = "instance_Id")]
        public int InstanceId
        {
            get; set;
        }

        [JsonProperty(PropertyName = "instance_type")]
        public string InstanceType
        {
            get; set;
        }

        [JsonProperty(PropertyName = "instance_type_enum")]
        public InstanceTypes InstanceTypeEnum
        {
            get; set;
        }

        public string BinaryFolder
        {
            get
            {
                if (string.IsNullOrEmpty(this.FolderSuffix))
                {
                    return WinMinerReleaseBinary.ProjectName;
                }
                else
                {
                    return string.Format("{0}_{1}", WinMinerReleaseBinary.ProjectName, this.FolderSuffix);
                }
            }
        }

        public string BinaryPath
        {
            get
            {
                return System.IO.Path.Combine(this.DeploymentFolder, this.BinaryFolder);
            }
        }

        public bool IsServiceStatusRunning()
        {
            return this.CurrentServiceStatus > ServiceStatus.Stopped;
        }

        [JsonProperty(PropertyName = "deployment_folder")]
        public string DeploymentFolder
        {
            get; private set;
        }

        private DeploymentStatus currentDeploymentStatus = DeploymentStatus.Unknown;

        [JsonProperty(PropertyName = "current_deployment_status")]
        public DeploymentStatus CurrentDeploymentStatus
        {
            get
            {
                return this.currentDeploymentStatus;
            }
            set
            {
                if (value != this.currentDeploymentStatus)
                {
                    this.currentDeploymentStatus = value;
                    hasStatusChanged = true;
                    this.OnStatusChanged(EventArgs.Empty);
                }
                else
                {
                    this.currentDeploymentStatus = value;
                }
            }
        }

        private ServiceStatus currentServiceStatus;

        [JsonProperty(PropertyName = "current_service_status")]
        public ServiceStatus CurrentServiceStatus
        {
            get
            {
                return this.currentServiceStatus;
            }

            set
            {
                if (value != this.currentServiceStatus)
                {
                    this.currentServiceStatus = value;
                    hasStatusChanged = true;
                    this.OnStatusChanged(EventArgs.Empty);
                }
                else
                {
                    this.currentServiceStatus = value;
                }
            }
        }

        private string currentServiceErrorDetails = string.Empty;

        [JsonProperty(PropertyName = "current_service_error_details")]
        public string CurrentServiceErrorDetails
        {
            get
            {
                return this.currentServiceErrorDetails;
            }

            set
            {
                if (value != this.currentServiceErrorDetails)
                {
                    this.currentServiceErrorDetails = value;
                    hasStatusChanged = true;
                    this.OnStatusChanged(EventArgs.Empty);
                }
                else
                {
                    this.currentServiceErrorDetails = value;
                }
            }
        }

        private double currentHashRate = 0;

        public double CurrentHashRate
        {
            get
            {
                return currentHashRate;
            }
            private set
            {
                if (value != this.currentHashRate)
                {
                    this.currentHashRate = value;
                    hasStatusChanged = true;
                    this.OnStatusChanged(EventArgs.Empty);
                }
                else
                {
                    this.currentHashRate = value;
                }
            }
        }

        [JsonProperty(PropertyName = "device")]
        public MinerDevice Device
        {
            get; set;
        }

        [JsonProperty(PropertyName = "version")]
        public string Version
        {
            get; set;
        }

        
        public string GetRemoteDeploymentPath()
        {
            if (string.IsNullOrEmpty(this.MachineFullName) || string.IsNullOrEmpty(DeploymentFolder))
            {
                return string.Empty;
            }

            return string.Format("\\\\{0}\\{1}", this.MachineFullName, this.DeploymentFolder.Replace(":", "$"));
        }

        public string GetRemoteBinaryPath()
        {
            if (string.IsNullOrEmpty(this.MachineFullName) || string.IsNullOrEmpty(BinaryPath))
            {
                return string.Empty;
            }

            return string.Format("\\\\{0}\\{1}", this.MachineFullName, this.BinaryPath.Replace(":", "$"));
        }

        public string GetRemoteTempPath()
        {
            if (string.IsNullOrEmpty(this.MachineFullName))
            {
                return string.Empty;
            }

            return string.Format("\\\\{0}\\{1}", this.MachineFullName, Path.GetTempPath().Replace(":", "$"));
        }

        public T ExecuteDaemon<T>(string parameters, bool disableTrace = false)
        {
            if (this.Machine == null)
            {
                throw new ArgumentNullException("The Machine object is not initialized in MinerClient. " + this.MachineFullName);
            }

            TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(this.Machine, disableTrace);
            string daemonFullPath = Path.Combine(this.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);

            return executor.ExecuteCommandAndThrow<T>(daemonFullPath, parameters);
        }

        public void DeleteBinaries()
        {
            if (this.CurrentDeploymentStatus < DeploymentStatus.Downloaded)
            {
                // There should not be binaries, just ignore
                this.CurrentDeploymentStatus = DeploymentStatus.NotExist;
                return;
            }

            try
            {
                string username = this.Machine?.Credential?.UserName;
                string password = this.Machine?.Credential?.LoginPlainPassword;

                NetworkFileAccess fileAccess = new NetworkFileAccess(this.MachineFullName, username, password);
                fileAccess.DirectoryDelete(BinaryPath);

                this.CurrentDeploymentStatus = MinerClient.DeploymentStatus.NotExist;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Tags Sample: MACHINE_NAME, INSTANCE_ID
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public string FillStringTags(string template)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            string result = template;
            result = result.Replace(@"{MACHINE_NAME}", this.MachineFullName);
            result = result.Replace(@"{INSTANCE_ID}", this.InstanceId.ToString());

            return result;
        }

        /// <summary>
        /// Update the current deployment status and service status
        /// </summary>
        public bool RefreshStatus()
        {
            try
            {
                ReportOutput report = this.ExecuteDaemon<ReportOutput>("-r", true);
                if (report == null)
                {
                    if (hasStatusChanged)
                    {
                        return false;
                    }
                    else
                    {
                        hasStatusChanged = true;
                        return true;
                    }
                }

                this.CurrentServiceStatus = (ServiceStatus)report.Status;
                this.CurrentHashRate = report.HashRate;

                if (this.CurrentServiceStatus == ServiceStatus.Error)
                {
                    this.CurrentServiceErrorDetails = report.Details;
                }

                hasStatusChanged = true;
            }
            catch(Exception ex)
            {
                hasStatusChanged = false;
            }

            return hasStatusChanged;
        }

        public void GenerateFolderSuffix()
        {
            FolderSuffix = RandomUtils.GenerateString(3);
        }

        public bool HasFolderSuffix
        {
            get
            {
                return !string.IsNullOrEmpty(FolderSuffix);
            }
        }

        #region Private Methods


        private void OnStatusChanged(EventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        
        #endregion




    }
}
