using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace XDaggerMinerManager.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomWebClient : WebClient
    {
        private int RequestTimeoutInSecond = 0;

        public CustomWebClient(int timeout = 30)
        {
            this.RequestTimeoutInSecond = timeout;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = this.RequestTimeoutInSecond * 1000;
            return w;
        }
    }
}
