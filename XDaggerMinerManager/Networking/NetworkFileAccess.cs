using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.Networking
{
    public class NetworkFileAccess
    {
        /// <summary>
        /// 
        /// </summary>
        private string userName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private string password = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private string machineName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public NetworkFileAccess(string machineName, string userName = "", string password = "")
        {
            this.machineName = machineName;
            this.userName = userName;
            this.password = password;
        }
        
        public static string ConvertToRemotePath(string machineName, string localPath)
        {
            return string.Format("\\\\{0}\\{1}", machineName, localPath.Replace(":", "$"));
        }

        public string ConvertToRemotePath(string localPath)
        {
            return string.Format("\\\\{0}\\{1}", this.machineName, localPath.Replace(":", "$"));
        }

        #region Public Methods
        
        public void EnsureDirectory(string localPath)
        {
            string remotePath = ConvertToRemotePath(localPath);

            AccessWithCredential<int>(() => {
                try
                {
                    if (!Directory.Exists(remotePath))
                    {
                        Directory.CreateDirectory(remotePath);
                    }
                    return 0;
                }
                catch (Exception)
                {
                    return -1;
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public bool CanAccessDirectory(string localPath)
        {
            string remotePath = ConvertToRemotePath(localPath);

            return AccessWithCredential<bool>(() => {
                try
                {
                    if (!Directory.Exists(remotePath))
                    {
                        return false;
                    }

                    File.Create(Path.Combine(remotePath, "touch.tst"), 10, FileOptions.DeleteOnClose);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public bool DirectoryExists(string localPath)
        {
            string remotePath = ConvertToRemotePath(localPath);

            return AccessWithCredential<bool>(() => {
                try
                {
                    return Directory.Exists(remotePath);
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// The sourceLocalDirectory must be on the local machine
        /// </summary>
        /// <param name="sourceLocalDirectory"></param>
        /// <param name="targetLocalDirectory"></param>
        public void DirectoryCopy(string sourceLocalDirectory, string targetLocalDirectory)
        {
            string remoteDirectory = ConvertToRemotePath(targetLocalDirectory);

            if (!CanAccessDirectory(targetLocalDirectory))
            {
                throw new InvalidOperationException($"Cannot access target directory [{targetLocalDirectory}].");
            }

            try
            {
                ConnectToShare(remoteDirectory);



            }
            finally
            {
                DisconnectFromShare(remoteDirectory, true);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Connects to the remote share
        /// </summary>
        /// <returns>Null if successful, otherwise error message.</returns>
        private string ConnectToShare(string remotePath)
        {
            //Create netresource and point it at the share
            NETRESOURCE nr = new NETRESOURCE();
            nr.dwType = RESOURCETYPE_DISK;
            nr.lpRemoteName = remotePath;

            //Create the share
            int ret = WNetUseConnection(IntPtr.Zero, nr, password, userName, 0, null, null, null);

            //Check for errors
            if (ret == NO_ERROR)
                return null;
            else
                return GetError(ret);
        }

        /// <summary>
        /// Remove the share from cache.
        /// </summary>
        /// <returns>Null if successful, otherwise error message.</returns>
        private string DisconnectFromShare(string remotePath, bool force)
        {
            //remove the share
            int ret = WNetCancelConnection(remotePath, force);

            //Check for errors
            if (ret == NO_ERROR)
                return null;
            else
                return GetError(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="accessFunction"></param>
        /// <returns></returns>
        private T AccessWithCredential<T>(Func<T> accessFunction)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return accessFunction();
            }
            else
            {
                using (var impersonation = new ImpersonatedUser(userName, machineName, password))
                {
                    return accessFunction();
                }
            }
        }

        #endregion

        #region P/Invoke Stuff
        [DllImport("Mpr.dll")]
        private static extern int WNetUseConnection(
            IntPtr hwndOwner,
            NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUserID,
            int dwFlags,
            string lpAccessName,
            string lpBufferSize,
            string lpResult
            );

        [DllImport("Mpr.dll")]
        private static extern int WNetCancelConnection(
            string lpName,
            bool fForce
            );

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope = 0;
            public int dwType = 0;
            public int dwDisplayType = 0;
            public int dwUsage = 0;
            public string lpLocalName = "";
            public string lpRemoteName = "";
            public string lpComment = "";
            public string lpProvider = "";
        }

        #region Consts
        const int RESOURCETYPE_DISK = 0x00000001;
        const int CONNECT_UPDATE_PROFILE = 0x00000001;
        #endregion

        #region Errors
        const int NO_ERROR = 0;

        const int ERROR_ACCESS_DENIED = 5;
        const int ERROR_ALREADY_ASSIGNED = 85;
        const int ERROR_BAD_DEVICE = 1200;
        const int ERROR_BAD_NET_NAME = 67;
        const int ERROR_BAD_PROVIDER = 1204;
        const int ERROR_CANCELLED = 1223;
        const int ERROR_EXTENDED_ERROR = 1208;
        const int ERROR_INVALID_ADDRESS = 487;
        const int ERROR_INVALID_PARAMETER = 87;
        const int ERROR_INVALID_PASSWORD = 1216;
        const int ERROR_MORE_DATA = 234;
        const int ERROR_NO_MORE_ITEMS = 259;
        const int ERROR_NO_NET_OR_BAD_PATH = 1203;
        const int ERROR_NO_NETWORK = 1222;
        const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;

        const int ERROR_BAD_PROFILE = 1206;
        const int ERROR_CANNOT_OPEN_PROFILE = 1205;
        const int ERROR_DEVICE_IN_USE = 2404;
        const int ERROR_NOT_CONNECTED = 2250;
        const int ERROR_OPEN_FILES = 2401;

        private struct ErrorClass
        {
            public int num;
            public string message;
            public ErrorClass(int num, string message)
            {
                this.num = num;
                this.message = message;
            }
        }

        private static ErrorClass[] ERROR_LIST = new ErrorClass[] {
        new ErrorClass(ERROR_ACCESS_DENIED, "Error: Access Denied"),
        new ErrorClass(ERROR_ALREADY_ASSIGNED, "Error: Already Assigned"),
        new ErrorClass(ERROR_BAD_DEVICE, "Error: Bad Device"),
        new ErrorClass(ERROR_BAD_NET_NAME, "Error: Bad Net Name"),
        new ErrorClass(ERROR_BAD_PROVIDER, "Error: Bad Provider"),
        new ErrorClass(ERROR_CANCELLED, "Error: Cancelled"),
        new ErrorClass(ERROR_EXTENDED_ERROR, "Error: Extended Error"),
        new ErrorClass(ERROR_INVALID_ADDRESS, "Error: Invalid Address"),
        new ErrorClass(ERROR_INVALID_PARAMETER, "Error: Invalid Parameter"),
        new ErrorClass(ERROR_INVALID_PASSWORD, "Error: Invalid Password"),
        new ErrorClass(ERROR_MORE_DATA, "Error: More Data"),
        new ErrorClass(ERROR_NO_MORE_ITEMS, "Error: No More Items"),
        new ErrorClass(ERROR_NO_NET_OR_BAD_PATH, "Error: No Net Or Bad Path"),
        new ErrorClass(ERROR_NO_NETWORK, "Error: No Network"),
        new ErrorClass(ERROR_BAD_PROFILE, "Error: Bad Profile"),
        new ErrorClass(ERROR_CANNOT_OPEN_PROFILE, "Error: Cannot Open Profile"),
        new ErrorClass(ERROR_DEVICE_IN_USE, "Error: Device In Use"),
        new ErrorClass(ERROR_EXTENDED_ERROR, "Error: Extended Error"),
        new ErrorClass(ERROR_NOT_CONNECTED, "Error: Not Connected"),
        new ErrorClass(ERROR_OPEN_FILES, "Error: Open Files"),
        new ErrorClass(ERROR_SESSION_CREDENTIAL_CONFLICT, "Error: Credential Conflict"),
    };

        private static string GetError(int errNum)
        {
            foreach (ErrorClass er in ERROR_LIST)
            {
                if (er.num == errNum) return er.message;
            }
            return "Error: Unknown, " + errNum;
        }
        
        #endregion

        #endregion
    }

    
}
