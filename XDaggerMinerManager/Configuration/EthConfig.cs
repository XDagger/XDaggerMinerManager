using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Configuration
{
    public class EthConfig
    {
        [JsonProperty(PropertyName = "wallet_address")]
        public string WalletAddress
        {
            get; set;
        }

        [JsonProperty(PropertyName = "email")]
        public string Email
        {
            get; set;
        }

        [JsonProperty(PropertyName = "worker_name")]
        public string WorkerName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "worker_password")]
        public string WorkerPassword
        {
            get; set;
        }

        [JsonProperty(PropertyName = "pool_index")]
        public int? PoolIndex
        {
            get; set;
        }

        [JsonProperty(PropertyName = "pool_host_index")]
        public int? PoolHostIndex
        {
            get; set;
        }

        [JsonProperty(PropertyName = "pool_full_address")]
        public string PoolFullAddress
        {
            get; set;
        }
    }
}
