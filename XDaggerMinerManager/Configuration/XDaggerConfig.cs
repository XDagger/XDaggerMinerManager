using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Configuration
{
    public class XDaggerConfig
    {
        [JsonProperty(PropertyName = "pool_address")]
        public string PoolAddress
        {
            get; set;
        }

        [JsonProperty(PropertyName = "wallet_address")]
        public string WalletAddress
        {
            get; set;
        }
    }
}
