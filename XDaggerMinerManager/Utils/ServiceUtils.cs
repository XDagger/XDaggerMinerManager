using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Utils
{
    public class ServiceUtils
    {
        public static readonly string ServiceNameBase = @"XDaggerMinerService";
        public static readonly string ServiceBinaryName = @"XDaggerMinerService.exe";

        public static readonly string ServiceNameEthBase = @"XDaggerMinerEthService";

        public static bool HasExistingService(string machineName)
        {
            if (CheckServiceExist(ServiceNameBase, machineName))
            {
                return true;
            }

            int instanceNumber = 1;
            while (CheckServiceExist(GetServiceName(instanceNumber.ToString()), machineName))
            {
                instanceNumber++;
            }

            return (instanceNumber > 1);
        }

        public static string DetectAvailableInstanceId(string machineName)
        {
            if (!CheckServiceExist(ServiceNameBase, machineName))
            {
                return string.Empty;
            }

            int instanceNumber = 1;
            while (CheckServiceExist(GetServiceName(instanceNumber.ToString()), machineName))
            {
                instanceNumber++;
            }

            return instanceNumber.ToString();
        }

        public static string GetServiceName(string instanceId)
        {
            return (string.IsNullOrEmpty(instanceId) ? ServiceNameBase : string.Format("{0}_{1}", ServiceNameBase, instanceId));
        }

        public static bool CheckServiceExist(string serviceName, string machineName = "localhost")
        {
            ServiceController[] services = ServiceController.GetServices(machineName);
            return (services.FirstOrDefault(s => s.ServiceName == serviceName) != null);
        }


    }
}
