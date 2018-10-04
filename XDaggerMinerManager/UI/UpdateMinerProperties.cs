﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string EthFullPoolAddress
        {
            get; set;
        }

        public UpdateMinerProperties()
        {
            this.InstanceType = MinerClient.InstanceTypes.Unset;
            this.XDaggerPoolAddress = string.Empty;
            this.XDaggerWalletAddress = string.Empty;
            this.EthFullPoolAddress = string.Empty;

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

            client.InstanceTypeEnum = this.InstanceType;

            if (this.InstanceType == MinerClient.InstanceTypes.XDagger)
            {
                client.XDaggerWalletAddress = (!string.IsNullOrWhiteSpace(XDaggerWalletAddress) ? XDaggerWalletAddress : client.XDaggerWalletAddress);
                client.XDaggerPoolAddress = (!string.IsNullOrWhiteSpace(XDaggerPoolAddress) ? XDaggerPoolAddress : client.XDaggerPoolAddress);
            }
            else
            {
                client.EthFullPoolAddress = (!string.IsNullOrWhiteSpace(EthFullPoolAddress) ? EthFullPoolAddress : client.EthFullPoolAddress);
            }
        }
    }
}