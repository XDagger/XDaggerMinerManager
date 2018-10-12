using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.Utils
{
    public class MachineConnectivityBackgroundWork
    {
        private Window window = null;
        private List<MinerMachine> minerMachines = null;
        private Dictionary<string, MachineConnectivity> connectivityResults = null;

        public event EventHandler<EventArgs> OnFinished;
        public event EventHandler<EventArgs> OnUpdated;

        private int startedWorksCount;
        private int finishedWorksCount;

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
        
        public void AddMachine(MinerMachine machine)
        {
            if (!connectivityResults.ContainsKey(machine.FullName))
            {
                connectivityResults[machine.FullName] = new MachineConnectivity();
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
                MachineConnectivity connectivity = GetConnectivity(keyValue.Key);

                if (connectivity.CanPing ?? false)
                {
                    continue;
                }

                startedWorksCount++;

                string machineName = keyValue.Key;
                BackgroundWork<PingReply>.CreateWork(
                    window,
                    () =>
                    {
                    },
                    () =>
                    {
                        return PingUtil.Send(machineName);
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
                            PingReply reply = taskResult.Result;
                            result = (reply.Status == IPStatus.Success);
                        }

                        ConsolidatePingResult(machineName, result);
                    }
                ).Execute();

            }

            while(finishedWorksCount < startedWorksCount)
            {
                Thread.Sleep(30);
            }

            OnFinished?.Invoke(this, new EventArgs());
        }

        private void ConsolidatePingResult(string machineName, bool result)
        {
            MachineConnectivity connectivity = GetConnectivity(machineName);
            connectivity.CanPing = result;

            OnUpdated?.Invoke(this, new EventArgs());

            finishedWorksCount++;
        }

        public MachineConnectivity GetConnectivity(string machineName)
        {
            if (!connectivityResults.ContainsKey(machineName))
            {
                connectivityResults[machineName] = new MachineConnectivity();
            }

            return connectivityResults[machineName];
        }
    }
}
