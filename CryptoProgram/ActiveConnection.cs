using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace CryptoProgram
{
    public class ActiveConnection
    {
        public Socket socket { get; set; }
        //public Protocol protocol; //protocol being used in comms
        public String identifier { get; set;}
        public int id { get; set; }

        public ActiveConnection()
        {
            
        }

        public ActiveConnection(Socket socket)
        {
            this.socket = socket;
            this.identifier = "";
            this.id = -1;
        }

        public EndPoint getLocal()
        {
            return this.socket.LocalEndPoint;
        }

        public EndPoint getRemote()
        {
            return this.socket.RemoteEndPoint;
        }

    }
}
