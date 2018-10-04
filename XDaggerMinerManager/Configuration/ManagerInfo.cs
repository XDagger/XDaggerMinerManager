using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using XDaggerMinerManager.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using XDaggerMinerManager.Utils;
using System.Linq;

namespace XDaggerMinerManager.Configuration
{
    public class ManagerInfo
    {
        private static readonly string defaultInfoFileName = @"manager-info.json";

        private static ManagerInfo instance = null;

        public static ManagerInfo Current
        {
            get
            {
                if (instance == null)
                {
                    instance = ManagerInfo.Load();
                }

                return instance;
            }
        }

        public static ManagerInfo Load()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);

            string fullPath = Path.Combine(directoryPath, defaultInfoFileName);

            if (File.Exists(fullPath))
            {
                using (StreamReader sr = new StreamReader(fullPath))
                {
                    string jsonString = sr.ReadToEnd();
                    ManagerInfo info = JsonConvert.DeserializeObject<ManagerInfo>(jsonString);

                    if (info.Machines == null)
                    {
                        info.Machines = new List<MinerMachine>();
                    }

                    if (info.Credentials == null)
                    {
                        info.Credentials = new List<MachineCredential>();
                    }

                    if (info.Clients == null)
                    {
                        info.Clients = new List<MinerClient>();
                    }

                    return info;
                }
            }
            else
            {
                return new ManagerInfo();
            }
        }
        
        public void SaveToFile()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);

            using (StreamWriter sw = new StreamWriter(Path.Combine(directoryPath, defaultInfoFileName)))
            {
                string content = JsonConvert.SerializeObject(this, Formatting.Indented);
                sw.Write(content);
            }
        }

        public ManagerInfo()
        {
            this.LockPasswordHash = string.Empty;
            this.Clients = new List<MinerClient>();
            this.Credentials = new List<MachineCredential>();
            this.Machines = new List<MinerMachine>();
        }

        [JsonProperty(PropertyName = "lock_password")]
        public string LockPasswordHash
        {
            get; set;
        }

        [JsonProperty(PropertyName = "machines")]
        public List<MinerMachine> Machines
        {
            get; set;
        }

        [JsonProperty(PropertyName = "credentials")]
        public List<MachineCredential> Credentials
        {
            get; set;
        }

        [JsonProperty(PropertyName = "clients")]
        public List<MinerClient> Clients
        {
            get; set;
        }

        /// <summary>
        /// Returns the credentialId
        /// </summary>
        /// <param name="credential"></param>
        /// <returns></returns>
        public string AddOrUpdateCredential(MachineCredential credential)
        {
            MachineCredential existingCredential = this.Credentials.FirstOrDefault(c => c.UserName.Equals(credential.UserName));
            if (existingCredential != null)
            {
                // Update the password and return the Id
                existingCredential.SetLoginPassword(credential.LoginPlainPassword);
                return existingCredential.Id;
            }
            else
            {
                // Generate credential Id
                credential.Id = RandomUtils.GenerateString(6, (cId => !this.Credentials.Any(c => c.Id == cId)));
                this.Credentials.Add(credential);

                return credential.Id;
            }
        }

        public void AddOrUpdateMachine(MinerMachine machine)
        {
            MinerMachine existingMachine = this.Machines.FirstOrDefault(m => m.FullName.Equals(machine.FullName));
            if (existingMachine != null)
            {
                // Duplicated machines, update the machine information
                if (machine.Devices.Count > existingMachine.Devices.Count)
                {
                    existingMachine.Devices = machine.Devices;
                }

                if (!string.IsNullOrEmpty(machine.IpAddressV4))
                {
                    existingMachine.IpAddressV4 = machine.IpAddressV4;
                }

                if(!string.IsNullOrEmpty(machine.CredentialId))
                {
                    existingMachine.CredentialId = machine.CredentialId;
                }
            }
            else
            {
                this.Machines.Add(machine);
            }
        }

        public bool HasLockPassword()
        {
            return !string.IsNullOrEmpty(LockPasswordHash);
        }

        public void SetLockPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("password");
            }

            this.LockPasswordHash = SecureStringHelper.HashPassword(password);
            this.SaveToFile();
        }
    }
}
