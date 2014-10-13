using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net;

namespace CryptoProgram 
{
    //required by IProtocol
    public delegate void ReadyToSendNextMessageOfProtocolEventHandler();
    
    public interface IProtocol
    {
        void initialiseProtocol<T>(bool isInitiator, List<Object> objects);
        string prepareProtocol();
        void beginProtocol();
        string encodeMessage(string message);
        List<string> decodeMessage(string message);

        event ReadyToSendNextMessageOfProtocolEventHandler readyToSendNextMessageOfProtocol;
    }    
    
    public class SecProtocol : IProtocol
    {

        //A list where each element represents one stage of the protocol.
        public List<SecProtocolTree<SecProtocolBlock>> protocol;

        //When data exchange commences, this is used to encode/decode data
        public SecProtocolTree<SecProtocolBlock> protocolForDataExchange;

        //If the protocol has reached the final stage it is considered finished, which is when data exchange can commence.
        public bool protocolComplete = false;

        //Current stage we are at in the protocol
        public int stageOfProtocol = 0;

        //Total number of stages in protocol
        public int stagesInProtocol = 0;//should be set to getNumberOfStagesInProtocol() from ProtocolParser

        //if you are the initiator of the protocol. E.g you called .beginProtocol();
        public bool isInitiator = false;        


        public SecProtocol()
        {
            //Define a new protocol that doesn't do anything.
            this.protocol = new List<SecProtocolTree<SecProtocolBlock>>();
            this.protocol.Add(new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData("")));
        }

        public SecProtocol(String protocolAsString)
        {
            this.protocol = new ProtocolParser().parseProtocol(protocolAsString);
        }

        public void initialiseProtocol<T>(bool isInitiator, List<Object> objects)
        {
            //Protocol string will be hardcoded for the time being.
            ProtocolParser a = new ProtocolParser();
            a.input = "IA,{Na,{H(Na)}Ka-1}Kb:IB,H(IB):IC:ID,{Na,Nb,{Na,Nb}Ka-1}Kb:IF";
            a.init();

            //Use results from ProtocolParser
            this.protocol = a.listOfTrees;
            this.protocolForDataExchange = a.dataExchangeRule;
            this.stagesInProtocol = a.getNumberOfStagesInProtocol();

            this.isInitiator = isInitiator;

            //add event so SecProtocol -> ActiveConn
            //this.protocol.readyToSendNextMessageOfProtocol += this.SendNextMessage_Event;
        }

        public string prepareProtocol()
        {
           
            if(this.isInitiator)
            {
                Console.WriteLine("Start protocol as initiator.");
                return encodeMessage(String.Empty);
            }
            else
            {
                Console.WriteLine("Start protocol as LISTEN.");
                //await stage zero to decode it.                
                return String.Empty;
            }
        }

        public void beginProtocol()
        {
            String toSend = prepareProtocol();

            if (toSend != String.Empty)
            {

            }

        }

        /*
         * Decode data.
         */
        public List<string> decodeMessage(string message)
        {
            SecProtocolTreeFunctions.decodeResult = new List<string>();
            if (this.protocolComplete)
            {
                //main protocol complete, decode data using data exchange protocol
                
                SecProtocolTreeFunctions.decodeMessageUsingTree(message, this.protocolForDataExchange);
            }
            else
            {
                //SecProtocolTreeFunctions.decodeResult = new List<string>();
                SecProtocolTreeFunctions.decodeMessageUsingTree(message, this.protocol.ElementAt(this.stageOfProtocol));
                
                this.stageOfProtocol++;

                //Console.WriteLine("Stage: " + this.stageOfProtocol + " : Total:" + this.stagesInProtocol);

                if (this.stageOfProtocol < this.stagesInProtocol)
                {
                    //before we are ready to send next part of protocol we will want to verify/check data.
                    //e.g compare hashes or create session key from nonces.

                    //fire an event saying we need to send next message in protocol                    
                    readyToSendNextMessageOfProtocol();
                }
                else
                {
                    //Protocol Done                    
                    this.protocolComplete = true;
                    Console.WriteLine("PROTOCOL COMPLETE DECODE");
                }
            }

            return SecProtocolTreeFunctions.decodeResult;
            /*
            Console.WriteLine("Decode results: ");
            for (int i = 0; i < SecProtocolTreeFunctions.decodeResult.Count; i++)
            {
                Console.WriteLine(SecProtocolTreeFunctions.decodeResult[i]);
            }
            Console.WriteLine("****");
            */
        }        
        
        /*
         * Encode data so it is ready to send
         */ 
        public string encodeMessage(string message)
        {
            if (!this.protocolComplete)
            {
                if (this.stageOfProtocol >= this.stagesInProtocol)
                {
                    Console.WriteLine("Protocol is complete ENCODE1");
                    this.protocolComplete = true;
                    protocolIsComplete();
                    return "";
                }

                String encoded = SecProtocolTreeFunctions.encodeMessageUsingProtocol(this.protocol.ElementAt(this.stageOfProtocol));

                this.stageOfProtocol++;
                if (this.stageOfProtocol >= this.stagesInProtocol)
                {
                    Console.WriteLine("Protocol is complete ENCODE2");
                    this.protocolComplete = true;
                }

                return encoded;
            }
            else
            {
                //main protocol complete, send data using data exchange protocol

                //null ref here
                //slightly cheating..
                if (this.protocolForDataExchange.children.Count != 0)
                {
                    if (this.protocolForDataExchange.getChild(0).children.Count == 0)
                    {
                        this.protocolForDataExchange.getChild(0).block.data = message;
                    }
                    else
                    {
                        this.protocolForDataExchange.getChild(0).getChild(0).block.data = message;
                    }
                }
                else
                {
                    return message;
                }

                return SecProtocolTreeFunctions.encodeMessageUsingProtocol(this.protocolForDataExchange);
            }
        }

