using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI.Controls
{
    public class MachineConfigurationDataGridItem
    {
        private MinerClient client = null;

        public MachineConfigurationDataGridItem(MinerClient client)
        {
            if (client == null || client.Machine == null)
            {
                throw new ArgumentNullException("MachineConfigurationDataGridItem has null data.");
            }

            this.client = client;
        }

        public MinerClient Client
        {
            get
            {
                return this.client;
            }
        }

        public MinerMachine GetMachine()
        {
            return client.Machine;
        }

        public string MachineFullName
        {
            get
            {
                return client.Machine.FullName;
            }
        }

        public string ConfigureStatus
        {
            get
            {
                return (client.CurrentDeploymentStatus == MinerClient.DeploymentStatus.Ready) ? "Done" : string.Empty;
            }
        }
    }
}
