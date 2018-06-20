using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.Utils
{
    public class ManagerInfo
    {
        [JsonProperty( PropertyName = "version")]
        public string Version
        {
            get; set;
        }

        [JsonProperty(PropertyName = "clients")]
        public List<MinerClient> Clients
        {
            get; set;
        }
    }
}