        //fire event saying protocol is complete.
        public delegate void ProtocolIsCompleteEventHandler();

        public static event ProtocolIsCompleteEventHandler protocolIsCompleteEvent;

        public static void protocolIsComplete()
        {
            if (protocolIsCompleteEvent != null)
            {
                protocolIsCompleteEvent();
            }
        }

        //Fire event when we are ready to send next message of protocol
        //public delegate void ReadyToSendNextMessageOfProtocolEventHandler();

        public event ReadyToSendNextMessageOfProtocolEventHandler readyToSendNextMessageOfProtocol;

        public void OnReadyToSendNextMessageOfProtocol()
        {
            if (readyToSendNextMessageOfProtocol != null)
            {
                readyToSendNextMessageOfProtocol();
            }
        }

        /*
        Protocol:

			1: A->B: A,{Na,{H(Na)}Ka-1}Kb
			2: B   : Decrypt 1. Generate Session Key: H(Na||Nb)
			3: B->A: B,{Nb,{H(Nb)}Kb-1}Ka,{Na}Kab
			4: A   : Decrypt 3. Generate Session Key: H(Na||Nb)
			5: A->B: {Nb}Kab
			6: B   : Verify 5.


			7+ Encrypt data using session key.
         */

        public class CustomProtocol1 : IProtocol
        {

            //If the protocol has reached the final stage it is considered finished, which is when data exchange can commence.
            public bool protocolComplete = false;

            //Current stage we are at in the protocol
            public int stageOfProtocol = 0;

            //Total number of stages in protocol
            public int stagesInProtocol = 3;//should be set to getNumberOfStagesInProtocol() from ProtocolParser

            //if you are the initiator of the protocol. E.g you called .beginProtocol();
            public bool isInitiator = false;

            public string IdentityA;
            public string IdentityB;
            public string NonceA;
            public string NonceB;
            public string Hash_NonceA;
            public string Hash_NonceB;
            public string sessionKey;
            public string PublicKeyA;
            public string PublicKeyB;
            public string PrivateKeyA;
            public string PrivateKeyB;

            
            public void initialiseProtocol<T>(bool isInitiator, List<Object> objects)
            {
                

                this.isInitiator = isInitiator;

                if (this.isInitiator)
                {
                    objects.Add("IamA");
                    objects.Add("12345678910");
                    objects.Add(CustomProtocol1.testPublicKeyA);
                    objects.Add(CustomProtocol1.testPrivateKeyA);
                    objects.Add(CustomProtocol1.testPublicKeyB);

                    this.IdentityA = objects[0] as String;// "IamA";
                    this.NonceA = objects[1] as String;// "12345678910";

                    this.PublicKeyA = objects[2] as String;// CustomProtocol1.testPublicKeyA;

                    this.PrivateKeyA = objects[3] as String;// CustomProtocol1.testPrivateKeyA;

                    this.PublicKeyB = objects[4] as String;// CustomProtocol1.testPublicKeyB;


                }
                else
                {
                    objects.Add("IamB");
                    objects.Add("67891012345");
                    objects.Add(CustomProtocol1.testPublicKeyB);
                    objects.Add(CustomProtocol1.testPrivateKeyB);
                    objects.Add(CustomProtocol1.testPublicKeyA);
                    
                    this.IdentityB = "IamB";
                    this.NonceB = "67891012345";

                    this.PublicKeyB = CustomProtocol1.testPublicKeyB;

                    this.PrivateKeyB = CustomProtocol1.testPrivateKeyB;


                    this.PublicKeyA = CustomProtocol1.testPublicKeyA;

                }
            }

            public string prepareProtocol()
            {
                if (this.isInitiator)
                {
                    
                    
                    return encodeMessage(String.Empty);
                }
                else
                {
                    return String.Empty;
                }
            }

            public void beginProtocol()
            {
                String toSend = prepareProtocol();
            }

            public string encodeMessage(string message)
            {
                if (!this.protocolComplete)
                {                    

                    String encoded = encode();

                    this.stageOfProtocol++;
                    if (this.stageOfProtocol >= this.stagesInProtocol)
                    {
                        Console.WriteLine("Protocol is complete ENCODE2");
                        this.protocolComplete = true;
                    }

                    return encoded;
                }
                else
                {
                    //main protocol complete, send data using data exchange protocol                    

                    return message;
                }
            }

            public List<string> decodeMessage(string message)
            {
                if (!this.protocolComplete)
                {
                    decode(message);
                    this.stageOfProtocol++;

                    //Console.WriteLine("Stage: " + this.stageOfProtocol + " : Total:" + this.stagesInProtocol);

                    if (this.stageOfProtocol < this.stagesInProtocol)
                    {
                        //before we are ready to send next part of protocol we will want to verify/check data.
                        //e.g compare hashes or create session key from nonces.

                        //fire an event saying we need to send next message in protocol                    
                        readyToSendNextMessageOfProtocol();
                        Console.WriteLine("sending next msg of proto");
                    }
                    else
                    {
                        //Protocol Done                    
                        this.protocolComplete = true;
                        Console.WriteLine("PROTOCOL COMPLETE DECODE");
                    }

                }
                else
                {
                    
                    
                    
                    //Console.WriteLine("decode this:\n" + message);
                    
                }
                
                return new List<string>();
            }

