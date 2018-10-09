using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Configuration
{

    /*
    public class EthMinerPoolHelper
    {
        public enum PoolIndex
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

        public EthMinerPoolHelper()
        {

        }

        public PoolIndex Index
        {
            get;set;
        }

        public int HostIndex
        {
            get;set;
        }

        public string WorkerName
        {
            get; set;
        }

        public bool? IsSSL
        {
            get;set;
        }

        /// <summary>
        /// This should be a Ethereum wallet number including the leading 0x.
        /// Sample: 0x1234567890ABCDEF1234567890abcdef12345678
        /// </summary>
        public string EthWalletAddress
        {
            get;set;
        }

        public string EmailAddress
        {
            get;set;
        }

        /// <summary>
        ///  This is only used when pool is miningpoolhub.com
        /// </summary>
        public string UserName
        {
            get;set;
        }

        /// <summary>
        ///  This is only used when pool is miningpoolhub.com
        /// </summary>
        public string WorkerPassword
        {
            get;set;
        }

        /// <summary>
        /// This is only used when pool is nicehash.com
        /// </summary>
        public string BtcWalletAddress
        {
            get;set;
        }

        public string GeneratePoolAddress()
        {
            this.ValidateProperties();

            int templateId = DecideTemplateId();

            string poolAddress = PoolAddressTemplates[Index.GetHashCode()][templateId];
            string poolHostUrl = PoolHostUrls[Index.GetHashCode()][HostIndex];

            poolAddress = poolAddress.Replace("ETH_WALLET", this.EthWalletAddress);
            poolAddress = poolAddress.Replace("WORKERNAME", this.WorkerName);
            poolAddress = poolAddress.Replace("HOSTURL", poolHostUrl);
            poolAddress = poolAddress.Replace("EMAIL", this.EmailAddress);
            poolAddress = poolAddress.Replace("USERNAME", this.UserName);
            poolAddress = poolAddress.Replace("WORKERPWD", this.WorkerPassword);
            poolAddress = poolAddress.Replace("BTC_WALLET", this.BtcWalletAddress);

            return poolAddress;
        }

        private int DecideTemplateId()
        {
            if (Index == PoolIndex.dwarfpool || Index == PoolIndex.nanopool)
            {
                return (string.IsNullOrEmpty(EmailAddress) ? 0 : 1);
            }

            if (Index == PoolIndex.ethermine)
            {
                return this.IsSSL.Value ? 1 : 0;
            }

            return 0;
        }

        private void ValidateProperties()
        {
            // Wallet Address
            if (Index != PoolIndex.nicehash)
            {
                if (string.IsNullOrEmpty(this.EthWalletAddress) || !this.EthWalletAddress.StartsWith("0x"))
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
            int indexCode = Index.GetHashCode();
            if (indexCode < 0 || indexCode > PoolHostUrls.Count)
            {
                throw new IndexOutOfRangeException("Index is out of range.");
            }

            if (HostIndex < 0 || HostIndex >= PoolHostUrls[indexCode].Count)
            {
                throw new IndexOutOfRangeException("Host URL index is out of range.");
            }

            // Special Checks
            if (string.IsNullOrEmpty(this.WorkerName))
            {
                throw new FormatException("WorkerName missing or format error.");
            }

            if (Index == PoolIndex.miningpoolhub)
            {
                if (string.IsNullOrEmpty(this.UserName))
                {
                    throw new FormatException("UserName missing for the miningpoolhub pool.");
                }

                if (string.IsNullOrEmpty(this.WorkerPassword))
                {
                    throw new FormatException("WorkerPWD missing for the miningpoolhub pool.");
                }
            }

            if (Index == PoolIndex.ethermine)
            {
                if (this.IsSSL == null)
                {
                    throw new ArgumentNullException("IsSSL is not specified for ethermine pool.");
                }
            }
        }
    }


    */
}
