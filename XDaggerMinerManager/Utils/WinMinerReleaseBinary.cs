using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.IO.Compression;
using Newtonsoft.Json;

namespace XDaggerMinerManager.Utils
{
    public class WinMinerReleaseBinary : ReleasedBinary
    {
        public WinMinerReleaseBinary(string version)
        {
            // Setup the download properties to support HTTPS protocol
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

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

        public static string PackagerVersionFileName
        {
            get
            {
                return "xdaggerminer-version.json";
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
                return string.Format(ManagerConfig.Current.MinerPackageName, this.Version);
            }
        }

        public static string DownloadBaseUrl
        {
            get
            {
                return ManagerConfig.Current.MinerDownloadUrlPath;
            }
        }

        public string DownloadUrl
        {
            get
            {
                return string.Format(ManagerConfig.Current.MinerDownloadUrlPath, this.Version);
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
        public static WinMinerReleaseVersions GetVersionInfo()
        {
            //Download the version file
            Uri uri = new Uri(DownloadBaseUrl + PackagerVersionFileName);

            Random random = new Random();
            string targetFullPath = Path.Combine(Path.GetTempPath(), PackagerVersionFileName + "." + random.Next().ToString());
            
            if (File.Exists(targetFullPath))
            {
                // Delete the conflict one if exists
                try
                {
                    File.Delete(targetFullPath);
                }
                catch (IOException)
                {
                }
            }

            bool downloadSuceeded = false;
            try
            {
                using (var client = new CustomWebClient(5))
                {
                    client.DownloadFile(uri, targetFullPath);
                }

                downloadSuceeded = true;
            }
            catch (WebException webExcetion)
            {
            }
            catch (InvalidOperationException invalidException)
            {
            }
            catch (TimeoutException)
            {

            }

            // If the file downloaded failed, just use the local one instead
            if (!downloadSuceeded)
            {
                var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var directoryPath = Path.GetDirectoryName(location);

                targetFullPath = Path.Combine(directoryPath, PackagerVersionFileName);
            }

            try
            {
                using (StreamReader sr = new StreamReader(targetFullPath))
                {
                    string jsonString = sr.ReadToEnd();
                    WinMinerReleaseVersions info = JsonConvert.DeserializeObject<WinMinerReleaseVersions>(jsonString);
                    info.Validate();

                    return info;
                }
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                // Delete the temp file
                try
                {
                    File.Delete(targetFullPath);
                }
                catch (IOException)
                {
                }
            }
        }

        public void DownloadPackage()
        {
            Uri uri = new Uri(this.DownloadUrl + this.PackageName);
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

                string[] directoryEntries = Directory.GetDirectories(sourceBinaryPath);
                foreach(string dir in directoryEntries)
                {
                    string[] files = Directory.GetFiles(dir);
                    string singleDir = dir.Substring(dir.LastIndexOf("\\") + 1);

                    Directory.CreateDirectory(Path.Combine(targetRemoteBinaryPath, singleDir));
                    foreach (string file in files)
                    {
                        string singleFileName = file.Substring(file.LastIndexOf("\\") + 1);
                        File.Copy(file, Path.Combine(targetRemoteBinaryPath, singleDir, singleFileName));
                    }
                }
            }
            catch (Exception ex)
            {
                // If there is any exception during the copying, fail the whole process
                throw ex;
            }

        }
    }

    /// <summary>
    /// This is the format for the XDaggerMinerWin.ver file in JSON.
    /// </summary>
    public class WinMinerReleaseVersions
    {
        [JsonProperty(PropertyName = "latest")]
        public string Latest
        {
            get;set;
        }

        [JsonProperty(PropertyName = "available_versions")]
        public List<string> AvailableVersions
        {
            get; set;
        }

        public void Validate()
        {
            if (!AvailableVersions.Contains(Latest))
            {
                throw new FormatException("The Latest version is not contained in Available version list.");
            }
        }
    }
}
