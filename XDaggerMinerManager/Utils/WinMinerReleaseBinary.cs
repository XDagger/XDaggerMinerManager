using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.IO.Compression;

namespace XDaggerMinerManager.Utils
{
    public class WinMinerReleaseBinary : ReleasedBinary
    {
        public WinMinerReleaseBinary(string version)
        {
            this.Version = version;
        }

        public string Version
        {
            get; private set;
        }

        public static string ProjectName
        {
            get
            {
                return "XDaggerMinerWin";
            }
        }

        public static string DaemonScriptFileName
        {
            get
            {
                return "DaemonScript.ps1";
            }
        }

        public static string DaemonExecutionFileName
        {
            get
            {
                return "XDaggerMinerDaemon.exe";
            }
        }

        public string PackageName
        {
            get
            {
                return string.Format("XDaggerMinerWin-{0}-x64.zip", this.Version);
            }
        }

        public string ReleaseDownloadUrl
        {
            get
            {
                return string.Format("https://github.com/Toneyisnow/XDaggerMinerWin/releases/download/{0}/", this.Version);
            }
        }

        public string TempDownloadFolder
        {
            get
            {
                return string.Format("{0}XDaggerMinerWinPackage-v{1}", Path.GetTempPath(), this.Version);
            }
        }

        /// <summary>
        /// Get all version list from the Release Website
        /// </summary>
        /// <returns></returns>
        public static List<string> GetVersions()
        {
            return null;
        }

        public static string GetLastestVersion()
        {
            return null;
        }

        public void DownloadPackage()
        {
            // Setup the download properties to support HTTPS protocol
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Uri uri = new Uri(this.ReleaseDownloadUrl + this.PackageName);
            string fullPath = Path.Combine(TempDownloadFolder, this.PackageName);

            if (!Directory.Exists(TempDownloadFolder))
            {
                Directory.CreateDirectory(TempDownloadFolder);
            }

            if (File.Exists(fullPath))
            {
                // Binary Already exists, skip the downloading
                // AsyncCompletedEventArgs args = new AsyncCompletedEventArgs(null, false, null);
                /// eventHander(this, args);
                return;
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(uri, fullPath);
                    ///client.DownloadFileCompleted += eventHander;
                    ///client.DownloadFileAsync(uri, fullPath);
                }
            }
            catch(WebException webExcetion)
            {
                throw webExcetion;
                // TODO: Add handler
            }
            catch (InvalidOperationException invalidException)
            {
                // TODO: Add handler
                throw invalidException;
            }
        }

        public void ExtractPackage()
        {
            if (Directory.Exists(Path.Combine(TempDownloadFolder, WinMinerReleaseBinary.ProjectName)))
            {
                // Already Exists, ignore
                return;
            }

            string fullPath = Path.Combine(TempDownloadFolder, this.PackageName);
            ZipFile.ExtractToDirectory(fullPath, TempDownloadFolder);
        }
        
        public void CopyBinaryToTargetPath(string targetRemoteBinaryPath)
        {
            string sourceBinaryPath = Path.Combine(TempDownloadFolder, WinMinerReleaseBinary.ProjectName);
            if (!Directory.Exists(sourceBinaryPath))
            {
                // No binaries, ignore
                return;
            }

            if (!Directory.Exists(targetRemoteBinaryPath))
            {
                Directory.CreateDirectory(targetRemoteBinaryPath);
            }

            try
            {
                string[] fileEntries = Directory.GetFiles(sourceBinaryPath);
                foreach (string sourceFullFileName in fileEntries)
                {
                    string singleFileName = sourceFullFileName.Substring(sourceFullFileName.LastIndexOf("\\") + 1);
                    File.Copy(sourceFullFileName, Path.Combine(targetRemoteBinaryPath, singleFileName));
                }
            }
            catch (Exception ex)
            {
                // If there is any exception during the copying, fail the whole process
                throw ex;
            }

        }
    }
}