            public event ReadyToSendNextMessageOfProtocolEventHandler readyToSendNextMessageOfProtocol;

            public void OnReadyToSendNextMessageOfProtocol()
            {
                if (readyToSendNextMessageOfProtocol != null)
                {
                    readyToSendNextMessageOfProtocol();
                }
            }

            private string encode()
            {
                switch (this.stageOfProtocol)
                {
                    case 0:
                        return encodeMessage1();                        

                    case 1:
                        return encodeMessage2();
                    case 2:
                        return encodeMessage3();
                    default:
                        return String.Empty;
                }
            }

            private string decode(string message)
            {
                switch (this.stageOfProtocol)
                {
                    case 0:
                        return decodeMessage1(message);
                    case 1:
                        return decodeMessage2(message);
                    case 2:
                        return decodeMessage3(message);
                    default:
                        return String.Empty;
                }
            }

            private string encodeMessage1()
            {
                //Encode: A,{Na,{H(Na)}Ka-1}Kb
                
                StringBuilder str = new StringBuilder();
                
                str.Append(this.IdentityA);
                //str.Append("," + this.NonceA);
                
                string hash = BCEngine.ComputeHash(this.NonceA, "SHA256");

                //Na,{H(Na)}Ka-1

                string toEncrypt = this.NonceA + ",";
                string temp1 = BCEngine.RSAEncryption(hash, this.PrivateKeyA, true);

                toEncrypt = toEncrypt + temp1;

                //string decrypt = BCEngine.RSADecryption(toEncrypt, this.PublicKeyA, false);
                //Console.WriteLine("le decrypt: " + decrypt);

                //Split it in half because it is too big to be encrypted all at once.
                //Console.WriteLine("whole: " + toEncrypt);
                //Console.WriteLine("pt1: " + toEncrypt.Substring(0, (toEncrypt.Length / 2) ));
                //Console.WriteLine("pt2: " + toEncrypt.Substring((toEncrypt.Length / 2), (toEncrypt.Length/2)- 1));
                string firstHalf = BCEngine.RSAEncryption(toEncrypt.Substring(0,(toEncrypt.Length/2)), this.PublicKeyB, false);
                string secondHalf = BCEngine.RSAEncryption(toEncrypt.Substring((toEncrypt.Length / 2), (toEncrypt.Length/2)-1), this.PublicKeyB, false);

                //StringBuilder combine = new StringBuilder();
                //combine.Append(BCEngine.RSADecryption(firstHalf, testPrivateKeyB, true));
                //combine.Append(BCEngine.RSADecryption(secondHalf, testPrivateKeyB, true));
                //string[] split = combine.ToString().Split(',');
                //Console.WriteLine("cmon0");

                //Console.WriteLine("temp1: " + temp1);
                //Console.WriteLine("split[1]: " + compensateForPadding(split[1]));

                //Thread.Sleep(150);
                //Thread.Sleep(150);

                //String tempHashNonceA = BCEngine.RSADecryption(compensateForPadding(split[1]), this.PublicKeyA, false);
                //Console.WriteLine("cmon");

                //string tempy = (this.NonceA + "," + encryptPt1);
                //Console.WriteLine("tempy: " + tempy);

                //Byte[] bytesToEncrypt = Encoding.ASCII.GetBytes(tempy);//System.Text.UnicodeEncoding.Unicode.GetBytes(tempy);
                //Console.WriteLine("tempy bytes: " + bytesToEncrypt.Length + ", len: " + tempy.Length );
                //string tempx = BCEngine.RSAEncryption(encryptPt1, this.PublicKeyB, false);
                str.Append("," +  firstHalf + "," + secondHalf);


                //String test = Convert.ToBase64String(Encoding.ASCII.GetBytes(str.ToString()));

                //Console.WriteLine("encrypted and good to go");
                //Console.WriteLine("Size: " + test.Length);
                return str.ToString();
            }

            public string decodeMessage2(string message)
            {
                //decode B,{Nb,{H(Nb)}Kb-1}Ka,{Na}Kab
                string[] split = message.Split(',');

                //A
                this.IdentityB = split[0];

                //Decode both halves
                StringBuilder combine = new StringBuilder();

                combine.Append(BCEngine.RSADecryption(split[1], this.PrivateKeyA, true));
                combine.Append(BCEngine.RSADecryption(split[2], this.PrivateKeyA, true));

                string encNa = split[3];

                Console.WriteLine("yiopee2");
                //Nb,{H(Nb)}Kb-1
                Console.WriteLine("combi2: \n" + combine.ToString());
                split = combine.ToString().Split(',');

                //Nb
                string tempNonceB = split[0];

                //input: {H(Nb)}Kb-1 -> output: H(Nb)
                string tempHashNonceB = BCEngine.RSADecryption(compensateForPadding(split[1]), this.PublicKeyB, false);

                //Create a hash from Nb above.
                string newHashNonceB = BCEngine.ComputeHash(tempNonceB, "SHA256");

                Console.WriteLine("Nonce A+B1: " + this.NonceA + "," + tempNonceB);
                Console.WriteLine("TempH nonce B: " + tempHashNonceB);
                Console.WriteLine("New hash nB: " + newHashNonceB);
                //Compare both hashes
                if (newHashNonceB.Equals(tempHashNonceB))
                {
                    //good
                    Console.WriteLine("hashes were equal.. pt2");
                    this.NonceB = tempNonceB;
                }
                else
                {
                    //bad
                    Console.WriteLine("hashes were not equal..pt2");
                }
                Console.WriteLine("Nonce A+B1.5: " + this.NonceA + "," + tempNonceB);
                this.sessionKey = BCEngine.ComputeHash(this.NonceA + tempNonceB, "SHA256");
                

                string NaFromDecrypt = BCEngine.AESDecryption(encNa, this.sessionKey);

                if (this.NonceA.Equals(NaFromDecrypt))
                {
                    //all good
                    Console.WriteLine("all good at end");
                }
                else
                {
                    //not good.
                    Console.WriteLine("all not good at end");
                }
                return String.Empty;
            }

