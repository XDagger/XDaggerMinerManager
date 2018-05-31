using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.ObjectModel
{
    public class MinerManager
    {
        private static MinerManager instance = null;

        public static MinerManager GetInstance()
        {
            if (instance == null)
            {
                instance = new MinerManager();
            }

            return instance;
        }

        private MinerManager()
        {
            ClientList = new List<MinerClient>();
        }

        public List<MinerClient> ClientList
        {
            get;private set;
        }


    }
}
