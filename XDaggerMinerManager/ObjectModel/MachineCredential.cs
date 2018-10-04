using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    public class MachineCredential
    {
        public MachineCredential()
        {

        }


        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get; set;
        }

        [JsonProperty(PropertyName = "user_name")]
        public string UserName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "password")]
        public SecureString Password
        {
            get; set;
        }

        [JsonProperty(PropertyName = "plain_password")]
        public string LoginPlainPassword
        {
            get; set;
        }
        
        public void SetLoginPassword(string password)
        {
            this.LoginPlainPassword = password;

            this.Password = new SecureString();
            foreach (char x in this.LoginPlainPassword)
            {
                this.Password.AppendChar(x);
            }
        }
    }
}
