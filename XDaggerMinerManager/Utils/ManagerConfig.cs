using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Utils
{
    public class ManagerConfig
    {
        private static readonly string defaultConfigFileName = @"manager-config.json";

        private static ManagerConfig instance = null;

        public static ManagerConfig Current
        {
            get
            {
                if (instance == null)
                {
                    instance = ManagerConfig.ReadFromFile();
                }

                return instance;
            }
        }

        public static ManagerConfig ReadFromFile()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);

            using (StreamReader sr = new StreamReader(Path.Combine(directoryPath, defaultConfigFileName)))
            {
                string jsonString = sr.ReadToEnd();
                ManagerConfig config = JsonConvert.DeserializeObject<ManagerConfig>(jsonString);
                return config;
            }
        }

        public void SaveToFile()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(location);
            
            using (StreamWriter sw = new StreamWriter(Path.Combine(directoryPath, defaultConfigFileName)))
            {
                string content = JsonConvert.SerializeObject(this, Formatting.Indented);
                sw.Write(content);
            }
        }

        public ManagerConfig()
        {

        }

        [JsonProperty(PropertyName = "version")]
        public string Version
        {
            get; set;
        }

        [JsonProperty(PropertyName = "miner_url_path")]
        public string MinerDownloadUrlPath
        {
            get; set;
        }

        [JsonProperty(PropertyName = "miner_package_name")]
        public string MinerPackageName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "default_install_path")]
        public string DefaultInstallationPath
        {
            get; set;
        }

        [JsonProperty(PropertyName = "default_username")]
        public string DefaultUserName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "default_password")]
        public string DefaultPassword
        {
            get; set;
        }

        [JsonProperty(PropertyName = "default_wallet")]
        public string DefaultWallet
        {
            get; set;
        }
    }
}
