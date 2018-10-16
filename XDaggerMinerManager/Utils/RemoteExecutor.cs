using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using System.Threading.Tasks;


namespace XDaggerMinerManager.Utils
{
    public class RemoteExecutor : TargetMachineExecutor
    {
        private Uri remoteComputerUri = null;

        public RemoteExecutor(string machineName)
        {
            this.MachineName = machineName;

            remoteComputerUri = new Uri(String.Format("http://{0}:5985/wsman", this.MachineName));
        }

        public string MachineName
        {
            get; private set;
        }

        public override List<string> Execute(string commandExec, string arguments = "")
        {
            PSCredential credentials = new PSCredential(credentialUsername, credentialPassword);

            var connection = new WSManConnectionInfo(remoteComputerUri, "", credentials);
            using (Runspace runspace = RunspaceFactory.CreateRunspace(connection))
            {
                try
                {
                    runspace.Open();

                    PowerShell psinstance = PowerShell.Create();
                    psinstance.Runspace = runspace;
                    psinstance.AddScript(commandExec + " " + arguments);

                    var results = psinstance.Invoke();

                    List<string> resultStringList = new List<string>();
                    foreach (var result in results)
                    {
                        resultStringList.Add(result.ToString());
                    }

                    return resultStringList;
                }
                catch (PSRemotingTransportException ex)
                {
                    if (ex.ErrorCode == 5)
                    {
                        // Access Denied, Credential Wrong
                        throw new TargetMachineException(TargetMachineErrorCode.LOGIN_ACCESS_DENIED, "远程机器登录失败，请检查用户名密码。");
                    }

                    if (ex.ErrorCode == -2144108526)
                    {
                        // WinRM Service cannot be connected
                        throw new TargetMachineException(TargetMachineErrorCode.WINRM_CONNECTION_FAILED, "远程机器连接失败，请检查目标机器上WinRM服务是否开启。");
                    }

                    throw ex;
                }
                catch (PSRemotingDataStructureException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    Type type = ex.GetType();
                    throw ex;
                }
                finally
                {
                    runspace.Close();
                }
            }
        }
        
        public override void SetCredential(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException("Username should not be empty.");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("Password should not be empty.");
            }

            credentialUsername = username;
            credentialPassword = new SecureString();

            foreach (char x in password)
            {
                credentialPassword.AppendChar(x);
            }
        }

        public override void TestConnection()
        {
            try
            {
                List<string> content = this.Execute("cmd.exe");

                if (!content.Any(s => s.Contains("Microsoft Windows")))
                {
                    string message = $"The TestConnection result validation failed on machine [{ this.MachineName }], it doesn't contain 'Microsoft Windows'. Content: { content }";
                    logger.Error(message);
                    throw new Exception(message);
                }
            }
            catch(Exception ex)
            {
                logger.Error($"TestConnection failed on machine [{ this.MachineName }] with UserName [ {this.credentialUsername} ]. Error: { ex.ToString() }");
                throw;
            }
        }
    }
}