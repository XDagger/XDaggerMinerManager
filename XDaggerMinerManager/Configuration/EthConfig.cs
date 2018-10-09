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
        #region Sub Data

        public enum PoolIndexes
        {
            eth2miners = 0,
            dwarfpool = 1,
            ethermine = 2,
            ethpool = 3,
            f2pool = 4,
            miningpoolhub = 5,
            nanopool = 6,
            nicehash = 7,
            sparkpool = 8,
            whaleburg = 9,
        }

        public static List<string> PoolDisplayNames = new List<string>()
        {
            "2miners.com",
            "dwarfpool.org",
            "ethermine.org",
            "ethpool.org",
            "f2pool.com",
            "miningpoolhub.com",
            "nanopool.org",
            "nicehash.com",
            "sparkpool.com",
            "whalesburg.com"
        };

        public static List<List<string>> PoolAddressTemplates = new List<List<string>>()
        {
            new List<string>() { @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:2020" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET@HOSTURL:8008/WORKERNAME/EMAIL", @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:8008" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:4444", @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:5555" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:3333" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:8008" },
            new List<string>() { @"stratum2+tcp://USERNAME.WORKERNAME:WORKERPWD@HOSTURL:20535" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET@HOSTURL:9999/WORKERNAME/EMAIL", @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:9999" },
            new List<string>() { @"stratum2+tcp://BTC_WALLET.WORKERNAME@HOSTURL:3353" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:3333" },
            new List<string>() { @"stratum1+tcp://ETH_WALLET.WORKERNAME@HOSTURL:8082" }
        };

        public static List<List<string>> PoolHostUrls = new List<List<string>>()
        {
            new List<string>() { "eth.2miners.com" },
            new List<string>() {
                "eth-ar.dwarfpool.com",
                "eth-asia.dwarfpool.com",
                "eth-au.dwarfpool.com"
            },
            new List<string>() {
                "asia1.ethermine.org",
                "eu1.ethermine.org",
                "us1.ethermine.org",
                "us2.ethermine.org"
            },
            new List<string>() {
                "asia1.ethpool.org",
                "eu1.ethpool.org",
                "us1.ethpool.org"
            },
            new List<string>() {
                "eth.f2pool.com"
            },
            new List<string>() {
                "asia.ethash-hub.miningpoolhub.com",
                "europe.ethash-hub.miningpoolhub.com",
                "us-east.ethash-hub.miningpoolhub.com"
            },
            new List<string>() {
                "eth-asia1.nanopool.org"
            },
            new List<string>() {
                "daggerhashimoto.br.nicehash.com",
                "daggerhashimoto.eu.nicehash.com"
            },
            new List<string>() {
                "cn.sparkpool.com",
                "eu.sparkpool.com"
            },
            new List<string>() {
                "proxy.pool.whalesburg.com"
            }
        };

        #endregion

        #region Properties

        [JsonProperty(PropertyName = "pool_index")]
        public PoolIndexes? PoolIndex
        {
            get; set;
        }

        [JsonProperty(PropertyName = "pool_host_index")]
        public int? PoolHostIndex
        {
            get; set;
        }

        [JsonProperty(PropertyName = "wallet_address")]
        public string WalletAddress
        {
            get; set;
        }

        [JsonProperty(PropertyName = "email_address")]
        public string EmailAddress
        {
            get; set;
        }

        [JsonProperty(PropertyName = "worker_name")]
        public string WorkerName
        {
            get; set;
        }

        /// <summary>
        ///  This is only used when pool is miningpoolhub.com
        /// </summary>
        [JsonProperty(PropertyName = "user_name")]
        public string UserName
        {
            get; set;
        }

        /// <summary>
        ///  This is only used when pool is miningpoolhub.com
        /// </summary>
        [JsonProperty(PropertyName = "worker_password")]
        public string WorkerPassword
        {
            get; set;
        }

        [JsonProperty(PropertyName = "is_ssl")]
        public bool? IsSSL
        {
            get; set;
        }
        
        /// <summary>
        /// This is only used when pool is nicehash.com
        /// </summary>
        public string BtcWalletAddress
        {
            get; set;
        }

        #endregion

        #region Public Methods

        [JsonProperty(PropertyName = "pool_full_address")]
        public string PoolFullAddress
        {
            get
            {
                try
                {
                    this.ValidateProperties();
                }
                catch(Exception)
                {
                    // If validation fail, just return empty string
                    return string.Empty;
                }

                int templateId = DecideTemplateId();

                string poolAddress = PoolAddressTemplates[PoolIndex.GetHashCode()][templateId];
                string poolHostUrl = PoolHostUrls[PoolIndex.GetHashCode()][PoolHostIndex.Value];

                poolAddress = poolAddress.Replace("ETH_WALLET", this.WalletAddress);
                poolAddress = poolAddress.Replace("WORKERNAME", this.WorkerName);
                poolAddress = poolAddress.Replace("HOSTURL", poolHostUrl);
                poolAddress = poolAddress.Replace("EMAIL", this.EmailAddress);
                poolAddress = poolAddress.Replace("USERNAME", this.UserName);
                poolAddress = poolAddress.Replace("WORKERPWD", this.WorkerPassword);
                poolAddress = poolAddress.Replace("BTC_WALLET", this.BtcWalletAddress);

                return poolAddress;
            }
        }

        /// <summary>
        /// This method will throw exception if the validation fail.
        /// </summary>
        public void ValidateProperties()
        {
            // Wallet Address
            if (PoolIndex != PoolIndexes.nicehash)
            {
                if (string.IsNullOrEmpty(this.WalletAddress) || !this.WalletAddress.StartsWith("0x"))
                {
                    throw new FormatException("EthWalletAddress missing or format error.");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(this.BtcWalletAddress) || !this.BtcWalletAddress.StartsWith("0x"))
                {
                    throw new FormatException("BtcWalletAddress missing or format error for nicehash pool.");
                }
            }

            // Host Url Index
            int indexCode = PoolIndex.GetHashCode();
            if (indexCode < 0 || indexCode > PoolHostUrls.Count)
            {
                throw new IndexOutOfRangeException("Index is out of range.");
            }

            if (PoolHostIndex < 0 || PoolHostIndex >= PoolHostUrls[indexCode].Count)
            {
                throw new IndexOutOfRangeException("Host URL index is out of range.");
            }

            // Special Checks
            if (string.IsNullOrEmpty(this.WorkerName))
            {
                throw new FormatException("WorkerName missing or format error.");
            }

            if (PoolIndex == PoolIndexes.miningpoolhub)
            {
                this.UserName = this.WorkerName;

                if (string.IsNullOrEmpty(this.UserName))
                {
                    throw new FormatException("UserName missing for the miningpoolhub pool.");
                }

                if (string.IsNullOrEmpty(this.WorkerPassword))
                {
                    throw new FormatException("WorkerPWD missing for the miningpoolhub pool.");
                }
            }

            if (PoolIndex == PoolIndexes.ethermine)
            {
                // TODO: Add this to config UI later
                this.IsSSL = false;

                if (this.IsSSL == null)
                {
                    throw new ArgumentNullException("IsSSL is not specified for ethermine pool.");
                }
            }
        }

        public bool IsEmptyConfig()
        {
            return (PoolIndex == null)
                && string.IsNullOrEmpty(WalletAddress)
                && string.IsNullOrEmpty(WorkerName)
                && string.IsNullOrEmpty(EmailAddress)
                && string.IsNullOrEmpty(UserName)
                && string.IsNullOrEmpty(WorkerPassword)
                && IsSSL == null;
        }

        public EthConfig CloneWithUpdate(EthConfig updatedConfig)
        {
            if (updatedConfig == null)
            {
                throw new ArgumentNullException("updatedConfig should not be null.");
            }

            EthConfig another = new EthConfig();

            another.WalletAddress = string.IsNullOrEmpty(updatedConfig.WalletAddress) ? this.WalletAddress : updatedConfig.WalletAddress;
            another.WorkerName = string.IsNullOrEmpty(updatedConfig.WorkerName) ? this.WorkerName : updatedConfig.WorkerName;
            another.EmailAddress = string.IsNullOrEmpty(updatedConfig.EmailAddress) ? this.EmailAddress : updatedConfig.EmailAddress;
            another.UserName = string.IsNullOrEmpty(updatedConfig.UserName) ? this.UserName : updatedConfig.UserName;
            another.WorkerPassword = string.IsNullOrEmpty(updatedConfig.WorkerPassword) ? this.WorkerPassword : updatedConfig.WorkerPassword;

            if (updatedConfig.PoolIndex == null)
            {
                another.PoolIndex = this.PoolIndex;
                another.PoolHostIndex = this.PoolHostIndex;
            }
            else
            {
                another.PoolIndex = updatedConfig.PoolIndex;
                another.PoolHostIndex = updatedConfig.PoolHostIndex;
            }


            return another;
        }

        #endregion

        #region Private Methods

        private int DecideTemplateId()
        {
            if (PoolIndex == PoolIndexes.dwarfpool || PoolIndex == PoolIndexes.nanopool)
            {
                return (string.IsNullOrEmpty(EmailAddress) ? 0 : 1);
            }

            if (PoolIndex == PoolIndexes.ethermine)
            {
                return this.IsSSL.Value ? 1 : 0;
            }

            return 0;
        }

        #endregion
    }
}
