using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace XDaggerMinerManager.Utils
{
    public class SystemInformation
    {
        public static string GetCPUInformation()
        {
            return GetWin32Information("Win32_Processor");
        }

        public static string GetWindowsVersion()
        {
            return Environment.OSVersion.ToString();
        }

        private static string GetWin32Information(string key)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + key);

            StringBuilder builder = new StringBuilder();
            foreach (ManagementObject share in searcher.Get())
            {
                builder.Append($"[{ share.ToString()}]");
                foreach(PropertyData data in share.Properties)
                {
                    builder.Append($"[{ data.ToString()}]");
                }
            }

            return builder.ToString();
        }


    }
}