            public string encodeMessage3()
            {
                //{Nb}Kab
                string encNb = BCEngine.AESEncryption(this.NonceB, this.sessionKey);

                return encNb;
            }

            //Entity B encoding & decoding..

            private string decodeMessage1(string message)
            {
                // Decode: A,{Na,{H(Na)}Ka-1}Kb

                string[] split = message.Split(',');

                //A
                this.IdentityA = split[0];

                //Decode both halves
                StringBuilder combine = new StringBuilder();

                combine.Append(BCEngine.RSADecryption(split[1], this.PrivateKeyB, true));
                combine.Append(BCEngine.RSADecryption(split[2], this.PrivateKeyB, true));

                Console.WriteLine("yiopee");
                //Na,{H(Na)}Ka-1
                Console.WriteLine("combi: \n" + combine.ToString());
                split = combine.ToString().Split(',');

                //Na
                String tempNonceA = split[0];

                //input: {H(Na)}Ka-1 -> output: H(Na)
                string tempHashNonceA = BCEngine.RSADecryption(compensateForPadding(split[1]), this.PublicKeyA, false);

                //Create a hash from Na above.
                string newHashNonceA = BCEngine.ComputeHash(tempNonceA, "SHA256");

                Console.WriteLine("Nonce A+B: " + tempNonceA + "," + this.NonceB);
                //Compare both hashes
                if (newHashNonceA.Equals(tempHashNonceA))
                {
                    //good
                    Console.WriteLine("hashes were equal..");
                    this.NonceA = tempNonceA;
                }
                else
                {
                    //bad
                    Console.WriteLine("hashes were not equal..");
                }
                
                this.sessionKey = BCEngine.ComputeHash(this.NonceA + this.NonceB, "SHA256");
                

                return String.Empty;
            }

            private string encodeMessage2()
            {
                //B,{Nb,{H(Nb)}Kb-1}Ka,{Na}Kab
                StringBuilder build = new StringBuilder();

                build.Append(this.IdentityB + ",");

                string hash = BCEngine.ComputeHash(this.NonceB, "SHA256");

                string encHash = BCEngine.RSAEncryption(hash, this.PrivateKeyB, true);

                string toEncrypt = this.NonceB + "," + encHash;

                string firstHalf = BCEngine.RSAEncryption(toEncrypt.Substring(0, (toEncrypt.Length / 2)), this.PublicKeyA, false);
                string secondHalf = BCEngine.RSAEncryption(toEncrypt.Substring((toEncrypt.Length / 2), (toEncrypt.Length / 2) - 1), this.PublicKeyA, false);

                string encNa = BCEngine.AESEncryption(this.NonceA, this.sessionKey);

                build.Append(firstHalf + "," + secondHalf + "," + encNa);

                return build.ToString();
            }

            public string decodeMessage3(string message)
            {
                //decode: {Nb}Kab
                string Nb = BCEngine.AESDecryption(message, this.sessionKey);

                if (this.NonceB.Equals(Nb))
                {
                    Console.WriteLine("Nbs are good");
                }
                else
                {
                    Console.WriteLine("Nbs are not good");
                }


                return String.Empty;
            }
            
            private static string compensateForPadding(string message)
            {
                //Console.WriteLine("message len: " + message.Length + " modulo: " + (message.Length % 4));
                int howMany = 4 - message.Length % 4;
                int index = 0;
                while (index < howMany)
                {
                    message = message + "=";
                    index++;
                }
                return message;
            }

            private static int getSectionLength(string section)
            {               

                int len = 0;
                if (Int32.TryParse(section, out len))
                {
                    //Get length in number of bytes                
                    return (len / sizeof(Char));
                }
                else
                {
                    return -1;
                }

            }

            private static string addMsgSize(string message)
            {
                String msgSize = "" + (message.Length * sizeof(Char)).ToString("00000000");
                return msgSize + message;
            }

            private static string decodeMessageUsingMsgSizeHeader(string message)
            {
                int index = 0;
                while (index < message.Length)
                {

                }
                
                return message;
            }

