using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace XDaggerMinerManager.Utils
{
    public class RemoteExecution
    {

        public RemoteExecution(string machineName)
        {
            this.MachineName = machineName;
        }

        public string MachineName
        {
            get; private set;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandFullLine"></param>
        /// <returns></returns>
        public string ExecuteCommand(string commandFullLine)
        {
            StringBuilder outputString = new StringBuilder();

            try
            {
                InitialSessionState initial = InitialSessionState.CreateDefault();
                using (Runspace runspace = RunspaceFactory.CreateRunspace(initial))
                {
                    runspace.Open();
                    PowerShell ps = PowerShell.Create();
                    ps.Runspace = runspace;
                    ps.AddCommand("Invoke-Command");
                    ps.AddParameter("ComputerName", this.MachineName);
                    ScriptBlock block = ScriptBlock.Create(commandFullLine);
                    ps.AddParameter("ScriptBlock", block);
                    foreach (PSObject obj in ps.Invoke())
                    {
                        outputString.Append(obj.ToString());
                    }
                }

                return outputString.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Execute Command Failed: [{0}]", commandFullLine), ex);
            }
        }

        /// <summary>
        /// Execute the command and deserialize the string as JSON to the T object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandFullLine"></param>
        /// <returns></returns>
        public T ExecuteCommand<T>(string commandFullLine)
        {
            string resultString = ExecuteCommand(commandFullLine);

            try
            {
                T result = JsonConvert.DeserializeObject<T>(resultString);
                return result;
            }
            catch (FormatException ex)
            {
                throw new Exception("The output for command is not a valid Json format.", ex);
            }
        }
    }

    public class DeviceOutput
    {
        public DeviceOutput()
        {

        }

        public long DeviceId
        {
            get; set;
        }

        public string DisplayName
        {
            get; set;
        }

    }

}