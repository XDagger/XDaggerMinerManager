using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerAssistant
{
    
    public class ServiceController : System.ServiceProcess.ServiceController
    {
        private string m_ImagePath;
        private ServiceController[] m_DependentServices;
        private ServiceController[] m_ServicesDependedOn;

        public ServiceController()
            : base()
        {
        }

        public ServiceController(string name)
            : base(name)
        {
        }

        public ServiceController(string name, string machineName)
            : base(name, machineName)
        {
        }

        public string ImagePath
        {
            get
            {
                if (m_ImagePath == null)
                {
                    m_ImagePath = GetImagePath();
                }
                return m_ImagePath;
            }
        }

        public new ServiceController[] DependentServices
        {
            get
            {
                if (m_DependentServices == null)
                {
                    m_DependentServices =
                         ServiceController.GetServices(base.DependentServices);
                }
                return m_DependentServices;
            }
        }

        public new ServiceController[] ServicesDependedOn
        {
            get
            {
                if (m_ServicesDependedOn == null)
                {
                    m_ServicesDependedOn =
                          ServiceController.GetServices(base.ServicesDependedOn);
                }
                return m_ServicesDependedOn;
            }
        }

        public static new ServiceController[] GetServices()
        {
            return GetServices(".");
        }

        public static new ServiceController[] GetServices(string machineName)
        {
            return GetServices(System.ServiceProcess.ServiceController.GetServices(machineName));
        }

        private string GetImagePath()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
            ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                string name = obj["Name"] as string;
                string pathName = obj["PathName"] as string;

                if (name.Equals(ServiceName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return pathName.Replace("\"", "");
                }
            }

            return string.Empty;
        }

        private string GetImagePath2()
        {
            string registryPath = @"SYSTEM\CurrentControlSet\Services\" + ServiceName;
            RegistryKey keyHKLM = Registry.LocalMachine;

            RegistryKey key;
            if (MachineName != "")
            {
                key = RegistryKey.OpenRemoteBaseKey
                  (RegistryHive.LocalMachine, this.MachineName).OpenSubKey(registryPath);
            }
            else
            {
                key = keyHKLM.OpenSubKey(registryPath);
            }

            string value = key.GetValue("ImagePath").ToString();
            key.Close();
            return ExpandEnvironmentVariables(value);
            //return value;
        }

        private string ExpandEnvironmentVariables(string path)
        {
            if (MachineName == "")
            {
                return Environment.ExpandEnvironmentVariables(path);
            }
            else
            {
                string systemRootKey = @"Software\Microsoft\Windows NT\CurrentVersion\";

                RegistryKey key = RegistryKey.OpenRemoteBaseKey
                     (RegistryHive.LocalMachine, MachineName).OpenSubKey(systemRootKey);
                string expandedSystemRoot = key.GetValue("SystemRoot").ToString();
                key.Close();

                path = path.Replace("%SystemRoot%", expandedSystemRoot);
                return path;
            }
        }

        private static ServiceController[] GetServices
             (System.ServiceProcess.ServiceController[] systemServices)
        {
            List<ServiceController> services = new List<ServiceController>
                (systemServices.Length);
            foreach (System.ServiceProcess.ServiceController service in systemServices)
            {
                services.Add(new ServiceController
                    (service.ServiceName, service.MachineName));
            }
            return services.ToArray();
        }
    }
}
