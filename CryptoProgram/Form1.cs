using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RSAKeygen = Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto.Generators;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Web.Script.Serialization;
using System.Net;

namespace CryptoProgram
{
    public partial class Form1 : Form
    {
        //Container to be used to hold all properties.
        public GlobalContainer _GC = null;
        
        public Form1()
        {
            InitializeComponent();

            this._GC = new GlobalContainer
            {
                //Initialise members here..
                activeConnections = new List<ActiveConnection>(),
                ports = new List<String>()
            };

            // Attach events
            SimpleSocket.receivedMessageEvent += receivedMsg;
            SimpleSocket.connectionEstablishedEvent += connectionEstablished;
            SimpleSocket.connectionInterruptedEvent += connectionEstablished;
            SimpleSocket.handlerForConnectionEstablishedEvent += handlerReceived;
            SimpleSocket.invalidHeaderReceivedEvent += connectionEstablished;
            
        }


        /*****************************************/
        //                                       //
        // First tab functionality               //
        //                                       //
        /*****************************************/

        /*
         * AES Encrypt/Decrypt functions
         */ 
        private void button1_Click(object sender, EventArgs e)
        {

            this.textBox2.Text = BCEngine.AESEncryption(this.textBox1.Text, "0123456789ABCDEF0123456789ABCDEF");
            this.textBox3.Text = BCEngine.AESDecryption(this.textBox2.Text, "0123456789ABCDEF0123456789ABCDEF");
        }

        /*
         * Generate RSA Key pair
         */ 
        private void button2_Click(object sender, EventArgs e)
        {
            AsymmetricCipherKeyPair keys = null;
            
            //Generate 2 new keys
            BCEngine.generateKeys(false, true, ref keys, 1024);
            
            AsymmetricKeyParameter priv = keys.Private;
            AsymmetricKeyParameter publ = keys.Public;

            //or            

            //Load the keys from file
            //AsymmetricKeyParameter priv = BCEngine.getKeyFromFile(this._GC.SELECT_PRIVATE_KEY);
            //AsymmetricKeyParameter publ = BCEngine.getKeyFromFile(this._GC.SELECT_PUBLIC_KEY);

            //Add the keys to the richtextboxes
            this.richTextBox1.Text = BCEngine.getKeyFromKeyAsPEM(priv);
            this.richTextBox2.Text = BCEngine.getKeyFromKeyAsPEM(publ);

            /*
            //Use the private key and hash it.
            this.richTextBox1.Text = BCEngine.getKeyFromKey(priv, true);
            String hashedKey = BCEngine.ComputeHash(this.richTextBox1.Text, "SHA256");

            //Use the hashed key as a key for AES encryption and decryption..
            this.textBox2.Text = BCEngine.AESEncryption(this.textBox1.Text, hashedKey, true);
            this.textBox3.Text = BCEngine.AESDecryption(this.textBox2.Text, hashedKey, true); 
            */
        }

        /*
         * Encrypt/Decrypt using RSA, requires keys to have been generated in textboxes
         */ 
        private void button3_Click(object sender, EventArgs e)
        {
            //Loads keys from file               
            //AsymmetricKeyParameter pub = BCEngine.getKeyFromFile(false);
            //AsymmetricKeyParameter pri = BCEngine.getKeyFromFile(true);

            //Load keys from textboxes
            AsymmetricKeyParameter pub = BCEngine.convertKeyAsStringToKey(this.richTextBox2.Text, this._GC.SELECT_PUBLIC_KEY);//public key//2
            AsymmetricKeyParameter pri = BCEngine.convertKeyAsStringToKey(this.richTextBox1.Text, this._GC.SELECT_PRIVATE_KEY);//private key

            //Encrypt
            this.textBox5.Text = BCEngine.RSAEncryption(this.textBox4.Text, pub);  
         
            //Decrypt the encrypted part.
            this.textBox6.Text = BCEngine.RSADecryption(this.textBox5.Text, pri);
        }

        /*****************************************/
        //                                       //
        // Second tab functionality              //
        //                                       //
        /*****************************************/
        
