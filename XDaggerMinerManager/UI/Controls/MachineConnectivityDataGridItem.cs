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
        private MachineConnectivity connectivity = null;

        public MachineConnectivityDataGridItem(MachineConnectivity connectivity)
        {
            if (connectivity == null || connectivity.Machine == null)
            {
                throw new ArgumentNullException("MachineConnectivity has null data.");
            }

            this.connectivity = connectivity;
        }

        public MachineConnectivity Connectivity
        {
            get
            {
                return this.connectivity;
            }
        }

        public MinerMachine GetMachine()
        {
            return connectivity.Machine;
        }

        public string FullName
        {
            get
            {
                return connectivity.Machine.FullName;
            }
        }

        public string IpAddressV4
        {
            get
            {
                return connectivity.Machine.IpAddressV4;
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
