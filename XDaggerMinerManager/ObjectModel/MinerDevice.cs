using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    public class MinerDevice
    {
        public MinerDevice(string deviceId, string displayName, string deviceVersion, string driverVersion)
        {
            this.DeviceId = deviceId;
            this.DisplayName = displayName;
            this.DeviceVersion = deviceVersion;
            this.DriverVersion = driverVersion;
        }

        public string DeviceId
        {
            get; private set;
        }

        public string DisplayName
        {
            get; private set;
        }

        public string DeviceVersion
        {
            get; private set;
        }

        public string DriverVersion
        {
            get; private set;
        }

    }
}