        /*
         * Listen for incoming connection on specified port.
         */
        private void button4_Click(object sender, EventArgs e)
        {
            int portNumber = 0;

            //try to parse
            if (Int32.TryParse(this.textBox8.Text, out portNumber))
            {
                SimpleSocket abc = new SimpleSocket();
                      
                //TODO remove hardcoded localhost ip
                Socket theSocketWhichAcceptsNewConnections = abc.listenSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), portNumber));
            }
            else
            {
                addMessageToRTBMainChat("Error: Invalid port number selected.\n");
            }
            
        }

        /*
         * Attempt to establish a connection to the specified IP and Port number
         */
        private void button5_Click(object sender, EventArgs e)
        {
            int portNumber = 0;

            //try to parse
            if (Int32.TryParse(this.textBox9.Text, out portNumber))
            {
                IPAddress ipOut;
                if (!IPAddress.TryParse(this.textBox7.Text, out ipOut))
                {
                    //Notify user of invalid IP Address
                    addMessageToRTBMainChat("Error: Invalid IP Address selected.\n");
                    return;                    
                }
                
                SimpleSocket abc = new SimpleSocket();
                
                //This lock has to be out here for localhost.
                //Establish connection doesnt return until it is successfully connected, so we could run into a race condition
                //when adding things to _GC.
                lock (GC_lock)
                {
                    //Attempt to establish connection
                    ActiveConnection connection = new ActiveConnection(abc.establishConnection(ipOut.ToString(), portNumber, "THIS IS AN ESTABLISH CONNECTION ATTEMPT"));

                    //Can only continue if establish was successful                
                    if (connection.socket != null)
                    {
                        int idForActiveConnection = this._GC.activeConnections.Count;
                        
                        this._GC.activeConnections.Add(connection);
                        

                        //Add port to list for reference.
                        this._GC.ports.Add("" + (connection.socket.LocalEndPoint as IPEndPoint).Port);
                        this._GC.activeConnections[idForActiveConnection].id = idForActiveConnection;

                        //Allow data to be received..
                        abc.ReceiveDataOnConnectedSocketCallback(this._GC.activeConnections[idForActiveConnection].socket);

                        this._GC.activeConnections[idForActiveConnection].initialiseProtocol(true, new List<Object>());
                        
                        //add event so ActiveConn -> Form1.cs
                        this._GC.activeConnections[idForActiveConnection].sendNextMessageOfProtocol += sendNextMessageOfProtocol_Event;
                       
                        this.listBox1.Items.Add("Socket " + this._GC.activeConnections[idForActiveConnection].socket.RemoteEndPoint.ToString());

                        //Tell the protocol to begin..
                        beginProto("" + (this._GC.activeConnections[idForActiveConnection].socket.LocalEndPoint as IPEndPoint).Port);
                    }
                    else
                    {
                        addMessageToRTBMainChat("Error: No connection could be established to: " + ipOut.ToString() + ":" + portNumber + "\n");
                    }
                }

                


            }
            else
            {
                addMessageToRTBMainChat("Error: Invalid port number selected.\n");
            }

        }    
        

        /*
         * Used to make sure we can set the text of a richtextbox from a thread
         */
        void SetTextME(string text, RichTextBox rtb)
        {
            
            if (rtb.InvokeRequired)
            {
                rtb.BeginInvoke(new Action(delegate {
                    SetTextME(text, rtb);
                }));
                return;
            }

            rtb.Text += text;
        }     

        /*
         * Used to handle event from SimpleSocket
         */ 
        private void receivedMsg(string message, EndPoint source, EndPoint local)
        {
            //no need to check for richTextBox.InvokeRequired, since we are only ever calling this function from an external thread
            if (!string.IsNullOrEmpty(message))
            {  
                this.Invoke((MethodInvoker)delegate
                {                    
                    addMessageToRTBMainChat(string.Format("--Time: {1}\tSource: {2}\tLocal: {3}\tMessage: {0}\n", message, DateTime.Now, source.ToString(), local.ToString()));

                    //get decoded result(s)
                    List<string> decoded = this._GC.activeConnections[this._GC.ports.IndexOf("" + (local as IPEndPoint).Port)].decodeMessage(message);

                    if (decoded.Count == 1)
                    {
                        addMessageToRTBMainChat(string.Format("--Time: {1}\tSource: {2}\tLocal: {3}\tMessage: {0}\n", decoded[0], DateTime.Now, source.ToString(), local.ToString()));
                    }
                    
                    /*
                    if (!this._GC.activeConnections[this._GC.ports.IndexOf("" + (local as IPEndPoint).Port)].protocol.protocolComplete)
                    {
                        //if we need to send a reply we do..
                        SimpleSocket abc = new SimpleSocket();

                        String toSend = this._GC.activeConnections[this._GC.ports.IndexOf("" + (local as IPEndPoint).Port)].sendMessage(String.Empty);
                        Console.WriteLine("Sending next msg of protocol: " + toSend);
                        abc.sendMessage(this._GC.activeConnections[this._GC.ports.IndexOf("" + (local as IPEndPoint).Port)], toSend);
                        
                    }
                     */
                     
                });
            }
        }

        public void sendNextMessageOfProtocol_Event(String portNumber)
        {
            SimpleSocket abc = new SimpleSocket();
            String toSend = this._GC.activeConnections[this._GC.ports.IndexOf(portNumber)].encodeMessage(String.Empty);
            //Console.WriteLine("Sending next msg of protocol: " + toSend);
            abc.sendMessage(this._GC.activeConnections[this._GC.ports.IndexOf(portNumber)], toSend);
        }

        /*
         * Used to handle event from SimpleSocket
         */ 
        private void connectionEstablished(string message, EndPoint source, EndPoint local)
        {
            //no need to check for richTextBox6.InvokeRequired, since we are only ever calling this function from an external thread
            if (!string.IsNullOrEmpty(message))
            {
                this.Invoke((MethodInvoker)delegate
                {                    
                    addMessageToRTBMainChat(string.Format("Connection Established: {1}\tSource: {2}\tLocal: {3}\tMessage: {0}\n", message, DateTime.Now, source.ToString(), local.ToString()));
                    Console.WriteLine("Beginning proto I am: " + (local as IPEndPoint).Port);
                    beginProto("" + (local as IPEndPoint).Port);                                     
                });
            }
        }      

        public void beginProto(string port)
        {
            //Prepare protocol for commencement
            String toSend = this._GC.activeConnections[this._GC.ports.IndexOf(port)].prepareProtocol();
            
            if (toSend != String.Empty)
            {
                SimpleSocket abc = new SimpleSocket();

                Console.WriteLine("Sending now: " + toSend);    
                //this._GC.activeConnections.Count > 0
                abc.sendMessage(this._GC.activeConnections[this._GC.ports.IndexOf(port)], toSend);               
                
            }
        }

        //Ensure Mutually exclusive access to the global container: this._GC.
        private static object GC_lock = new object();

        /*
         * When we receive handler add it to activeConnections and listbox
         */ 
        private void handlerReceived(Socket handler)
        {

            lock(GC_lock)
            {

                ActiveConnection connection = new ActiveConnection(handler);
                this._GC.activeConnections.Add(connection);

                //Add port to list for reference.                
                this._GC.ports.Add("" + (handler.LocalEndPoint as IPEndPoint).Port);

                //initialiseProtocol
                this._GC.activeConnections[this._GC.activeConnections.Count - 1].initialiseProtocol(false, new List<Object>());
                
                //add event so ActiveConn -> Form1.cs
                this._GC.activeConnections[this._GC.activeConnections.Count - 1].sendNextMessageOfProtocol += sendNextMessageOfProtocol_Event;
                
                this.Invoke((MethodInvoker)delegate
                {
                    this.listBox1.Items.Add("Socket " + handler.RemoteEndPoint.ToString());
                });                
            }
        }

        /*
         * Add text to the main richTextbox (Tab 2)
         */ 
        private void addMessageToRTBMainChat(String message)
        {
            this.richTextBox3.Text += message + "\n*****\n";
        }        
       

        /*****************************************/
        //                                       //
        // Local Events (Button clicks etc.)     //
        //                                       //
        /*****************************************/

        private static object lockerForSend = new object();

        //Send message (connection already established)
        private void button20_Click(object sender, EventArgs e)
        {
            //require a lock if sends are called from different threads?
            //lock (lockerForSend)
            {
                if (this.listBox1.SelectedIndex != -1 && this.listBox1.SelectedItem != null)
                {

                    SimpleSocket abc = new SimpleSocket();

                    //Encode the data
                    string messageToSend = this._GC.activeConnections[this.listBox1.SelectedIndex].encodeMessage(this.textBox10.Text);
                    
                    //We are sending a message over the currently selected activeConnection
                    abc.sendMessage(this._GC.activeConnections[this.listBox1.SelectedIndex], messageToSend);
                }
            }
        }









        /*****************************************/
        //                                       //
        // Temporary Testing Functionality       //
        //                                       //
        /*****************************************/

        /*
         * Used to send multiple messages as fast as possible.
         */ 
        private void button16_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                this.button20.PerformClick();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SecProtocolTreeTEST test = new SecProtocolTreeTEST();
            //test.populateTree2();
            //test.decodeMessage();

            ProtocolParser a = new ProtocolParser();
            a.main();
        }   

        

    }
}
