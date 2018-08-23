using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    /// <summary>
    /// 
    /// </summary>
    public class MinerMachine
    {
        public static string TranslateHeaderName(string header)
        {
            switch(header)
            {
                case "FullMachineName": return "机器名称";
                case "IpAddressV4": return "地址";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FullMachineName
        {
            get;set;
        }

        public string IpAddressV4
        {
            get; set;
        }

    }
}
