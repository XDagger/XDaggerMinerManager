using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDaggerMinerManager.ObjectModel;

namespace XDaggerMinerManager.UI.Controls
{
    public class MachineDataGridItem
    {
        private MinerMachine minerMachine = null;

        public MachineDataGridItem(MinerMachine machine)
        {
            this.minerMachine = machine;
        }

        public static string TranslateHeaderName(string header)
        {
            switch (header)
            {
                case "IsSelected": return "选择";
                case "FullName": return "机器名称";
                case "IpAddressV4": return "地址";
                default: return string.Empty;
            }
        }

        public string FullName
        {
            get
            {
                return minerMachine.FullName;
            }
        }

        public string IpAddressV4
        {
            get
            {
                return minerMachine.IpAddressV4;
            }
        }

        public bool IsSelected
        {
            get; set;
        }

        public MinerMachine GetMachine()
        {
            return minerMachine;
        }

    }
}
