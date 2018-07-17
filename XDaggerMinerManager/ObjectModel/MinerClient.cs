﻿using System;
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
        private bool hasStatusChanged = false;

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
            NotInstalled = 1,
            Stopped = 2,
            Disconnected = 3,
            Connected = 4,
            Mining = 5
        }

        public event EventHandler StatusChanged;
        
        public void ResetStatusChanged()
        {
            this.hasStatusChanged = false;
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

        public ServiceStatus currentServiceStatus;

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

        public ExecutionResult<T> ExecuteDaemon<T>(string parameters)
        {
            TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(this.MachineName);
            string daemonFullPath = System.IO.Path.Combine(this.BinaryPath, WinMinerReleaseBinary.DaemonExecutionFileName);

            return executor.ExecuteCommand<T>(daemonFullPath, parameters);
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
                string binariesPath = GetRemoteBinaryPath();

                if (!string.IsNullOrEmpty(binariesPath))
                {
                    System.IO.Directory.Delete(binariesPath, true);
                }

                this.CurrentDeploymentStatus = MinerClient.DeploymentStatus.NotExist;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Update the current deployment status and service status
        /// </summary>
        public bool RefreshStatus()
        {
            ExecutionResult<ReportOutput> exeResult = this.ExecuteDaemon<ReportOutput>("-r");
            if (exeResult.HasError || exeResult.Data == null)
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

            ReportOutput report = exeResult.Data;

            this.CurrentServiceStatus = (ServiceStatus)report.Status;
            
            if (this.CurrentServiceStatus == ServiceStatus.Mining)
            {
                this.CurrentHashRate = report.HashRate;
            }
            else
            {
                this.CurrentHashRate = 0;
            }

            hasStatusChanged = true;

            return hasStatusChanged;
        }

        #region Private Methods
        

        private void OnStatusChanged(EventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        #endregion




    }
}