            public static string testPublicKeyA = "-----BEGIN PUBLIC KEY-----" + Environment.NewLine +
"MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAkFJToBedb2N2PRRkns0N" + Environment.NewLine +
"aXQu3qZay+LlIC3cYP3dmNS8cNugwpdHmB4kFp4VXvc4DsVeRHeJjkunccdFqf02" + Environment.NewLine +
"WzSZTnCWTinDQ9+m5qzg9YAYHDtGMxeMEa1iyUwlUnuQ9THj2CYYTCDf2OjgQzQU" + Environment.NewLine +
"hB+Ygt0LePMeGcefSdE3R1xTdkvBPpgeTIyb1QOVBrDiPpZWt/2v/2wtK+Fo7CwD" + Environment.NewLine +
"pNdTdVQYnYmMygqYqCeuPII4lVaTgp9ik8XkiKep/1SLMjKyzVoiGzsIaWClipLo" + Environment.NewLine +
"k8O8O9/goaekE9VqogJwnlkwsXES8QhixIbJlUPlnwCKaWvNfqJ8YXo1rHc0VFiN" + Environment.NewLine +
"WU+nzaD9PQsRqw8/yf1dO3X5xJEGUz9EqZUv6B09z9GH8nuhHyNxfF8WXvHBBREd" + Environment.NewLine +
"6TGgsDvSQcRr/7tg2fUxP1jfWh0OdZomUcNr2TmC3uL83Nwm/uCN8G01z1TxLW1r" + Environment.NewLine +
"TYg9zYRYazADJA7Hd1AXrZXiE6X7jiJqYiKg+NlfAw/3n42+CBH59QaCfuoRZaSd" + Environment.NewLine +
"xQ7r1ENM7PeqqHzDUgMH3lb0wnzdVwTuNP+hdWYWQp5H5w9Ma0x4TMUi/TZ7K2GY" + Environment.NewLine +
"gdxVN2OCE/n+GyUlIbx3l5Fu8/IHyBQdqniYoS3QW2LS/foq3lFD0Vl1M8DumxBR" + Environment.NewLine +
"j7PUfysX8gZJg5D6o+eWUm8CAwEAAQ==" + Environment.NewLine +
"-----END PUBLIC KEY-----" + Environment.NewLine;

