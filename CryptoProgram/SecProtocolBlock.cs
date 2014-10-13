using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto;

namespace CryptoProgram
{
    public abstract class SecProtocolBlock
    {
        public string data { get; set; }
        public bool isReversible { get; set; }

        public SecProtocolBlock()
        {
            this.data = String.Empty;
            this.isReversible = true;
        }

        //public abstract SecProtocolBlock deepCopy(SecProtocolBlock toCopy);

        public abstract string doWork(string data);

        public abstract string reverseWork(string data);

        /*
        public SecProtocolBlock(int blockType, string data)
        {
            this.blockType = blockType;
            this.data = data;
        }
        */

    }

    public class SecProtocolBlockData : SecProtocolBlock
    {

        public SecProtocolBlockData(string data)
        {
            base.data = data;
            base.isReversible = false;//should be true?
        }

        public override string doWork(string data)
        {
            return base.data;
        }

        public override string reverseWork(string data)
        {
            return base.data;
        }

    }

    public class SecProtocolBlockHashing : SecProtocolBlock
    {
        public static readonly string HASH_SHA1 = "SHA1";
        public static readonly string HASH_SHA256 = "SHA256";
        public static readonly string HASH_SHA384 = "SHA384";
        public static readonly string HASH_SHA512 = "SHA512";
        public static readonly string HASH_MD5 = "MD5";
        
        public string hashType { get; set; }

        public SecProtocolBlockHashing(string hashType)
        {
            base.data = null;
            base.isReversible = false;
            this.hashType = hashType;
        }

        public override string doWork(string data)
        {
            string result = null;

            //do some hashing on data and return it
            //result = BCEngine.ComputeHash(data, this.hashType);
            result = "H(" + data + ")";

            return result;
        }

        public override string reverseWork(string data)
        {
            string result = null;

            //do some hashing on data and return it
            //result = BCEngine.ComputeHash(data, this.hashType);
            //result = "H(" + data + ")";
            data = data.Remove(0, 2);
            data = data.Remove(data.Length - 1, 1);
            result = data;
            return result;
        }
    }

    public class SecProtocolBlockEncryption : SecProtocolBlock
    {
        public static readonly int ENC_AES = 0;
        public static readonly int ENC_RSA = 1;
        
        public int encryptionType { get; set; }
        public string key { get; set; }
        public AsymmetricCipherKeyPair rsaKeys { get; set; }
        public bool usePrivateKey { get; set; }

        public SecProtocolBlockEncryption(int encryptionType, string key)
        {
            base.data = null;
            base.isReversible = true;
            this.encryptionType = encryptionType;
            this.key = key;
        }

        public SecProtocolBlockEncryption(int encryptionType, AsymmetricCipherKeyPair rsaKeys, bool usePrivateKey)
        {
            base.data = null;
            base.isReversible = true;
            this.encryptionType = encryptionType;
            this.rsaKeys = rsaKeys;
            this.usePrivateKey = usePrivateKey;
        }


        public override string doWork(string data)
        {
            string result = null;

            //do some encrypting on data and return it
            /*
            if (this.encryptionType == SecProtocolBlockEncryption.ENC_AES)
            {
                result = BCEngine.AESEncryption(data, this.key);
            }
            else if (this.encryptionType == SecProtocolBlockEncryption.ENC_RSA)
            {
                if (this.usePrivateKey)
                {
                    result = BCEngine.RSAEncryption(data, this.rsaKeys.Private);
                }
                else
                {
                    result = BCEngine.RSAEncryption(data, this.rsaKeys.Public);
                }
            }
            */

            //While testing keep things simple
            result = "{" + data + "}K";


            return result;
        }

        public override string reverseWork(string data)
        {
            string result = null;

            //do some encrypting on data and return it
            /*
            if (this.encryptionType == SecProtocolBlockEncryption.ENC_AES)
            {
                result = BCEngine.AESDecryption(data, this.key);
            }
            else if (this.encryptionType == SecProtocolBlockEncryption.ENC_RSA)
            {
                if (this.usePrivateKey)
                {
                    result = BCEngine.RSADecryption(data, this.rsaKeys.Private);
                }
                else
                {
                    result = BCEngine.RSADecryption(data, this.rsaKeys.Public);
                }
            }
            */

            //While testing keep things simple
            data = data.Remove(0, 1);
            data = data.Remove(data.Length - 2, 2);
            result = data;


            return result;
        }

    }
}
