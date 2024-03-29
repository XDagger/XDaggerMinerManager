﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XDaggerMinerManager.ObjectModel;
using XDaggerMinerManager.Utils;

namespace XDaggerMinerManager.Utils
{
    public abstract class TargetMachineExecutor
    {
        protected string credentialUsername = string.Empty;

        protected bool disableTrace = false;

        protected Logger logger = Logger.GetInstance();

        protected SecureString credentialPassword = null;

        public TargetMachineExecutor()
        {

        }

        public virtual void SetCredential(string username, string password)
        {
            
        }

        public static TargetMachineExecutor GetExecutor(MinerMachine machine, bool disableTrace = false)
        {
            if (machine == null)
            {
                return null;
            }

            TargetMachineExecutor executor = GetExecutor(machine?.FullName);
            if (machine.Credential != null)
            {
                executor.SetCredential(machine.Credential.UserName, machine.Credential.LoginPlainPassword);
            }

            executor.disableTrace = disableTrace;

            return executor;
        }

        public static TargetMachineExecutor GetExecutor(string machineName)
        {
            string currentMachineName = Environment.MachineName.Split('.')[0];

            if (machineName.Equals("LOCALHOST", StringComparison.InvariantCultureIgnoreCase)
                || machineName.StartsWith(currentMachineName, StringComparison.InvariantCultureIgnoreCase))
            {
                //// return new RemoteExecutor(machineName);
                return new LocalExecutor();
            }
            else
            {
                return new RemoteExecutor(machineName);
            }
        }

        public abstract List<string> Execute(string commandFile, string arguments = "");

        public abstract void TestConnection();

        public string ExecuteCommand(string commandExec, string arguments = "")
        {
            List<string> resultStrings = Execute(commandExec, arguments);
            if (resultStrings == null)
            {
                return string.Empty;
            }

            foreach (var result in resultStrings)
            {
                string resultString = result.ToString();
                if (resultString.Contains("||"))
                {
                    return resultString;
                }
            }

            return string.Empty;
        }

        public T ExecuteCommandAndThrow<T>(string commandFullLine, string arguments = "")
        {
            if (!disableTrace)
            {
                logger.Trace($"ExecuteCommand: [{commandFullLine}] with arguments [{arguments}]");
            }

            try
            {
                string resultString = ExecuteCommand(commandFullLine, arguments);
                return ParseOutput<T>(resultString);
            }
            catch(Exception ex)
            {
                logger.Error("Got exception in ExecuteCommandAndThrow: " + ex.ToString());
                throw;
            }
        }

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
            catch (TargetMachineException ex)
            {
                return ExecutionResult<T>.ErrorResult(ex);
            }
            catch (Exception ex)
            {
                return ExecutionResult<T>.ErrorResult(TargetMachineErrorCode.EXECUTOR_GENERIC_ERROR, ex.Message);
            }
        }

        public static T ParseOutput<T>(string rawOutput)
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

            if (code == 0)
            {
                try
                {
                    T data = JsonConvert.DeserializeObject<T>(resultStrings[1]);
                    return data;
                }
                catch (FormatException ex)
                {
                    throw new Exception("The output for command is not a valid Json format.", ex);
                }
            }
            else
            {
                TargetMachineErrorCode errorCode;
                try
                {
                    errorCode = (TargetMachineErrorCode)code;
                }
                catch(Exception)
                {
                    errorCode = TargetMachineErrorCode.UNKNOWN_ERROR;
                }

                throw new TargetMachineException(errorCode, resultStrings[1]);
            }
        }
    }

    public class ExecutionResult<T>
    {
        


        public static ExecutionResult<T> ErrorResult(TargetMachineException ex)
        {
            return ErrorResult(ex.ErrorCode.GetHashCode(), ex.Message);
        }

        public static ExecutionResult<T> ErrorResult(TargetMachineErrorCode code, string message)
        {
            return ErrorResult(code.GetHashCode(), message);
        }

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

    public class ConfigureOutput
    {
        public ConfigureOutput()
        {
        }

        public int? InstanceId
        {
            get; set;
        }
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

        public string Details
        {
            get; set;
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
