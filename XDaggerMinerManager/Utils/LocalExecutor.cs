using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

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
        public override List<string> Execute(string commandFullLine, string arguments = "")
        {
            string result = ExecuteCommandWithStreamOutput(commandFullLine, arguments,
                (reader) => { return (reader == null) ? null : reader.ReadToEnd(); });

            return new List<string>() { result };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandFullLine"></param>
        /// <param name="arguments"></param>
        /// <param name="streamHandler"></param>
        /// <returns></returns>
        public string ExecuteCommandWithStreamOutput(string commandFullLine, string arguments, Func<StreamReader, string> streamHandler)
        {
            if (!disableTrace)
            {
                logger.Trace("Start ExecuteCommandWithStreamOutput.");
            }

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

                return streamHandler(process.StandardOutput);
            }
            catch (Exception ex)
            {
                logger.Error("Got error while ExecuteCommandWithStreamOutput: " + ex.ToString());
                throw ex;
            }
            finally
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // Swallow exception
                    }
                }
            }
        }

        public override void TestConnection()
        {
            // Do nothing in the local executor
        }
    }
}
