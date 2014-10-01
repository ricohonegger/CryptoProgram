using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Encodings;
using System.IO;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;

//AES functions we from here: http://elian.co.uk/post/2009/07/29/Bouncy-Castle-CSharp.aspx
//However, site seems to be down/gone..

namespace CryptoProgram
{
    public class BCEngine
    {
        /*
         * Some notes:
         * 
         * 1) The AES encryption needs improvement in the sense of cleaner code/structure.
         *
         * 
         * 2) The RSA encryption/decryption might need to be modified to allow for encoding
         *    options to be specified.
         */
        
        
        private readonly Encoding _encoding;
        private readonly IBlockCipher _blockCipher;
        private PaddedBufferedBlockCipher _cipher;
        private IBlockCipherPadding _padding;

        private static String _privateFile = "private.key";
        private static String _publicFile = "public.key";

        public BCEngine(IBlockCipher blockCipher, Encoding encoding)
        {
            _blockCipher = blockCipher;
            _encoding = encoding;
        }

        public void SetPadding(IBlockCipherPadding padding)
        {
            if (padding != null)
                _padding = padding;
        }

        public string Encrypt(string plain, string key)
        {
            byte[] result = BouncyCastleCrypto(true, _encoding.GetBytes(plain), key);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipher, string key)
        {
            byte[] result = BouncyCastleCrypto(false, Convert.FromBase64String(cipher), key);
            return _encoding.GetString(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forEncrypt"></param>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="CryptoException"></exception>
        private byte[] BouncyCastleCrypto(bool forEncrypt, byte[] input, string key)
        {
            try
            {
                //If padding isn't specified don't use it.
                _cipher = _padding == null ? new PaddedBufferedBlockCipher(_blockCipher) : new PaddedBufferedBlockCipher(_blockCipher, _padding);
                //byte[] keyByte = _encoding.GetBytes(key);
                byte[] keyByte = Convert.FromBase64String(key);
                _cipher.Init(forEncrypt, new KeyParameter(keyByte));                
                return _cipher.DoFinal(input);
            }
            catch (Org.BouncyCastle.Crypto.CryptoException ex)
            {
                throw new CryptoException("crypto ex.." + ex.ToString());
            }           
        }

        public static Encoding _encodingS = Encoding.ASCII;//not actually used since I hardcoded base64
        public static Pkcs7Padding _paddingS = new Pkcs7Padding();

        /*
         * Encrypt a string using a key which is base64 encoded.
         * Best way to get a key is to use the SHA256 hash.
         */
        public static string AESEncryption(string plain, string key, bool fips)
        {
            BCEngine bcEngine = new BCEngine(new AesEngine(), _encodingS);
            bcEngine.SetPadding(_paddingS);
            return bcEngine.Encrypt(plain, key);
        }

        /*
         * Decrypt a string using a key which is base64 encoded.
         * Best way to get a key is to use the SHA256 hash.
         */
        public static string AESDecryption(string cipher, string key, bool fips)
        {
            BCEngine bcEngine = new BCEngine(new AesEngine(), _encodingS);
            bcEngine.SetPadding(_paddingS);
            return bcEngine.Decrypt(cipher, key);
        }

        /* 
         * Use public or private key to encrypt a piece of unicode encoded text
         * return Base64 encoded string version of the encrypted string
         */
        public static string RSAEncryption(string plaintextAsUnicode, AsymmetricKeyParameter key, bool fips)
        {
            RsaEngine engine = new RsaEngine();
            engine.Init(true, key);

            //Convert .NET Unicode encoded characters to a byte array
            Byte[] bytesToEncrypt = System.Text.UnicodeEncoding.Unicode.GetBytes(plaintextAsUnicode);
            byte[] encryptedText = engine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);

            //Convert to base64, since we don't want to see any nasty characters
            return Convert.ToBase64String(encryptedText);
        }

        /*
         * Use public or private key to decrypt a piece of base64 encoded encrypted string
         * return Unicode encoded decrypted string
         */
        public static string RSADecryption(string plaintextAsBase64, AsymmetricKeyParameter key, bool fips)
        {
            RsaEngine engine = new RsaEngine();
            engine.Init(false, key);

            Byte[] bytesToEncrypt = Convert.FromBase64String(plaintextAsBase64);
            byte[] encryptedText = engine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);

            //All .NET strings are Unicode, so let's make it one again..
            return System.Text.Encoding.Unicode.GetString(encryptedText);
        }

