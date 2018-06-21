using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.ObjectModel
{
    public class MinerManager
    {
        private static readonly string defaultInfoFileName = @"manager-info.json";

        private static MinerManager instance = null;

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
            ClientList = new List<MinerClient>();
        }

        public string Version
        {
            get; private set;
        }

        public List<MinerClient> ClientList
        {
            get; private set;
        }

        public void SaveCurrentInfo()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);

            using (StreamWriter sw = new StreamWriter(Path.Combine(directoryPath, defaultInfoFileName)))
            {
                ManagerInfo info = new ManagerInfo();
                info.Version = this.Version;
                info.Clients = this.ClientList;

                string content = JsonConvert.SerializeObject(info, Formatting.Indented);
                sw.Write(content);
            }
        }

        public void LoadCurrentInfo()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);

            using (StreamReader sr = new StreamReader(Path.Combine(directoryPath, defaultInfoFileName)))
            {
                string jsonString = sr.ReadToEnd();
                ManagerInfo info = JsonConvert.DeserializeObject<ManagerInfo>(jsonString);

                this.Version = info.Version;
                this.ClientList = info.Clients;
            }
        }

        public void AddClient(MinerClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("New Client should not be null");
            }

            this.ClientList.Add(client);
            this.SaveCurrentInfo();
        }

        public void RemoveClient(MinerClient client)
        {
            this.ClientList.Remove(client);
            this.SaveCurrentInfo();
        }
    }
}
