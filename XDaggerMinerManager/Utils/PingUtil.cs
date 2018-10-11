using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Utils
{
    public class PingUtil
    {
        public PingUtil()
        {

        }

        public static PingReply Send(string machineName)
        {
            Ping pingSender = new Ping();
            AutoResetEvent waiter = new AutoResetEvent(false);

            string data = "test";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 10000;

            // Set options for transmission:
            // The data can go through 64 gateways or routers
            // before it is destroyed, and the data packet
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            // Send the ping asynchronously.
            // Use the waiter as the user token.
            // When the callback completes, it can wake up this thread.
            return pingSender.Send(machineName, timeout, buffer, options);
        }

    }
}
