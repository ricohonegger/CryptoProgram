using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto;
using System.Windows.Forms;
using System.Net.Sockets;

namespace CryptoProgram
{
    public class GlobalContainer
    {
        //Holds all global members required by GUI.


        public AsymmetricCipherKeyPair myKeys { get; set; }
        //public AsymmetricCipherKeyPair destKey { get; set; }

        public SecProtocol sP { get; set; }
        public SecProtocol sP2 { get; set; }

        //Contains the currently active connections (Sockets).
        public List<ActiveConnection> activeConnections { get; set; }

        public readonly bool SELECT_PRIVATE_KEY = true;
        public readonly bool SELECT_PUBLIC_KEY = false;


        public GlobalContainer()
        {
            //add comment to constructor..
        }

    }
}
