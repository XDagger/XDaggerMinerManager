using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XDaggerMinerManager.Configuration;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.ObjectModel
{
    public class MinerManager
    {
        private static MinerManager instance = null;

        private Logger logger = Logger.GetInstance();

        public event EventHandler ClientStatusChanged;

        public static MinerManager GetInstance()
        {
            if (instance == null)
            {
                instance = new MinerManager();
                instance.LoadCurrentInfo();
            }

            return instance;
        }

        private MinerManager()
        {
            logger.Trace("Initializing MinerManager.");

            ClientList = new List<MinerClient>();
            MachineList = new List<MinerMachine>();
            this.Version = ManagerConfig.Current.Version;
        }

        public string Version
        {
            get; private set;
        }

        public List<MinerClient> ClientList
        {
            get; private set;
        }

        public List<MinerMachine> MachineList
        {
            get; private set;
        }

        public void SaveCurrentInfo()
        {
            logger.Information("Start SaveCurrentInfo.");

            ManagerInfo.Current.Clients = this.ClientList;
            ManagerInfo.Current.Machines = this.MachineList;
            ManagerInfo.Current.SaveToFile();
        }

        public void LoadCurrentInfo()
        {
            logger.Information("Start LoadCurrentInfo.");

            ManagerInfo info = ManagerInfo.Load();
            this.ClientList = info.Clients;
            this.MachineList = info.Machines;
        }

        public void AddClient(MinerClient client)
        {
            logger.Information("Start AddClient.");

            if (client == null)
            {
                logger.Error("New Client should not be null");
                throw new ArgumentNullException("New Client should not be null");
            }

            client.StatusChanged += ClientStatusChanged;
            this.ClientList.Add(client);
            this.SaveCurrentInfo();
        }

        public void RemoveClient(MinerClient client)
        {
            logger.Information("Start AddClient.");

            if (client == null)
            {
                logger.Error("Client should not be null");
                throw new ArgumentNullException("Client should not be null");
            }

            this.ClientList.Remove(client);
            this.SaveCurrentInfo();
        }

        public void AddMachine(MinerMachine machine)
        {
            logger.Information("Start AddMachine.");
            if (machine == null)
            {
                logger.Error("Machine should not be null");
                throw new ArgumentNullException("Machine should not be null");
            }

            MinerMachine existingMachine = this.MachineList.First(m => m.FullName.Equals(machine.FullName));
            if (existingMachine != null)
            {
                // Duplicated machines, just ignore except adding the unknown devices
                if (machine.Devices.Count > existingMachine.Devices.Count)
                {
                    existingMachine.Devices = machine.Devices;
                }
            }
            else
            {
                this.MachineList.Add(machine);
            }

            this.SaveCurrentInfo();
        }
    }
}
