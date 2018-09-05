using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    /// <summary>
    /// 
    /// </summary>
    public class MinerMachine
    {
        public MinerMachine()
        {
        }

        public static string TranslateHeaderName(string header)
        {
            switch(header)
            {
                case "FullMachineName": return "机器名称";
                case "IpAddressV4": return "地址";
                default: return string.Empty;
            }
        }

        public void SetLoginPassword(string password)
        {
            this.LoginPlainPassword = password;

            this.LoginPassword = new SecureString();
            foreach (char x in this.LoginPlainPassword)
            {
                this.LoginPassword.AppendChar(x);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "full_name")]
        public string FullMachineName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "user_name")]
        public string LoginUserName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "password")]
        public SecureString LoginPassword
        {
            get; set;
        }

        [JsonProperty(PropertyName = "plain_password")]
        public string LoginPlainPassword
        {
            get; set;
        }

        public string IpAddressV4
        {
            get; set;
        }

    }
}