            public static string testPrivateKeyA = "-----BEGIN RSA PRIVATE KEY-----" + Environment.NewLine +
"MIIJJwIBAAKCAgEAkFJToBedb2N2PRRkns0NaXQu3qZay+LlIC3cYP3dmNS8cNug" + Environment.NewLine +
"wpdHmB4kFp4VXvc4DsVeRHeJjkunccdFqf02WzSZTnCWTinDQ9+m5qzg9YAYHDtG" + Environment.NewLine +
"MxeMEa1iyUwlUnuQ9THj2CYYTCDf2OjgQzQUhB+Ygt0LePMeGcefSdE3R1xTdkvB" + Environment.NewLine +
"PpgeTIyb1QOVBrDiPpZWt/2v/2wtK+Fo7CwDpNdTdVQYnYmMygqYqCeuPII4lVaT" + Environment.NewLine +
"gp9ik8XkiKep/1SLMjKyzVoiGzsIaWClipLok8O8O9/goaekE9VqogJwnlkwsXES" + Environment.NewLine +
"8QhixIbJlUPlnwCKaWvNfqJ8YXo1rHc0VFiNWU+nzaD9PQsRqw8/yf1dO3X5xJEG" + Environment.NewLine +
"Uz9EqZUv6B09z9GH8nuhHyNxfF8WXvHBBREd6TGgsDvSQcRr/7tg2fUxP1jfWh0O" + Environment.NewLine +
"dZomUcNr2TmC3uL83Nwm/uCN8G01z1TxLW1rTYg9zYRYazADJA7Hd1AXrZXiE6X7" + Environment.NewLine +
"jiJqYiKg+NlfAw/3n42+CBH59QaCfuoRZaSdxQ7r1ENM7PeqqHzDUgMH3lb0wnzd" + Environment.NewLine +
"VwTuNP+hdWYWQp5H5w9Ma0x4TMUi/TZ7K2GYgdxVN2OCE/n+GyUlIbx3l5Fu8/IH" + Environment.NewLine +
"yBQdqniYoS3QW2LS/foq3lFD0Vl1M8DumxBRj7PUfysX8gZJg5D6o+eWUm8CAwEA" + Environment.NewLine +
"AQKCAgBNWIHO07khQEnW9D30yWo9sPGJi9gvWtt3An0QUh3X0XNofJxMjWzmPokS" + Environment.NewLine +
"wggsDAw0Bly+Dt5er3b+yFAyiSz/dlIPMtGq9EDc+FjnWZF6oPrK7o1xxlXgB29g" + Environment.NewLine +
"+HksGmMWtXUpm0j8S8YL5sqB2cCBCrnesH58hLcGE/DvS7v3d6iXRoQ7eqUKW3UM" + Environment.NewLine +
"lU3h9xxZdJLnKoOPPTd8Q+LKZ9BQIMJup/JFQ7l7cnBb9mAvt46BopONtsPK9IzF" + Environment.NewLine +
"HC5EU9gBwCAJZBvRQjMA3rX5bUBOKOGRqSsnF8QciQ0L5IjcpGovPq1rFhZwwtf4" + Environment.NewLine +
"Mho2u8ByPT81dfl4+FkZpA0cgxTFDNesNkn8Y3R1xsP35L9rAGq6WzUivgXJP0cp" + Environment.NewLine +
"/tkraGmRNvB0hbtJ9wFOFvlzggzLiCj3CrZ+L19Aq5fofm0S8AcyPnG81mSW+2Dv" + Environment.NewLine +
"i98d/xjc17zuNVykQ2lK4lDPf6noibHX5ul4ThlXr8Ky+XWiA5Sh5LlZf8EaU+zy" + Environment.NewLine +
"fuHOVqPjr4Udal807k6bWfaLvJjzFJkMCJsN4mhj0Z7h4JpcBpqrk/D7kjLIy8ji" + Environment.NewLine +
"vvHXpxESjH6FMiCC8mDOrdEBsrO+jX1ImaieNEmKP+KjQlaXckUlKgUHmhx0ORBP" + Environment.NewLine +
"6FN2WUfeDXABZtWGYNDppE8iQGYdjVHiWn/YkpqJrDWH1i2NSQKCAQEA7lFEaXuZ" + Environment.NewLine +
"kV3Bcw6dADgY0hGvBP+qoqtwg5lVEdzwZCDsa4yCawVTsVE/P4tr3kKE21aSBMoT" + Environment.NewLine +
"0suqO9HYVcqwKk49UEYeM/G7epVZ4ZH7yh75QmITWNKJZU2X8MUEY7milgCr7VsW" + Environment.NewLine +
"oI86TH+VcPln71251AAmwK6r8/Pi1P+gItnwtRJYp0GpitQD+xBAV65GAsImHCmZ" + Environment.NewLine +
"URRRXAkH4gmR0Z1BacyUJ/akBxkbhKt6v6zmap3KJosJVcIxDiFQiLswzH8ioBO3" + Environment.NewLine +
"jyW6aKGdbmKv9YvAObkTw6KqnyLEQT/ahHBKnQ/OYdl9gUUcy7j9FD0EUxMyqHhY" + Environment.NewLine +
"CsKrm3CZa/ptWwKCAQEAmwemeypq2pIBAyzzZzNDOMl1dYW1NJP6+7vW4Uj+QvBW" + Environment.NewLine +
"ciULdo5fx2yWbw/rdlVTwXOEJj4Dq6i+quoqROZ8xvqxt6ch1XbAOUjSH1M0d19o" + Environment.NewLine +
"wMXF8L2zfRj7k3awNcmaL2jqzXoafqIS5xfSn1wc7VYX73LIifv0EfqGaruFWstR" + Environment.NewLine +
"kfyPj9RLtJDTOXOjalVBeeKy149Etn8WfArXH+uR2dlQaS1SXlTCYGfiLDHQv3Dt" + Environment.NewLine +
"hV6YR8jqDg3lTmhN924TORsciJaLYQPBkhC5+pzl+FFwpQ6ATwW3CCevVgk/+OST" + Environment.NewLine +
"Ng12d1PmExmdiMUtkVAoqg+0mrBuNfWOdkcM6X9XfQKCAQBElQaHBJbRCpYdMltk" + Environment.NewLine +
"MMCT05r2aU0FuyiJ9ppQpbBYYFEpMipl+gZ3xNXax5inQaVSKbujvTOvOgUnaeBD" + Environment.NewLine +
"8Cx6QEHM1CDk+e/l+wz+qTA4nmlE/UxsB0qa6JWNKGV2/XkYieDwUYJVemJgmWa8" + Environment.NewLine +
"OEn8zJApvlFoqdu6PLlOarH+1ZE7yqfQmkjcNt9eZPLfSLvFF3I4MJB6kMpJHiAy" + Environment.NewLine +
"oGZiWEr203Tfe08A6+zLZT3R40P78qS2KtTo5RWQ521xq80DTKL+Ri5Q956JddkE" + Environment.NewLine +
"Z0oT87/B3M0fQ7SuTycDUAjmjBos0NuntRs1FFqRFg9ev6B6989gRCGyFwujk+fS" + Environment.NewLine +
"9yJ5AoIBAHgqD+K72CMetGYvu2Ksm1gy3zZ+sxvT7+Cbkk0A9QQRog/Lovz8EkVT" + Environment.NewLine +
"Z68iWdJZBRiXX0D9JH8zxsZXxves596bhpDhnRoGd6xvQ19AcRRuAZYaNfkKMUuv" + Environment.NewLine +
"x6BfiOnIIBjLa+Rk1pB9M8Wn83vOPCXCa78P61z4zA/7baDhRNZBbjKH3wcO+Lc2" + Environment.NewLine +
"4mJPvcS33I6LJzBqPkpua5EuHd0CDQUcqnU7yfKQJDHxk5/J5RHeiFyuG77YfoLy" + Environment.NewLine +
"RmDl/DjjO0cyOoWsmtBRxwJesKkOYDp/dZ6ahN27gklx6Tf453sWQPzOppqLj3QS" + Environment.NewLine +
"kMbofw95YGugzM5yHpr4gLoxDFMYf0kCggEAOAlExbqHODfcxYQvfhUO1+L/mps+" + Environment.NewLine +
"6K4Lz8yS8IEfGWlQvavYGgLdw9dGKYYGG9lLgqFUBAcLh/czQYgEdSSWjiFrWD+i" + Environment.NewLine +
"fjt5+7o2eUV5yUQuutGzN778rZYBP7tzY3sXLaBq7kAoOHGzdvt722ujGmgiQv54" + Environment.NewLine +
"KAUeJVFAh7dqPxVZfSPYKUekSFJCN6DysmkwcSdZ2LMUOUMO1q6avcCapAOXoiwT" + Environment.NewLine +
"yGpevOc6D1y1+v4i2wBoedyUa5RYHMqqbgehCD+6W95DFM0T3N2w68y1KWkeDC/Y" + Environment.NewLine +
"1mNd+X7iy//cbB7VjXzxwDfj/O8co58Zf97fAfuhW7Bl02b6rn3ar6Bniw==" + Environment.NewLine +
"-----END RSA PRIVATE KEY-----" + Environment.NewLine;

