using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XDaggerMinerManager.Utils
{
    public abstract class TargetMachineExecutor
    {
        public TargetMachineExecutor()
        {

        }

        public static TargetMachineExecutor GetExecutor(string machineName)
        {
            string currentMachineName = Environment.MachineName.Split('.')[0];

            if (machineName.Equals("LOCALHOST", StringComparison.InvariantCultureIgnoreCase)
                || machineName.Equals(currentMachineName, StringComparison.InvariantCultureIgnoreCase))
            {
                return new LocalExecutor();
            }
            else
            {
                return new RemoteExecutor(machineName);
            }
        }

        public abstract string ExecuteCommand(string commandFile, string arguments = "");

        /// <summary>
        /// Execute the command and deserialize the string as JSON to the T object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandFullLine"></param>
        /// <returns></returns>
        public T ExecuteCommand<T>(string commandFullLine, string arguments = "")
        {
            string resultString = ExecuteCommand(commandFullLine, arguments);

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
