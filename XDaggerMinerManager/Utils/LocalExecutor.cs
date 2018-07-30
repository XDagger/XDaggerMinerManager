using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XDaggerMinerManager.Utils
{
    public class LocalExecutor : TargetMachineExecutor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandFullLine"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public override string ExecuteCommand(string commandFullLine, string arguments = "")
        {
            Process process = new System.Diagnostics.Process();
            ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;

            startInfo.FileName = commandFullLine;
            startInfo.Arguments = arguments;
            process.StartInfo = startInfo;

            try
            {
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                return output;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }

}
