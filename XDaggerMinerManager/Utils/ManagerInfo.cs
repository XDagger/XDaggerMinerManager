using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using XDaggerMinerManager.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace XDaggerMinerManager.Utils
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

        [JsonProperty(PropertyName = "clients")]
        public List<MinerClient> Clients
        {
            get; set;
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

            this.LockPasswordHash = HashPassword(password);
            this.SaveToFile();
        }

        public bool IsPasswordMatch(string password)
        {
            if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(this.LockPasswordHash))
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(this.LockPasswordHash))
            {
                return false;
            }

            return (HashPassword(password) == this.LockPasswordHash);
        }

        private static string HashPassword(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }

            return hashString;
        }
    }
}
