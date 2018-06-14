using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.Utils;

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

        public MinerClient(string machineName, string deploymentFolder, string version = "")
        {
            this.MachineName = machineName.Trim().ToUpper();
            this.DeploymentFolder = deploymentFolder.Trim().ToLower();
            this.BinaryPath = System.IO.Path.Combine(this.DeploymentFolder, WinMinerReleaseBinary.ProjectName);
            this.Version = version;

            this.CurrentDeploymentStatus = DeploymentStatus.Unknown;
            this.CurrentServiceStatus = ServiceStatus.Unknown;
        }

        public string MachineName
        {
            get; private set;
        }

        public string BinaryPath
        {
            get; private set;
        }

        public string DeploymentFolder
        {
            get; private set;
        }

        public DeploymentStatus CurrentDeploymentStatus
        {
            get; set;
        }

        public ServiceStatus CurrentServiceStatus
        {
            get; set;
        }

        public MinerDevice Device
        {
            get; set;
        }

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

        /// <summary>
        /// Deploy the full client.
        ///     1. Validate paramters
        ///     2. 
        /// </summary>
        /// <param name="progressHandler"></param>
        public void Deploy(Func<int, string, int> progressHandler)
        {

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
