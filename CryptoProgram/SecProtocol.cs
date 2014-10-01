using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoProgram 
{
    public class SecProtocol
    {
        public String srcIp { get; set; }
        public String destIp { get; set; }
        public int srcPort { get; set; }
        public int destPort { get; set; }

        //private int defaultListenPort = 7540;
        //private int defaultSendPort = 7541;

        public SecProtocol()
        {
            this.srcIp = "127.0.0.1";
            this.srcPort = 7530;

            this.destIp = "127.0.0.1";
            this.destPort = 7531;
        }

        public SecProtocol(String srcIp, int srcPort, String destIp, int destPort)
        {
            this.srcIp = srcIp;
            this.srcPort = srcPort;

            this.destIp = destIp;
            this.destPort = destPort;
        }

        public void Encode(String message)
        {
            //encode a piece of data using a specified protocol...
        }

    }
}
