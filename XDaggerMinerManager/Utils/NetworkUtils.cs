using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.Utils
{
    public class NetworkUtils
    {
        public static List<MinerMachine> DetectMachinesInLocalNetwork()
        {

        }


        private static List<string> GetLocalNetworkIPs()
        {
            List<string> result = new List<string>();

            LocalExecutor executor = new LocalExecutor();
            StreamReader sr = executor.ExecuteCommandWithStreamOutput("arp.exe", "-a");

            string line = string.Empty;
            while((line = sr.ReadLine()) != null)
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

            sr.Close();

            return result;
        }

        private static string GetMachineName(string ipAddress)
        {

        }

    }
}
