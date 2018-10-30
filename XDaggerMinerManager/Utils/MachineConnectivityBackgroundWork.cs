using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using XDaggerMinerManager.Networking;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.Utils
{
    public class MachineConnectivityBackgroundWork
    {
        private Window window = null;
        private List<MinerMachine> minerMachines = null;
        private Dictionary<string, MachineConnectivity> connectivityResults = null;

        private Logger logger = Logger.GetInstance();

        //// public event EventHandler<EventArgs> OnFinished;
        public event EventHandler<MachineConnectivityEventArgs> OnUpdated;

        private int startedWorksCount;
        private int finishedWorksCount;

        private string testingFolderPath = string.Empty;

        public MachineConnectivityBackgroundWork(Window window)
        {
            this.window = window;
            minerMachines = new List<MinerMachine>();
            connectivityResults = new Dictionary<string, MachineConnectivity>();
        }

        public MachineConnectivityBackgroundWork(Window window, List<MinerMachine> machineList)
        {
            this.window = window;
            minerMachines = machineList;
            connectivityResults = new Dictionary<string, MachineConnectivity>();
        }

        public void SetTestingFolderPath(string localPath)
        {
            this.testingFolderPath = localPath;
        }

        public void AddConnectivity(MachineConnectivity connectivity)
        {
            if (connectivity == null || connectivity.Machine == null)
            {
                throw new ArgumentNullException("AddConnectivity with null argument.");
            }

            connectivityResults[connectivity.Machine.FullName] = connectivity;
        }

        public void AddMachine(MinerMachine machine)
        {
            if (!connectivityResults.ContainsKey(machine.FullName))
            {
                connectivityResults[machine.FullName] = new MachineConnectivity(machine);
            }
        }

        public void AddMachines(List<MinerMachine> machines)
        {
            foreach(MinerMachine machine in machines)
            {
                AddMachine(machine);
            }
        }

        public void ClearExcept(List<MinerMachine> machines)
        {
            foreach (KeyValuePair<string, MachineConnectivity> keyValue in connectivityResults)
            {
                if (!machines.Any(m => m.FullName.Equals(keyValue.Key)))
                {
                    connectivityResults.Remove(keyValue.Key);
                }
            }
        }

        public void DoWork()
        {
            startedWorksCount = 0;
            finishedWorksCount = 0;

            foreach (KeyValuePair<string, MachineConnectivity> keyValue in connectivityResults)
            {
                MachineConnectivity connectivity = keyValue.Value;

                if (connectivity.CanPing ?? false)
                {
                    continue;
                }

                startedWorksCount++;

                string machineName = keyValue.Key;
                BackgroundWork<bool>.CreateWork(window, () => { },
                    () =>
                    {
                        return PingUtil.PingHost(machineName);
                    },
                    (taskResult) =>
                    {
                        bool result = false;
                        if (taskResult.HasError)
                        {
                            result = false;
                        }
                        else
                        {
                            result = taskResult.Result;
                        }

                        ConsolidatePingResult(machineName, result);
                    }
                ).Execute();
            }

            foreach (KeyValuePair<string, MachineConnectivity> keyValue in connectivityResults)
            {
                MachineConnectivity connectivity = keyValue.Value;

                if (connectivity.CanRemotePathAccess ?? false)
                {
                    continue;
                }

                startedWorksCount++;
                
                string machineName = keyValue.Key;
                string userName = connectivity.Machine.Credential?.UserName;
                string password = connectivity.Machine.Credential?.LoginPlainPassword;

                NetworkFileAccess fileAccess = new NetworkFileAccess(machineName, userName, password);
 
                BackgroundWork.CreateWork(window, () => { },
                    () =>
                    {
                        fileAccess.EnsureDirectory(testingFolderPath);
                        fileAccess.CanAccessDirectory(testingFolderPath);
                        return 0;
                    },
                    (taskResult) =>
                    {
                        bool result = true;
                        if (taskResult.HasError)
                        {
                            logger.Error($"Testing of RemotePathAccess failed for machine [{ machineName }]. Message: { taskResult.Exception.ToString() }");
                            result = false;
                        }

                        ConsolidateRemotePathAccessResult(machineName, result);
                    }
                ).Execute();
            }

            foreach (KeyValuePair<string, MachineConnectivity> keyValue in connectivityResults)
            {
                MachineConnectivity connectivity = keyValue.Value;
                MinerMachine machine = connectivity.Machine;
                if (connectivity.CanRemotePowershell ?? false)
                {
                    continue;
                }

                startedWorksCount++;

                string machineName = keyValue.Key;

                BackgroundWork.CreateWork(window, () => { },
                    () =>
                    {
                        TargetMachineExecutor executor = TargetMachineExecutor.GetExecutor(machineName);
                        if (machine.Credential != null)
                        {
                            executor.SetCredential(machine.Credential.UserName, machine.Credential.LoginPlainPassword);
                        }

                        executor.TestConnection();
                        return 0;
                    },
                    (taskResult) =>
                    {
                        bool result = true;
                        if (taskResult.HasError)
                        {
                            logger.Error($"Testing of RemotePathAccess failed for machine [{ machineName }]. Message: { taskResult.Exception.ToString() }");
                            result = false;
                        }

                        ConsolidateRemotePowershellResult(machineName, result);
                    }
                ).Execute();
            }

            while (finishedWorksCount < startedWorksCount)
            {
                Thread.Sleep(30);
            }
        }

        private void ConsolidatePingResult(string machineName, bool result)
        {
            finishedWorksCount++;

            MachineConnectivity connectivity = connectivityResults[machineName];
            lock (connectivity)
            {
                connectivity.CanPing = result;
            }

            OnUpdated?.Invoke(this, new MachineConnectivityEventArgs(connectivity));
        }

        private void ConsolidateRemotePathAccessResult(string machineName, bool result)
        {
            finishedWorksCount++;

            MachineConnectivity connectivity = connectivityResults[machineName];
            lock (connectivity)
            {
                connectivity.CanRemotePathAccess = result;
            }

            OnUpdated?.Invoke(this, new MachineConnectivityEventArgs(connectivity));
        }

        private void ConsolidateRemotePowershellResult(string machineName, bool result)
        {
            finishedWorksCount++;

            MachineConnectivity connectivity = connectivityResults[machineName];
            lock (connectivity)
            {
                connectivity.CanRemotePowershell = result;
            }

            OnUpdated?.Invoke(this, new MachineConnectivityEventArgs(connectivity));
        }

        public MachineConnectivity GetConnectivity(string machineName)
        {
            if (!connectivityResults.ContainsKey(machineName))
            {
                connectivityResults[machineName] = new MachineConnectivity(new MinerMachine() { FullName = machineName } );
            }

            return connectivityResults[machineName];
        }
    }

    public class MachineConnectivityEventArgs : EventArgs
    {
        public MachineConnectivityEventArgs(MachineConnectivity connectivity)
        {
            this.Result = connectivity;
        }

        public MachineConnectivity Result
        {
            get; set;
        }
    }
}
