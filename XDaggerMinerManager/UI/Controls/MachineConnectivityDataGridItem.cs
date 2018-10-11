using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI.Controls
{
    public class MachineConnectivityDataGridItem
    {
        private MinerMachine minerMachine = null;

        private MachineConnectivity connectivity = null;

        public MachineConnectivityDataGridItem(MinerMachine machine, MachineConnectivity connectivity = null)
        {
            minerMachine = machine;
            this.connectivity = connectivity;
        }

        public MinerMachine GetMachine()
        {
            return minerMachine;
        }

        public string FullName
        {
            get
            {
                return minerMachine.FullName;
            }
        }

        public string IpAddressV4
        {
            get
            {
                return minerMachine.IpAddressV4;
            }
        }

        public string CanPing
        {
            get
            {
                if (connectivity == null || connectivity.CanPing == null)
                {
                    return string.Empty;
                }

                if (connectivity.CanPing.Value)
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }
        }

        public string CanRemotePathAccess
        {
            get
            {
                if (connectivity == null || connectivity.CanRemotePathAccess == null)
                {
                    return string.Empty;
                }

                if (connectivity.CanRemotePathAccess.Value)
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }
        }
        public string CanRemotePowershell
        {
            get
            {
                if (connectivity == null || connectivity.CanRemotePowershell == null)
                {
                    return string.Empty;
                }

                if (connectivity.CanRemotePowershell.Value)
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }
        }
    }
}
