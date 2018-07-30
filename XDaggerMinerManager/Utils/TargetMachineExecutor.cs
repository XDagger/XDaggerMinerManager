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
        public ExecutionResult<T> ExecuteCommand<T>(string commandFullLine, string arguments = "")
        {
            try
            {
                string resultString = ExecuteCommand(commandFullLine, arguments);

                return ExecutionResult<T>.Parse(resultString);
            }
            catch(Exception ex)
            {
                return ExecutionResult<T>.ErrorResult(30000, ex.Message);
            }
        }
    }

    public class ExecutionResult<T>
    {
        public static ExecutionResult<T> ErrorResult(int code, string message)
        {
            ExecutionResult<T> result = new ExecutionResult<T>();
            result.Code = code;
            result.ErrorMessage = message;

            return result;
        }

        public static ExecutionResult<T> Parse(string rawOutput)
        {
            string[] resultStrings = rawOutput.Split(new string[] { "||" }, StringSplitOptions.None);

            if (resultStrings.Length != 2)
            {
                throw new FormatException("Error while parsing Execution Result: " + rawOutput);
            }

            int code = 0;
            if (!Int32.TryParse(resultStrings[0], out code))
            {
                throw new FormatException("Error while parsing Execution Result: " + rawOutput);
            }

            ExecutionResult<T> result = new ExecutionResult<T>();
            result.Code = code;

            if (code == 0)
            {
                try
                {
                    result.Data = JsonConvert.DeserializeObject<T>(resultStrings[1]);
                }
                catch (FormatException ex)
                {
                    throw new Exception("The output for command is not a valid Json format.", ex);
                }
            }
            else
            {
                result.ErrorMessage = resultStrings[1];
            }

            return result;
        }

        public ExecutionResult()
        {

        }

        public bool HasError
        {
            get
            {
                return (this.Code != 0) || !string.IsNullOrEmpty(this.ErrorMessage);
            }
        }

        public string ErrorMessage
        {
            get; private set;

        }

        public T Data
        {
            get; private set;
        }

        public int Code
        {
            get; private set;
        }

    }

    public class OKResult
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class DeviceOutput
    {
        public DeviceOutput()
        {

        }

        public string DeviceId
        {
            get; set;
        }

        public string DisplayName
        {
            get; set;
        }

        public string DeviceVersion
        {
            get; set;
        }

        public string DriverVersion
        {
            get; set;
        }

    }

    public class MessageOutput
    {
        public MessageOutput()
        {

        }

        public string Message
        {
            get; set;
        }
    }

    public class ReportOutput
    {
        public ReportOutput()
        {

        }

        /// <summary>
        /// Status: Unknown, NotInstalled, Stopped, Disconnected, Connected, Mining
        /// </summary>
        public enum StatusEnum
        {
            Unknown = 0,
            NotInstalled = 1,
            Stopped = 2,
            Disconnected = 3,
            Connected = 4,
            Mining = 5
        }

        public StatusEnum Status
        {
            get; set;
        }

        public string StatusString
        {
            get
            {
                return this.Status.ToString();
            }
        }

        /// <summary>
        /// Unit: Mhash per second
        /// </summary>
        public double HashRate
        {
            get; set;
        }
        
    }
}
