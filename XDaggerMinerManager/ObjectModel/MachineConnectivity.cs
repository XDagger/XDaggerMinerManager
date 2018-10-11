using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    public class MachineConnectivity
    {
        public MachineConnectivity()
        {

        }

        public bool? CanPing
        {
            get; set;
        }

        public bool? CanRemotePathAccess
        {
            get; set;
        }

        public bool? CanRemotePowershell
        {
            get; set;
        }
    }
}