        /*
         * Convert a string to a hash using specific algorithm:
         * e.g SHA1, SHA256, SHA384, SHA512.
         */
        public static string ComputeHash(string plainText, string hashAlgorithm)
        {
            // Convert plain text into a byte array.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);


            // Because we support multiple hashing algorithms, we must define
            // hash object as a common (abstract) base class. We will specify the
            // actual hashing algorithm class later during object creation.
            HashAlgorithm hash;

            // Make sure hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Initialize appropriate hashing algorithm class.
            switch (hashAlgorithm.ToUpper())
            {
                case "SHA1":
                    hash = new SHA1Managed();
                    break;

                case "SHA256":
                    hash = new SHA256Managed();
                    break;

                case "SHA384":
                    hash = new SHA384Managed();
                    break;

                case "SHA512":
                    hash = new SHA512Managed();
                    break;

                default:
                    hash = new MD5CryptoServiceProvider();
                    break;
            }

            // Compute hash value of our plain text.
            byte[] hashBytes = hash.ComputeHash(plainTextBytes);            

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashBytes);

            // Return the result.
            return hashValue;
        }

        
        /*
         * Load a key from file
         */
        public static AsymmetricKeyParameter getKeyFromFile(bool getPrivate)
        {
            AsymmetricCipherKeyPair keyPair;
            AsymmetricKeyParameter theKey;
            if (getPrivate)
            {
                using (var reader = File.OpenText(_privateFile))
                    keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();

                theKey = keyPair.Private;
            }
            else
            {
                using (var reader = File.OpenText(_privateFile))
                    keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();

                theKey = keyPair.Public;
            }

            return theKey;
        }

        /*
         * Generate the keypair and optionally savetofile or return in the object
         */
        public static void generateKeys(bool saveToFile, bool returnAsObject, ref AsymmetricCipherKeyPair keys, int size)
        {            
            
            RsaKeyPairGenerator keygen = new RsaKeyPairGenerator();
            keygen.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(SecureRandom.GetInstance("SHA1PRNG"), size));
            
            //Generate the pub/pri keypair
            AsymmetricCipherKeyPair keyPair = keygen.GenerateKeyPair();

            if (saveToFile)
            {
                TextWriter textWriter = new StreamWriter(_privateFile);
                PemWriter pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Private);
                pemWriter.Writer.Flush();
                textWriter.Close();

                textWriter = new StreamWriter(_publicFile);
                pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Public);
                pemWriter.Writer.Flush();
                textWriter.Close();
            }

            if (returnAsObject)
            {
                keys = keyPair;
            }
        }

        /*
         * If we have a pair of keys and want to select one as string
         */
        public static String getKeyFromKeyPair(AsymmetricCipherKeyPair keys, bool getPrivate)
        {
            String theKey = "";
            if (getPrivate)
            {
                PrivateKeyInfo pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keys.Private);
                theKey = Convert.ToBase64String(pkInfo.GetDerEncoded());

            }
            else
            {
                SubjectPublicKeyInfo info = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keys.Public);
                theKey = Convert.ToBase64String(info.GetDerEncoded());
            }

            return theKey;
        }

        /*
         *  If we have a single key and want to output as string.
         */
        public static String getKeyFromKey(AsymmetricKeyParameter key, bool getPrivate)
        {
            String theKey = "";
            if (getPrivate)
            {
                PrivateKeyInfo pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(key);
                theKey = Convert.ToBase64String(pkInfo.GetDerEncoded());

            }
            else
            {
                SubjectPublicKeyInfo info = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(key);
                theKey = Convert.ToBase64String(info.GetDerEncoded());
            }

            return theKey;
        }

    }
}