            public static string testPublicKeyB = "-----BEGIN PUBLIC KEY-----" + Environment.NewLine +
"MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAoWU3t31hgcIt4iWJibtY" + Environment.NewLine +
"qDilfs4EavR+T2Sd3aNcLw2RbYOgR3c3a2hA3wXCtZyhjtAlZY7RpG2NgzePg62N" + Environment.NewLine +
"RlVRkqGMJEUZ+X5w2HjVJ2St331cPGIB8gZ2G1I4c0pgJjdU/7kIL4Nkp2kF/jnD" + Environment.NewLine +
"gq169HHvOSSu8GAzCplp2N5VRERWm/fCUij7pNC5kebBjwIgVSjF7PkJ9/eHyojk" + Environment.NewLine +
"HsGiOQyvKVXFQTlGK54dbig6wq4fex21xF4JKpQkSNDgdukeODHoLm22HJJkGktV" + Environment.NewLine +
"Ncl2lf6GN1+fvkA3yhbIbOa2q5CNxFNi+IhvwCRu8VoCDExvitvErsw76eGRuVog" + Environment.NewLine +
"RjpSduA0hk2+1IjZtRNaB5189MOxey85vdhqfVCNITZwWcBKCnD1QgWhI+wmzEqg" + Environment.NewLine +
"omHH6ECNDhzexNEyA7DHqSlIuGVP+FnDZ8ZM2RSrrnmriJUsjZnfyCCj+Qmiuqx0" + Environment.NewLine +
"xNu5KSNCjRPuUYVM84ge1H+/CcH/XldqUmMvNw6b8D3iGHKvXf4O+mL577/MXoIU" + Environment.NewLine +
"OmJkAhafvvTrws/X9H10dnZJgKsrOXnxhkroU0sqYpDnieHDbNTTNRcUouC5Io1g" + Environment.NewLine +
"uFTW6LEtSDvsb1Et/aaYDOXezumaEc/UNZZs5kZsQGr8n5i4fbFa+bjPTmG+FI0I" + Environment.NewLine +
"cWDaL5d7KiP/nLcDSYHi4N8CAwEAAQ==" + Environment.NewLine +
"-----END PUBLIC KEY-----" + Environment.NewLine;

