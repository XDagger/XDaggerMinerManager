using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    /// <summary>
    /// 
    /// </summary>
    public class MinerMachine
    {
        public MinerMachine()
        {
            this.Devices = new List<MinerDevice>();
        }

        public static string TranslateHeaderName(string header)
        {
            switch(header)
            {
                case "FullMachineName": return "机器名称";
                case "IpAddressV4": return "地址";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "full_name")]
        public string FullName
        {
            get; set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "credential_id")]
        public string CredentialId
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public MachineCredential Credential
        {
            get; set;
        }

        [JsonProperty(PropertyName = "ip_address_v4")]
        public string IpAddressV4
        {
            get; set;
        }

        [JsonProperty(PropertyName = "devices")]
        public List<MinerDevice> Devices
        {
            get; set;
        }

    }
}
