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
        public Form1()
        {
            InitializeComponent();

            this._GC = new GlobalContainer
            {
                //Initialise members here..
                activeConnections = new List<ActiveConnection>()
            };

            // Attach events
            SimpleSocket.updateUIUsingEvent += receivedMsg;
            SimpleSocket.connectionEstablishedEvent += connectionEstablished;
            SimpleSocket.connectionInterruptedEvent += connectionEstablished;
            SimpleSocket.handlerForConnectionEstablishedEvent += handlerReceived;
            SimpleSocket.invalidHeaderReceivedEvent += connectionEstablished;
        }

        public GlobalContainer _GC = null;

        //AES Encrypt/Decrypt
        private void button1_Click(object sender, EventArgs e)
        {

            this.textBox2.Text = BCEngine.AESEncryption(this.textBox1.Text, "0123456789ABCDEF0123456789ABCDEF", true);
            this.textBox3.Text = BCEngine.AESDecryption(this.textBox2.Text, "0123456789ABCDEF0123456789ABCDEF", true);
        }

        //Generate RSA Key Pair
        private void button2_Click(object sender, EventArgs e)
        {
            AsymmetricCipherKeyPair keys = null;
            
            //Generate 2 new keys
            BCEngine.generateKeys(false, true, ref keys, 1024);
            
            AsymmetricKeyParameter priv = keys.Private;
            AsymmetricKeyParameter priv2 = keys.Public;

            //or            

            //Load the keys from file
            //AsymmetricKeyParameter priv = BCEngine.getKeyFromFile(true);
            //AsymmetricKeyParameter priv2 = BCEngine.getKeyFromFile(false);

            //Add the keys to the richtextboxes
            this.richTextBox1.Text = BCEngine.getKeyFromKey(priv, true);
            this.richTextBox2.Text = BCEngine.getKeyFromKey(priv2, false);

            /*
            //Use the private key and hash it.
            this.richTextBox1.Text = BCEngine.getKeyFromKey(priv, true);
            String hashedKey = BCEngine.ComputeHash(this.richTextBox1.Text, "SHA256");

            //Use the hashed key as a key for AES encryption and decryption..
            this.textBox2.Text = BCEngine.AESEncryption(this.textBox1.Text, hashedKey, true);
            this.textBox3.Text = BCEngine.AESDecryption(this.textBox2.Text, hashedKey, true); 
            */
        }

        //Encrypt/Decrypt RSA
        private void button3_Click(object sender, EventArgs e)
        {
            //Loads keys from file            
            AsymmetricKeyParameter pub = BCEngine.getKeyFromFile(false);
            AsymmetricKeyParameter pri = BCEngine.getKeyFromFile(true);

            //Encrypt
            this.textBox5.Text = BCEngine.RSAEncryption(this.textBox4.Text, pub, true);  
         
            //Decrypt the encrypted part.
            this.textBox6.Text = BCEngine.RSADecryption(this.textBox5.Text, pri, true);
        }
        
        /*
         * Listen for incoming connection.
         */
        private void button4_Click(object sender, EventArgs e)
        {

            int portNumber = 0;

            //try to parse
            if (Int32.TryParse(this.textBox8.Text, out portNumber))
            {
                SimpleSocket abc = new SimpleSocket();
                Socket theSocketWhichAcceptsNewConnections = abc.listenSocket(new IPEndPoint(IPAddress.Parse("127.0.0.1"), portNumber));
            }
            else
            {
                //Invalid input or port number..
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

                SimpleSocket abc = new SimpleSocket();

                int idForActiveConnection = this._GC.activeConnections.Count;
                this._GC.activeConnections.Add(new ActiveConnection(abc.establishConnection("127.0.0.1", portNumber, "message to be sent encoded using protocol..")));
                this._GC.activeConnections[idForActiveConnection].id = idForActiveConnection;

                //Can only call this if establish was successful
                if (this._GC.activeConnections[0].socket != null)
                {
                    abc.ReceiveDataOnConnectedSocketCallback(this._GC.activeConnections[idForActiveConnection].socket);

                    this.listBox1.Items.Add("Socket " + this._GC.activeConnections[idForActiveConnection].socket.RemoteEndPoint.ToString());
                }
                else
                {
                    addMessageToRTBMainChat("Error: No connection could be established to: 127.0.0.1:" + portNumber + "\n");
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

        

        public void guiListen()
        {    
            //Generate my keys
            AsymmetricCipherKeyPair tempKeys = _GC.myKeys;
            BCEngine.generateKeys(false, true, ref tempKeys, 1024);
            _GC.myKeys = tempKeys;

            _GC.sP2 = new SecProtocol
            {
                srcIp = "127.0.0.1",
                srcPort = 1337,
                destIp = "127.0.0.1",
                destPort = 2051
            };
        }

        /*
         * Offer Keypair exchange
         */ 
        private void button11_Click(object sender, EventArgs e)
        {
            //Function:
            //Generate my public and private keys, and send my public key.
            //Wait for response; their public key

            AsymmetricCipherKeyPair tempKeys = _GC.myKeys;
            BCEngine.generateKeys(false, true, ref tempKeys, 1024);
            _GC.myKeys = tempKeys;

            _GC.sP = new SecProtocol
            {
                srcIp = "127.0.0.1",
                srcPort = 1337,
                destIp = "127.0.0.1",
                destPort = 2051
            };
         
            this.label10.Text = "Sending Key";            
            //send
            this.label10.Text = "Sent Key";
        }

        /*
         * Accept Keypair Exchange
         */
        private void button12_Click(object sender, EventArgs e)
        {
            AsymmetricCipherKeyPair tempKeys = _GC.myKeys;
            BCEngine.generateKeys(false, true, ref tempKeys, 1024);
            _GC.myKeys = tempKeys;

            _GC.sP2 = new SecProtocol
            {
                srcIp = "127.0.0.1",
                srcPort = 1337,
                destIp = "127.0.0.1",
                destPort = 2051
            };
            SecProtocol test1 = new SecProtocol("127.0.0.1", 1337, "127.0.0.1", 2051);
            
            //...
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
                    addMessageToRTBMainChat(string.Format("Time: {1}\tSource: {2}\tLocal: {3}\tMessage: {0}\n", message, DateTime.Now, source.ToString(), local.ToString()));
                });
            }
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
                });
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

                this._GC.activeConnections.Add(new ActiveConnection(handler));
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
            this.richTextBox3.Text += message;
        }

        
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

                    //this._GC.activeConnections.Count > 0
                    abc.sendMessage(this._GC.activeConnections[this.listBox1.SelectedIndex], this.textBox10.Text);
                }
            }
        }
        
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

        

        

    }
}
