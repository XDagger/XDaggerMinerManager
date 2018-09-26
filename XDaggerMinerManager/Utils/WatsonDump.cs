using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qiniu.Storage;
using Qiniu.Util;
using Qiniu.Http;
using System.Web;
using System.IO;
using System.Reflection;
using System.IO.Compression;

namespace XDaggerMinerManager.Utils
{
    public class WatsonDump
    {
        public WatsonDump()
        {
            this.Id = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        }

        public string Id
        {
            get; private set;
        }

        public string Description
        {
            get; set;
        }

        public string WatsonName
        {
            get
            {
                return string.Format("XDaggerMiner-Dump-{0}", this.Id);
            }
        }

        public void SendReport()
        {
            string currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string tempFolder = Path.GetTempPath();
            string tempWatsonFolder = Path.Combine(tempFolder, WatsonName);
            
            Mac mac = new Mac("Zj7BFGPSGlDdbB2FDjT8patg5R9DBs_C6kMv_bRe", "YSQCj_M0sIrhcnuvQd7lvr50roOdbXE30ZSgTAsd");
            PutPolicy putPolicy = new PutPolicy();
            putPolicy.Scope = "watsondump-xdaggerminer";
            string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());

            Config config = new Config();
            config.Zone = Zone.ZONE_CN_East;
            config.MaxRetryTimes = 3;

            try
            {
                if (!Directory.Exists(tempWatsonFolder))
                {
                    Directory.CreateDirectory(tempWatsonFolder);
                }
            }
            catch (Exception)
            {

            }

            CopyFileNoThrow(currentFolder, tempWatsonFolder, "manager-config.json");
            CopyFileNoThrow(currentFolder, tempWatsonFolder, "manager-info.json");
            CopyFileNoThrow(currentFolder, tempWatsonFolder, "Manager-Trace.log");

            if (!string.IsNullOrWhiteSpace(this.Description))
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(Path.Combine(tempWatsonFolder, "description.txt")))
                    {
                        sw.WriteLine(this.Description);
                    }
                }
                catch (Exception)
                {

                }
            }

            string dumpFileFullPath = Path.Combine(tempFolder, WatsonName + ".zip");

            try
            {
                ZipFile.CreateFromDirectory(tempWatsonFolder, dumpFileFullPath, CompressionLevel.Fastest, true);
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Error while packing dump file.", ex);
            }

            try
            {
                FormUploader target = new FormUploader(config);
                HttpResult result = target.UploadFile(dumpFileFullPath, WatsonName + ".zip", token, null);

                if (result.Code == 200)
                {
                    // Success
                }
                else
                {
                    throw new HttpException("UploadFile to Qiniu failed. Code=" + result.Code);
                }
            }
            catch(Exception ex)
            {
                throw new HttpException("UploadFile to Qiniu failed.", ex);
            }
        }

        private void CopyFileNoThrow(string sourceFolder, string destinationFolder, string fileName)
        {
            try
            {
                File.Copy(Path.Combine(sourceFolder, fileName), Path.Combine(destinationFolder, fileName));
            }
            catch(Exception)
            {

            }
        }
    }
}
