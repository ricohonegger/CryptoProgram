using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace CryptoProgram
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public bool messageLengthReceived = false;
        public int messageLength = 0;
        //public int messageReceived = 0;
        public int headerLength = 0;        
    }
    
    public class SimpleSocket
    {

        public SimpleSocket()
        {

        }

        /*
         * Sets up a socket to listen for any incoming connections on the specified IPEndPoint
         * returns the socket that is created
         */
        public Socket listenSocket(IPEndPoint endPoint)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Bind(endPoint);
                socket.Listen(10);

                //no need for thread
                /*
                Thread t = new Thread(() => funcName());
                t.Start();
                */

                socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);

                return socket;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Issue trying to bind to socket or something: " + ex.ToString());
                return null;
            }
        }      

        private void AcceptCallback(IAsyncResult ar)
        {
            
            // Get the socket that handles the client request
            Socket listener = (Socket)ar.AsyncState; 
            Socket handler = listener.EndAccept(ar);

            // Create the state object
            StateObject state = new StateObject();
            state.workSocket = handler;

            //Send handler so it can be used to send/receive more data.
            handlerForConnectionEstablished(handler);  
                
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackWithLength), state);
            connectionEstablished("Connection is established.", state.workSocket.RemoteEndPoint, state.workSocket.LocalEndPoint);
                
            //Needs listener.BeginAccept(new AsyncCallback(AcceptCallback), listener) again to allow for multiple connections at once
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

        }

        /*
         * Receive data from connected socket asynchronously.
         * Not useful + not finished. Would require escaping mechanism to allow a delimiter to be used. e.g <EOF>
         */
        public static void ReceiveCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            //need a SocketException try catch in case connection is forced closed on the other end.
            try
            {
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    //content = state.sb.ToString();

                    content = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                    //Console.WriteLine("content: " + content);
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        //Console.WriteLine("Read {0} bytes from socket {1}. \n Data : {2}", state.sb.Length, handler.RemoteEndPoint.ToString(), state.sb.ToString());
                        Console.WriteLine("Read {0} bytes from socket {1}. \n", state.sb.Length, handler.RemoteEndPoint.ToString());
                        //inform GUI of received message.                    
                        updateUIOnReceivedMessage(state.sb.ToString(), handler.RemoteEndPoint, handler.LocalEndPoint);


                        //have to clear SB since we dont want previous messages output again.
                        state.sb.Clear();

                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {

                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
                else
                {
                    //Sender hasn't sent anything, or closed connection?                   
                    //handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (SocketException e)
            {
                connectionInterruptedEvent("Connection was forcibly closed on the other end.", handler.RemoteEndPoint, handler.LocalEndPoint);
                Console.WriteLine("Socket was forcibly closed.\n" + e.ToString());
            }
        }

        /*
         * Receive data from socket asynchronously using an n byte header (defined in protocol) to convey the message length.
         */
        public static void ReceiveCallbackWithLength(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;

            int bytesRead = 0;

            //need a catch in case connection is forced closed on the other end.
            try
            {
                bytesRead = state.workSocket.EndReceive(ar);
            }
            catch (SocketException e)
            {
                connectionInterruptedEvent("Connection was forcibly closed on the other end.", state.workSocket.RemoteEndPoint, state.workSocket.LocalEndPoint);
                Console.WriteLine("Socket was forcibly closed.\n" + e.ToString());

                //return?
            }

            if (bytesRead > 0)
            {
                int headerLength = 8;

                //Check to make sure we have at least enough bytes received to read header for the messageLength
                if (bytesRead + state.sb.Length >= headerLength)
                {                    
                    int index = 0;

                    //while we have not reached end of buffer find stuff.
                    while (index < bytesRead)
                    {
                        //Check to see if we have already received the message length
                        if (!state.messageLengthReceived)
                        {
                            //Extract message length from buffer
                            int valid = cb_receiveMessageLength(state, bytesRead, headerLength, index);
                            if (valid != -1)
                            {
                                index += valid;
                            }
                            else
                            {
                                //error.. should be handled within cb_receiveMessageLength already.
                                return;
                            }                            

                        }                        
                        else
                        {                            
                            //Extract message body from buffer
                            int valid = cb_receiveMessageBody(state, bytesRead, headerLength, index);
                            if (valid != -1)
                            {
                                index += valid;                                
                            }
                            else
                            {
                                //Should be impossible to reach.
                                //error.. should be handled within cb_receiveMessageBody already.                                
                                return;
                            } 
                        }
                    }                    
                }
                else
                {
                    //not enough received to handle header, receive some more..
                }

                //Continue receiving..
                state.workSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackWithLength), state);
                   
            }
            //bytes read <= 0
            else
            {
                //Sender hasn't sent anything, or closed connection or the current message has been fully received?
                //handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackWithLength), state);
            }
            
        } 

        /*
         * Looks at socket buffer at specified index to extract the header of a message containing the message size
         */
        private static int cb_receiveMessageLength(StateObject state, int bytesRead, int headerLength, int index)
        {
            String messageLength = Encoding.ASCII.GetString(state.buffer, index, headerLength);

            int len = 0;
            if (Int32.TryParse(messageLength, out len))
            {
                //Get length in number of bytes                
                state.messageLength = len / sizeof(Char);
                state.messageLengthReceived = true;

                //Console.WriteLine("Header received: " + state.messageLength);
            }
            else
            {
                //Fire event to say an invalid header was received.
                invalidHeaderReceived("Invalid header was received: " + Encoding.ASCII.GetString(state.buffer, 0, state.buffer.Length) + ".\n" +
                        "Expected a " + headerLength + " byte numerical string at the start of the message.", state.workSocket.RemoteEndPoint, state.workSocket.LocalEndPoint);
                
                Console.WriteLine("*** Error with header ***");
                Console.WriteLine("Buffer: " + Encoding.ASCII.GetString(state.buffer, 0, state.buffer.Length));
                Console.WriteLine("#########################");
                
                return -1;
            }           
            
            //We return saying we found the message header therefore we ate headerLength byte(s) of the buffer
            return headerLength;
        }

        /*
         * Looks at socket buffer at specified index to find bytes of data related to the message.
         * Expects cb_receiveMessageLength to have already determined the size of the message (state.messageLength).
         */
        private static int cb_receiveMessageBody(StateObject state, int bytesRead, int headerLength, int index)
        {
            //how much left in buffer?
            int remainingInBuffer = bytesRead - index;

            //if what we already have and what is in buffer is less than entire message size
            if (state.sb.Length + remainingInBuffer < state.messageLength)
            {
                //then add it all
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, index, bytesRead - index));

                //return saying we ate rest of buffer
                return bytesRead - index;
            }
            //if the rest of the buffer contains the last part of our message (with no remainder for another message)
            else if (state.sb.Length + remainingInBuffer == state.messageLength)
            {
                //then add it all and fire event
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, index, bytesRead - index));

                //Console.WriteLine("Whole message received " + state.sb.Length + " bytes/" + state.messageLength + " bytes");
                updateUIOnReceivedMessage(state.sb.ToString(), state.workSocket.RemoteEndPoint, state.workSocket.LocalEndPoint);

                //Clear the string builder and reset message length received.
                state.sb.Clear();
                state.messageLengthReceived = false;
                
                //return saying we ate rest of buffer
                return bytesRead - index;
            }
            //if the rest of buffer has our message but other message(s) also
            else if (state.sb.Length + remainingInBuffer > state.messageLength)
            {
                int amountEaten = (state.messageLength - state.sb.Length);
                //then add up until end of message. Fire event                
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, index, (state.messageLength - state.sb.Length)));
                //Console.WriteLine("Next: " + Encoding.ASCII.GetString(state.buffer, index + state.messageLength - state.sb.Length, state.buffer.Length - index));
               
                //Console.WriteLine("Whole message received " + state.sb.Length + " bytes/" + state.messageLength + " bytes");
                updateUIOnReceivedMessage(state.sb.ToString(), state.workSocket.RemoteEndPoint, state.workSocket.LocalEndPoint);

                //Clear the string builder and reset message length received.
                state.sb.Clear();
                state.messageLengthReceived = false;

                //return saying we ate some of buffer
                return amountEaten;
            }

            //should be impossible to reach considering the if statements above.
            return -1;
        }

        /*
         * Send the given message to the user at the other end of the given ActiveConnection.
         */
        public void sendMessage(ActiveConnection connection, String message)
        {
            if (!connection.socket.Connected)
            {
                //Fire event to say socket was not connected..
            }
            else
            {

                //Hard coded string will be replaced using some form of a messaging protocol.
                message = "My message.. ";

                for (int i = 0; i < 5; i++)
                {
                    //message += message;
                }
                message += "<EOF>";

                String msgSize = "" + (message.Length * sizeof(Char)).ToString("00000000");
                
                //Console.WriteLine("Sending message of length: " + message.Length + ", Char size: " + sizeof(Char));

                byte[] outData = System.Text.Encoding.ASCII.GetBytes(msgSize + message);
                connection.socket.Send(outData, 0, outData.Length, SocketFlags.None);
            }
        }
        
        /*
         * Establish a connection to the specified ipAddress and port.
         * Expects both ipAddress and port to have been sanity checked.
         */
        public Socket establishConnection(String ipAddress, int port, String message)
        {
            return establishConnection(new IPEndPoint(IPAddress.Parse(ipAddress), port), message);
        }

        /*
         * Establishes a connection to the specified IPEndPoint
         */
        private Socket establishConnection(IPEndPoint destination, String message)
        {

            Socket connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Add it to list of connections
            //this.outboundConnections.Add(connection);//old

            try
            {
                Console.WriteLine("Attempting to connect to: " + destination.Address + ":" + destination.Port);
                connection.Connect(destination);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket err happend: " + e.ToString());
                //this.outboundConnections.Remove(connection);//old

                //Fire event to display failed connection
                return null;
            }

            
            message = "Hi, I want to establish a connection. ";

            for (int i = 0; i < 10; i++)
            {
                //message += message;
            }
            //message += "<EOF>";

            String msgSize = "" + (message.Length * sizeof(Char)).ToString("00000000");
            Console.WriteLine("MessageLength: " + message.Length + ", Char length: " + sizeof(Char));

            byte[] outData = System.Text.Encoding.ASCII.GetBytes(msgSize + message);
            connection.Send(outData, 0, outData.Length, SocketFlags.None);
            Console.WriteLine("Connection should be established");

            return connection;
        }       

        /*
         *  When a connection is already established, use this to receive data asynchronously.
         */
        public void ReceiveDataOnConnectedSocketCallback(Socket socket)
        {
            StateObject state = new StateObject();
            state.workSocket = socket;

            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackWithLength), state);
        }

        /*
         * Somehow close a socket to disable send receive...
         */
        public void Stop(ActiveConnection connection)
        {
            connection.socket.Shutdown(SocketShutdown.Both);
            connection.socket.Close();//Close() also calls Dispose()
        }


        //Need delegate and stuff to inform GUI of any completed operation..
        public delegate void SendMessage(string message, EndPoint source, EndPoint local);

        public static event SendMessage updateUIUsingEvent;

        //Delegate method
        public static void updateUIOnReceivedMessage(string message, EndPoint source, EndPoint local)
        {
            if (updateUIUsingEvent != null)
            {
                updateUIUsingEvent(message, source, local);
            }
        }

        //Established connection event
        public static event SendMessage connectionEstablishedEvent;

        public static void connectionEstablished(string message, EndPoint source, EndPoint local)
        {
            if (connectionEstablishedEvent != null)
            {
                connectionEstablishedEvent(message, source, local);
            }
        }

        //Abruptly disconnected connection
        public static event SendMessage connectionInterruptedEvent;
        
        public static void connectionInterrupted(string message, EndPoint source, EndPoint local)
        {
            if (connectionInterruptedEvent != null)
            {
                connectionInterruptedEvent(message, source, local);
            }
        }

        //Send the handler when a connection is received.
        public delegate void sendHandler(Socket handler);

        public static event sendHandler handlerForConnectionEstablishedEvent;

        public static void handlerForConnectionEstablished(Socket handler)
        {
            if (handlerForConnectionEstablishedEvent != null)
            {
                handlerForConnectionEstablishedEvent(handler);
            }
        }

        //Invalid header received
        public static event SendMessage invalidHeaderReceivedEvent;

        public static void invalidHeaderReceived(string message, EndPoint source, EndPoint local)
        {
            if (invalidHeaderReceivedEvent != null)
            {
                invalidHeaderReceivedEvent(message, source, local);
            }
        }

            

    }
}
