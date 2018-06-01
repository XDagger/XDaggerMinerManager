using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    public class DisplayedMinerDevice
    {
        public DisplayedMinerDevice(long deviceId, string displayName)
        {
            this.DeviceId = deviceId;
            this.DisplayName = displayName;
        }

        public long DeviceId
        {
            get; private set;
        }

        public string DisplayName
        {
            get; private set;
        }


    }
}
