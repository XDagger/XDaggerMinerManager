using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    public class MachineConnectivity
    {
        public MachineConnectivity(MinerMachine machine)
        {
            this.Machine = machine;
        }

        public MinerMachine Machine
        {
            get; private set;
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

        public bool IsAllTestingSuccess()
        {
            /*
            return this.CanPing.HasValue && this.CanPing.Value
                && this.CanRemotePathAccess.HasValue && this.CanRemotePathAccess.Value
                && this.CanRemotePowershell.HasValue && this.CanRemotePowershell.Value;
                */
            return  this.CanRemotePathAccess.HasValue && this.CanRemotePathAccess.Value
                 && this.CanRemotePowershell.HasValue && this.CanRemotePowershell.Value;
        }

        public void ResetFailureResults()
        {
            if (this.CanPing.HasValue && !this.CanPing.Value)
            {
                this.CanPing = null;
            }

            if (this.CanRemotePathAccess.HasValue && !this.CanRemotePathAccess.Value)
            {
                this.CanRemotePathAccess = null;
            }
            if (this.CanRemotePowershell.HasValue && !this.CanRemotePowershell.Value)
            {
                this.CanRemotePowershell = null;
            }
        }

        public string FullResult()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"MachineName=[{ this.Machine.FullName }] UserName=[{ this.Machine.Credential?.UserName }]");
            builder.Append($"CanPing=[{ this.CanPing }], CanRemotePathAccess=[{ this.CanRemotePathAccess }], CanRemotePowershell=[{ this.CanRemotePowershell }]");
            
            return builder.ToString();
        }
    }
}
