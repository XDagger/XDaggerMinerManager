using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.Configuration;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI
{
    public class UpdateMinerProperties
    {
        public MinerClient.InstanceTypes InstanceType
        {
            get; set;
        }

        public string XDaggerPoolAddress
        {
            get; set;
        }

        public string XDaggerWalletAddress
        {
            get; set;
        }

        public EthConfig EthConfig
        {
            get; set;
        }
        
        public string DeviceName
        {
            get; set;
        }

        public UpdateMinerProperties()
        {
            this.InstanceType = MinerClient.InstanceTypes.Unset;
            this.XDaggerPoolAddress = string.Empty;
            this.XDaggerWalletAddress = string.Empty;
            this.DeviceName = string.Empty;

            this.EthConfig = null;
        }

        public void UpdateClient(MinerClient client)
        {
            if (client == null)
            {
                return;
            }

            if (this.InstanceType == MinerClient.InstanceTypes.Unset)
            {
                // Since the type is not set, nothing should be done
                return;
            }

            if (!string.IsNullOrEmpty(this.DeviceName))
            {
                client.Device = client.Machine.Devices.FirstOrDefault(d => d.DisplayName == this.DeviceName);
            }

            client.InstanceTypeEnum = this.InstanceType;

            if (this.InstanceType == MinerClient.InstanceTypes.XDagger)
            {
                client.XDaggerConfig.WalletAddress = (!string.IsNullOrWhiteSpace(XDaggerWalletAddress) ? XDaggerWalletAddress : client.XDaggerConfig.WalletAddress);
                client.XDaggerConfig.PoolAddress = (!string.IsNullOrWhiteSpace(XDaggerPoolAddress) ? XDaggerPoolAddress : client.XDaggerConfig.PoolAddress);
            }
            else
            {
                if (this.EthConfig != null && !this.EthConfig.IsEmptyConfig())
                client.EthConfig = client.EthConfig.CloneWithUpdate(this.EthConfig);
            }
        }
    }
}
