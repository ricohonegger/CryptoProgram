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
        public IProtocol protocol { get; set; } //protocol being used in comms
        public string identifier { get; set;}
        public int id { get; set; }

        public ActiveConnection()
        {
            
        }

        public ActiveConnection(Socket socket)
        {
            this.socket = socket;
            this.identifier = "";
            this.id = -1;
            //this.useProtocol = false;
            this.protocol = new CryptoProgram.SecProtocol.CustomProtocol1();//new SecProtocol();
        }

        public void initialiseProtocol(bool isInitiator, List<Object> objects)
        {
            //Protocol string will be hardcoded for the time being.
            //ProtocolParser a = new ProtocolParser();
            //a.input = "IA,{Na,{H(Na)}Ka-1}Kb:IB,H(IB):IC:ID,{Na,Nb,{Na,Nb}Ka-1}Kb:IF";
            //a.init();

            //Use results from ProtocolParser
            //this.protocol.protocol = a.listOfTrees;
            //this.protocol.protocolForDataExchange = a.dataExchangeRule;
            //this.protocol.stagesInProtocol = a.getNumberOfStagesInProtocol();

            //this.protocol.isInitiator = isInitiator;

            this.protocol.initialiseProtocol<Object>(isInitiator, objects);

            //add event so SecProtocol -> ActiveConn
            this.protocol.readyToSendNextMessageOfProtocol += this.SendNextMessage_Event;
        }

        public void displaySettings()
        {
            //information to send to tab page that shows active connection settings and details
        }

        public EndPoint getLocal()
        {
            return this.socket.LocalEndPoint;
        }

        public EndPoint getRemote()
        {
            return this.socket.RemoteEndPoint;
        }

        /*
         * Prepare protocol for start
         */ 
        public string prepareProtocol()
        {
            return this.protocol.prepareProtocol();
        }

        /*
         * Encode a message using protocol
         */ 
        public string encodeMessage(string message)
        {
            return this.protocol.encodeMessage(message);
        }

        /*
         * Decode the message using protocol.
         */ 
        public List<string> decodeMessage(string message)
        {
            return this.protocol.decodeMessage(message);
        }       

        /*
         * SecProtocol tells us when to send the next message of the protocol.
         */ 
        public void SendNextMessage_Event()
        {
            //Fire event to say we need to send a message
            OnSendNextMessageOfProtocol();
        }

        public delegate void SendNextMessageOfProtocolEventHandler(string port);

        public event SendNextMessageOfProtocolEventHandler sendNextMessageOfProtocol;

        public void OnSendNextMessageOfProtocol()
        {
            if (sendNextMessageOfProtocol != null)
            {
                sendNextMessageOfProtocol("" + (this.socket.LocalEndPoint as IPEndPoint).Port);
            }
        }

    }
}
