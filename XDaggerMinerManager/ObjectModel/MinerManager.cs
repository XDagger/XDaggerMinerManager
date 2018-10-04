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

        private ManagerConfig managerConfig = null;

        private ManagerInfo managerInfo = null;

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

            managerConfig = ManagerConfig.Current;
            managerInfo = ManagerInfo.Current;
        }

        public string Version
        {
            get
            {
                return managerConfig.Version;
            }
        }

        public List<MinerClient> ClientList
        {
            get
            {
                return managerInfo.Clients;
            }
        }

        public List<MinerMachine> MachineList
        {
            get
            {
                return managerInfo.Machines;
            }
        }

        public List<MachineCredential> CredentialList
        {
            get
            {
                return managerInfo.Credentials;
            }
        }

        public void SaveCurrentInfo()
        {
            logger.Information("Start SaveCurrentInfo.");
            
            ManagerInfo.Current.SaveToFile();
        }

        public void LoadCurrentInfo()
        {
            logger.Information("Start LoadCurrentInfo.");
            
            // Fill the rich object data to each object
            foreach(MinerMachine machine in managerInfo.Machines)
            {
                if(!string.IsNullOrEmpty(machine.CredentialId))
                {
                    MachineCredential credential = managerInfo.Credentials.FirstOrDefault(c => c.Id == machine.CredentialId);
                    if (credential == null)
                    {
                        throw new InvalidDataException($"LoadCurrentInfo error: cannot find CredentialId { machine.CredentialId } in ManagerInfo.");
                    }

                    machine.Credential = credential;
                }
            }

            foreach(MinerClient client in managerInfo.Clients)
            {
                MinerMachine machine = managerInfo.Machines.FirstOrDefault(m => m.FullName == client.MachineFullName);
                if(machine == null)
                {
                    throw new InvalidDataException($"LoadCurrentInfo error: cannot find machine with name { client.MachineFullName } in ManagerInfo.");
                }

                client.Machine = machine;
            }
        }

        public void AddClient(MinerClient client)
        {
            logger.Information("Start AddClient.");

            if (client == null)
            {
                logger.Error("New Client should not be null");
                throw new ArgumentNullException("New Client should not be null");
            }

            if (client.Machine == null)
            {
                logger.Error("New Client Machine should not be null");
                throw new ArgumentNullException("New Client Machine should not be null");
            }

            client.StatusChanged += ClientStatusChanged;
            client.MachineFullName = client.Machine.FullName;

            MinerMachine newMachine = client.Machine;
            MachineCredential newCredential = newMachine.Credential;
            if (newCredential != null)
            {
                newMachine.CredentialId = managerInfo.AddOrUpdateCredential(newMachine.Credential);
            }

            managerInfo.AddOrUpdateMachine(newMachine);

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

        private void AddMachine(MinerMachine machine)
        {
            logger.Information("Start AddMachine.");
            if (machine == null)
            {
                logger.Error("Machine should not be null");
                throw new ArgumentNullException("Machine should not be null");
            }

            
            
        }
    }
}
