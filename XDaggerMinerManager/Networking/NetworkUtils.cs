using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.Networking
{
    public class NetworkUtils
    {
        public static List<MinerMachine> DetectMachinesInLocalNetwork()
        {
            List<MinerMachine> machineList = new List<MinerMachine>();

            List<string> ipAddressList = GetLocalNetworkIPs();

            foreach(string ipAddress in ipAddressList)
            {
                if (ipAddress.EndsWith(".1") || ipAddress.EndsWith(".255"))
                {
                    continue;
                }

                string name = GetMachineName(ipAddress);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                MinerMachine machine = new MinerMachine() { FullName = name, IpAddressV4 = ipAddress };
                machineList.Add(machine);
            }

            return machineList;
        }
        
        private static List<string> GetLocalNetworkIPs()
        {
            List<string> result = new List<string>();

            LocalExecutor executor = new LocalExecutor();
            executor.ExecuteCommandWithStreamOutput("arp.exe", "-a",
                (reader) =>
                {
                    string line = string.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("  "))
                        {
                            var items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (items.Length == 3)
                            {
                                result.Add(items[0]);
                            }
                        }
                    }

                    return string.Empty;
                });

            return result;
        }

        private static string GetMachineName(string ipAddress)
        {
            LocalExecutor executor = new LocalExecutor();
            return executor.ExecuteCommandWithStreamOutput("nslookup", ipAddress,
                (reader) =>
                {
                    string line = string.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Name:"))
                        {
                            var items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (items.Length == 2)
                            {
                                return items[1];
                            }
                        }
                    }

                    return string.Empty;
                });
        }

    }
}