            public static string testPrivateKeyB = "-----BEGIN RSA PRIVATE KEY-----" + Environment.NewLine +
"MIIJKAIBAAKCAgEAoWU3t31hgcIt4iWJibtYqDilfs4EavR+T2Sd3aNcLw2RbYOg" + Environment.NewLine +
"R3c3a2hA3wXCtZyhjtAlZY7RpG2NgzePg62NRlVRkqGMJEUZ+X5w2HjVJ2St331c" + Environment.NewLine +
"PGIB8gZ2G1I4c0pgJjdU/7kIL4Nkp2kF/jnDgq169HHvOSSu8GAzCplp2N5VRERW" + Environment.NewLine +
"m/fCUij7pNC5kebBjwIgVSjF7PkJ9/eHyojkHsGiOQyvKVXFQTlGK54dbig6wq4f" + Environment.NewLine +
"ex21xF4JKpQkSNDgdukeODHoLm22HJJkGktVNcl2lf6GN1+fvkA3yhbIbOa2q5CN" + Environment.NewLine +
"xFNi+IhvwCRu8VoCDExvitvErsw76eGRuVogRjpSduA0hk2+1IjZtRNaB5189MOx" + Environment.NewLine +
"ey85vdhqfVCNITZwWcBKCnD1QgWhI+wmzEqgomHH6ECNDhzexNEyA7DHqSlIuGVP" + Environment.NewLine +
"+FnDZ8ZM2RSrrnmriJUsjZnfyCCj+Qmiuqx0xNu5KSNCjRPuUYVM84ge1H+/CcH/" + Environment.NewLine +
"XldqUmMvNw6b8D3iGHKvXf4O+mL577/MXoIUOmJkAhafvvTrws/X9H10dnZJgKsr" + Environment.NewLine +
"OXnxhkroU0sqYpDnieHDbNTTNRcUouC5Io1guFTW6LEtSDvsb1Et/aaYDOXezuma" + Environment.NewLine +
"Ec/UNZZs5kZsQGr8n5i4fbFa+bjPTmG+FI0IcWDaL5d7KiP/nLcDSYHi4N8CAwEA" + Environment.NewLine +
"AQKCAgBfJRdqkXypDTsVZYGmc455ZSOTFIqgLtBDp5I1NffDOWFxSTZ0ywAdzpDn" + Environment.NewLine +
"qTK288Z+NZDGRSKrp3XUVC3Dt81gGC4FnjzKqP3+Ch8mTl2CYqTp6rI0WqbA8jQw" + Environment.NewLine +
"ORFUThVOkjIGqyL7N59f3dcNnyn14KVqc7xOWKTUyjFs3zH6CmAD5bGVMsMYwlZP" + Environment.NewLine +
"PEkZjQqwbtV2vpmn8MyCpSclK/wncYlbznF4kbq+j7AhSI4bAZZabGHcp5AfWjxX" + Environment.NewLine +
"IIwfbRvWnekVwb4ZmM3SHC7tHVn3YnQJSsn+3N2EP8Fj3nh7Uqt/irE4etwOnggb" + Environment.NewLine +
"Ip9QovowG+Np3dMeJQJxB2xr84iwS1F+ZaiNq5tKJdnWI/6uvERJWlJ5akV90gFR" + Environment.NewLine +
"oImiOJs/Z2u7awPiU/XdJn6ArYeMkJpdKS67I6RqF4cWnG/ijDmE0/8iZLtnQRIA" + Environment.NewLine +
"uTpurIFsxT8T66cIh66I90FYXtWENNjIvRstabLtT+hGhbfG00TSH50/VZ/AZqqv" + Environment.NewLine +
"L3q4MeLHYXBi37gaqe1aZ87KgZIoGzgrl7krYgZz5DvqiuHgH3/M1Ed76/XWV10v" + Environment.NewLine +
"2mqjrhKvXaeEZxaMFkekb3vlu+cAvFPu7E6WIFqxLdIHVEzh7McA1V9i2EyvrmJg" + Environment.NewLine +
"Jxy5YqjytS5RCaOQ0Fe79iRGXf5qAxbgdHHJLJDAN6gvEMGoGQKCAQEA7eayFgTT" + Environment.NewLine +
"TqQFGDlqndGoUlcS7UE5Lv7sCzWd6EQQYJ/IEGbD6X5VP4w8U/eYFQ2KQsE2XQ/r" + Environment.NewLine +
"Fgm4BYlfzld7T6LsqfjLd7TcywZ4/ijoF/71aU/uNxoKg3QHX6Q8uFKDGsHrezrz" + Environment.NewLine +
"+LixkqGwO8dTKzCvDyuvumT9FuVR005+UcSqjol3gLU7KCkMAQqHSu7A/MwpXHcB" + Environment.NewLine +
"HSk3ZIOeurGC+/y8DAhKeynSfXIXUAZRSMODuUKO81sXn8xN8Nf/nobODsE4ECQH" + Environment.NewLine +
"/HDiaucqpO87ZOYjX/8+G3sBTlanNF5ElDAZRULV+f5tXWPJjVhFVXWRp4gUvW5y" + Environment.NewLine +
"OhshVgNdPxpykwKCAQEArayDrjl1EVjAvFRh0uwfowRKUHCQecXyjs0EcsQlwJTf" + Environment.NewLine +
"1L97d4UmHgCYSm3hFZ2F6jkdU9XYWsFEIyKy7c/cPgGgd2qdH6rz1H9GcX41zoVL" + Environment.NewLine +
"HwIV5v8zfpgVFrGIOsTa11Xq4+kTIje39JfioT2B4UVHZM78CKRCq1HhgbZWrahI" + Environment.NewLine +
"zarMXLeTrsXcj7Xg4xwN03PLjh+hRQuqEuDxUs3MSdSRcOQGHhBGtXjIrr7A6K62" + Environment.NewLine +
"XvNx9QlU96pTyjUv8rI1DBvATgpoXOWrtSLRIr+1yfwqI+YqGsBmRL841rf0Hfl1" + Environment.NewLine +
"6Yp8d8epvppKZRBqUcZb2q8XmlMhSwZ2TPP6VBlMBQKCAQAfkvCrz3QkCczLTEOE" + Environment.NewLine +
"MqE+XGQcrP1j6V12l2UfwHjT+iDZXGpAAQ5bYsdW9ZNvp95jwbTvYP94a03LHlJi" + Environment.NewLine +
"1mV9SkhSvrxyZMSxBWjlgpRbKvFzSrJ5CbG/hJS98tREOl9AG+Ce+FIM/qZCqcb0" + Environment.NewLine +
"CB5XqxxwzQGDoYedlE3p148YVxuz0zbTFDqHt/rp4HXYUhu28XnD1d+F+URqyLU2" + Environment.NewLine +
"XfzeaMqkZi1Mb16KW6wQaOY83AYNfnHuhnZ6NOlbP6+jYIzUlHxWWBHfRTQnsAqO" + Environment.NewLine +
"3VlqqMilIwEhMJAZz5Jddu1vJXsVICu6BDSLooTLh52cHAB61aq7AX+Tiqo/+i5U" + Environment.NewLine +
"A7SVAoIBAHeSO8L6+hesVF4VdphiS1HQBV4mccH1QA+DJZniY9+YYBa4ksfyGxEy" + Environment.NewLine +
"9abgycQ5BNcC5acvptqtDz1liW1j282xDwrIk08XQvK9ggFnlKIQcWzy4aN9drWk" + Environment.NewLine +
"/Xf3WMSlfcod9C9f6/V2CSfUXosGruIq8YF9ZFaKyP/syuakZD8BisZW3obDOWFu" + Environment.NewLine +
"BnHHFgMm8Hnx0maSblT8N+bxihSpbgoy2MOxqmiOajBM7VYqLOTGTPdIgt/iweYW" + Environment.NewLine +
"FcQ1JELi4NTqUlcooTu8QKDgTL6w4Pckrtqyf7CVYJPaV8a1NdRhSQY2e4V4KOz6" + Environment.NewLine +
"i1fTmm+csub3/7cXnudplo9atrGqDQUCggEBAKRSXItusBp+x8yQxTQqcIBsqQ4h" + Environment.NewLine +
"bxHYdintzSn4saOc4BGypNfQRFH3zj2kdPp+zm9P+ODebqXzh26kMyvrqW8BWck1" + Environment.NewLine +
"qXmDcqs9mKAbPVlMXd0Uao6tc93OTwNbYrvYv+3FWcsUaP17Epy0NC5Ujlku2h3v" + Environment.NewLine +
"kcMKhoH4cfpQjnmITYrPrkCV5SquzM/BTzMKoLMbBadoX7Cv3+JwHftJICz3Ep4u" + Environment.NewLine +
"e1S9kKxKl4cg9HwDZBd1pRO8lTZiEhJ2/I7UMb4y3W1vONBHae0iifkXnBJ7TaGE" + Environment.NewLine +
"4v1baq3bVCV3WBGxRRcLWTXBAXTLTIOZMjno6uFVAKndMwTKcTjoPF727h4=" + Environment.NewLine +
"-----END RSA PRIVATE KEY-----" + Environment.NewLine;
        }

    }
}
