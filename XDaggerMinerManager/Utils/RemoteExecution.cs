using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;


namespace XDaggerMinerManager.Utils
{
    public class RemoteExecutor : TargetMachineExecutor
    {

        public RemoteExecutor(string machineName)
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
        public override string ExecuteCommand(string commandFullLine, string arguments = "")
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

    }


}