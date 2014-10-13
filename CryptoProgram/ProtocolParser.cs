using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CryptoProgram
{
    class ProtocolParser
    {
        //EBNF Grammar
        /*
         * Since there is no scanner, only single character keywords are feasible.
         * Current keywords: I (Identifier), T (Timestamp), N (Nonce), C (Certification Authority), { (Encryption), H (Hash)
         * 
         * [ ] -> Zero or one instance of (Option operator)
         * { } -> Zero or more instances of (Repetition operator)
         * |   -> Or operator
         * ( ) -> Used to group terms (Group symbol)
         * #   -> Commented out rule
         * 
         * <Protocol>       := <Expression> { ":" <Expression> } ["." <DataEncrypt>]
         * <Expression>     := <Block> {"," <Block> }
         * <DataEncrypt>    := "{" "}" "K" (("a" ["b" | ["-" "1"]]) | ("b" ["-" "1"]))
         * <Block>          := <Identifier> | <TimeStamp> | <Nonce> | <Encrypt> | <Hash> | <CertAuth>
         * # <Identifier>   := <Alpha> { <Alpha> | <Num> }
         * <Nonce>          := "N" <Alpha>
         * <CertAuth>       := "C" <Alpha>
         * <TimeStamp>      := "T" <Alpha>
         * <Identifier>     := "I" <Alpha>
         * <Encrypt>        := "{" <Expression> "}" "K" (("a" ["b" | ["-" "1"]]) | ("b" ["-" "1"]))
         * <Hash>           := "(" <Expression> ")" "H" 
         * <Alpha>          := "a" .. "Z"
         * <Num>            := "0" .. "9"
         * 
         * Extension to hash/encryption may be needed to specify type of hash/encryption function
         * Could restrict <Nonce> to be: "N" ("a" | "b") or loosen to be: "N" <Alpha> { <Alpha> | <Num> }
         * E.g restriction could allow us to only allow 2 entities, which are always called a and b.
         * E.g loosening could allow a broader range of entities such as: Nuser1
         * Similar changes would be needed for <CerthAuth>, <TimeStamp> and <Identifier> if applied.
         */

        //Input string that needs parsing
        public string input;

        //used to index the input string
        public int currentChar;

        //contains current char
        public char lookAhead;

        SecProtocolTree<SecProtocolBlock> tree = new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData(""));
        SecProtocolTree<SecProtocolBlock> pointerToParentNode = new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData(""));
        public List<SecProtocolTree<SecProtocolBlock>> listOfTrees = new List<SecProtocolTree<SecProtocolBlock>>();

        //When data exchange takes place this holds the encoding/decoding block
        public SecProtocolTree<SecProtocolBlock> dataExchangeRule = new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData(""));
        
        /*
         * For Test/output purposes only
         */
        public static readonly int PROTOCOL = 0;
        public static readonly int EXPRESSION = 1;
        public static readonly int BLOCK = 2;
        public static readonly int NONCE = 3;
        public static readonly int IDENTIFIER = 4;
        public static readonly int TIMESTAMP = 5;
        public static readonly int ENCRYPT = 6;
        public static readonly int HASH = 7;
        public static readonly int CERTAUTH = 8;

        public int[] found = { 0, 0, 0, 0, 0, 0, 0, 0, 0};
        String[] foundNames = { "Protocol", "Expression", "Block", "Nonce", "Identifier", "Timestamp", "Encrypt", "Hash", "Certification Authority" };

        

        /*
         * End For Test/output purposes only
         */

        public void getChar()
        {
            if (!(this.currentChar >= this.input.Length))
            {
                this.lookAhead = this.input[currentChar];
                this.currentChar++;
            }            
        }

        public void Error(String error)
        {
            Console.WriteLine("Error: " + error);
        }

        public void Abort(String s)
        {
            Error(s);
            throw new Exception();
            //Application.Exit();
        }

        public void Match(char x)
        {
            if (x == lookAhead)
            {
                //EmitLn("matched: " + x);
                getChar();
            }
            else
            {
                Expected(x, lookAhead);
            }
        }

        public void Expected(char expected, char got)
        {
            Abort("Error: expected char: " + expected + ", but instead got: " + got);
        }

        public void Expected(String s)
        {
            Abort("Error: expected " + s);
        }

        public bool isAlpha(char c)
        {
            return Char.IsLetter(c);
        }

        public bool isDigit(char c)
        {
            return Char.IsDigit(c);
        }

        /*
        public char getName()
        {
            if (!isAlpha(lookAhead))
            {
                Expected("Name");
            }
            char next = this.lookAhead;
            getChar();
            return next;            
        }

        public char getNum()
        {
            if (!isDigit(lookAhead))
            {
                Expected("Digit");
            }
            char next = this.lookAhead;
            getChar();
            return next;   
        }
        */

        public void Emit(String s)
        {
            Console.Write("\t" + s);
        }

        public void EmitLn(String s)
        {
            Emit(s);
            Console.WriteLine();
        }

        public void init()
        {
            this.pointerToParentNode = this.tree;
            getChar();
            //Expression();
            Protocol();
        }

        public void Protocol()
        {
            Expression();
            this.listOfTrees.Add(this.tree);
            while (this.lookAhead == ':')
            {
                this.tree = new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData(""));
                this.pointerToParentNode = this.tree;
                
                Match(':');
                Expression();

                this.listOfTrees.Add(this.tree);

                this.found[ProtocolParser.PROTOCOL]++;
            }
            this.found[ProtocolParser.PROTOCOL]++;
            
            if (this.lookAhead == '.')
            {
                Match('.');
                DataEncrypt();
            }
            else
            {
                this.dataExchangeRule.addChild(new SecProtocolBlockData(""));
            }
             
        }

        public void Expression()
        {
            //EmitLn("MOVE #" + getNum() + ",D0");
            //while(this.lookAhead 

            //
            Block();
            //this.tree.addChild(this.pointerToNode);
            
            while (this.lookAhead == ',')
            {
                Match(',');                
                Block();
                this.found[ProtocolParser.EXPRESSION]++;
            }

            this.found[ProtocolParser.EXPRESSION]++;
           
        }

        public void Block()
        {
            whiteSpace();
            switch (this.lookAhead)
            {
                case 'H':
                    Hash();
                    break;
                case '{':
                    Encrypt();
                    break;
                case 'N':
                    Nonce();
                    break;
                case 'T':
                    TimeStamp();
                    break;
                case 'I':
                    Identifier();
                    break;
                case 'C':
                    CertAuth();
                    break;
                default:
                    //Identifier();
                    Error("Expected one of: {\"H\",\"N\",\"{\"} but got: " + this.lookAhead);
                    break;
            }
            whiteSpace();
            this.found[ProtocolParser.BLOCK]++;
        }

        public void DataEncrypt()
        {

            Match('{');
            Match('}');
            Match('K');

            this.dataExchangeRule.addChild(new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, ""));
            this.dataExchangeRule.getChild(0).addChild(new SecProtocolBlockData(""));

            if (this.lookAhead == 'a')
            {
                Match('a');
                //if next is b - shared session key
                if (this.lookAhead == 'b')
                {
                    Match('b');
                    
                }
                //if -1 then private key of a
                else if (this.lookAhead == '-')
                {
                    Match('-');
                    Match('1');
                    
                }
                //else - public key of a
                else
                {
                    
                }
            }
            //else if(this.lookAhead == 'b')
            else
            {
                Match('b');
                //if -1 then we have private key of b
                if (this.lookAhead == '-')
                {
                    Match('-');
                    Match('1');
                }
                //public key of b
                else
                {

                }
            }

        }

        public void whiteSpace()
        {
            //While this character is whitespace, move onto the next character
            //Also, make sure we don't run into an infinite loop if we reach the end of input.
            while (Char.IsWhiteSpace(this.lookAhead) && this.currentChar < this.input.Length)
            {
                getChar();
            }
        }

        public void Identifier()
        {
            Match('I');
            char temp = Alpha();

            this.found[ProtocolParser.IDENTIFIER]++;
            this.pointerToParentNode.addChild(new SecProtocolBlockData("I" + temp));
        }

        public void Nonce()
        {
            Match('N');
            char temp = Alpha();

            this.found[ProtocolParser.NONCE]++;
            this.pointerToParentNode.addChild(new SecProtocolBlockData("N" + temp));
        }

        public void TimeStamp()
        {
            Match('T');
            char temp = Alpha();

            this.found[ProtocolParser.TIMESTAMP]++;
            this.pointerToParentNode.addChild(new SecProtocolBlockData("T" + temp));
        }

        public void CertAuth()
        {
            Match('C');
            char temp = Alpha();

            this.found[ProtocolParser.CERTAUTH]++;
            this.pointerToParentNode.addChild(new SecProtocolBlockData("N" + temp));
        }

        public void Encrypt()
        {
            //We cant yet determine exact specifications.
            //this.pointerToParentNode.addChild(new SecProtocolBlockEncryption(-1, "key"));
            this.pointerToParentNode.addChild(new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, ""));

            SecProtocolTree<SecProtocolBlock> saveParent = backupParent(this.pointerToParentNode);
            this.pointerToParentNode = selectLatestChild(this.pointerToParentNode);

            Match('{');            
            Expression();
            Match('}');
            Match('K');

            //restore parent
            this.pointerToParentNode = saveParent;

            if (this.lookAhead == 'a')
            {
                Match('a');
                //if next is b - shared session key
                if (this.lookAhead == 'b')
                {
                    Match('b');
                    //this.pointerToParentNode.block = new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, "");
                }
                //if -1 then private key of a
                else if (this.lookAhead == '-')
                {
                    Match('-');
                    Match('1');
                    //this.pointerToParentNode.block = new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_RSA, "");
                }
                //else - public key of a
                else
                {
                    //this.pointerToParentNode.block = new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_RSA, "");
                }
            }
            //else if(this.lookAhead == 'b')
            else
            {
                Match('b');
                //if -1 then we have private key of b
                if (this.lookAhead == '-')
                {
                    Match('-');
                    Match('1');
                }
                //public key of b
                else
                {
                    
                }
            }

            this.found[ProtocolParser.ENCRYPT]++;
        }

        public void Hash()
        {
            this.pointerToParentNode.addChild(new SecProtocolBlockHashing(SecProtocolBlockHashing.HASH_SHA256));

            SecProtocolTree<SecProtocolBlock> saveParent = backupParent(this.pointerToParentNode);
            this.pointerToParentNode = selectLatestChild(this.pointerToParentNode);

            Match('H');
            Match('(');            
            Expression();
            Match(')');

            this.pointerToParentNode = saveParent;
            
            this.found[ProtocolParser.HASH]++;
        }

        public char Alpha()
        {
            if (isAlpha(this.lookAhead))
            {
                char temp = this.lookAhead;
                //Console.WriteLine("\tWriteLine Matched: " + this.lookAhead);
                Match(this.lookAhead);
                return temp;
            }
            else
            {
                //Console.Write("Not alpha: " + this.lookAhead);
                Error("Not alpha: " + this.lookAhead);
                return ' ';
            }
        }

        public void Num()
        {            
            if (isDigit(this.lookAhead))
            {
                //Console.WriteLine("\tMatched: " + this.lookAhead);
                Match(this.lookAhead);
            }
            else
            {
                //Console.Write("Not digit: " + this.lookAhead);
                Error("Not digit: " + this.lookAhead);
            }
        }

        /*
         * retrieve the number of stages for the parsed protocol.
         */ 
        public int getNumberOfStagesInProtocol()
        {
            return this.found[ProtocolParser.PROTOCOL];
        }

        /*
         * Create a copy of input
         */ 
        public SecProtocolTree<SecProtocolBlock> backupParent(SecProtocolTree<SecProtocolBlock> toSave)
        {
            return new SecProtocolTree<SecProtocolBlock>(toSave);
        }

        /*
         * Select the latest child. (not sure why I made this a method)
         */ 
        public SecProtocolTree<SecProtocolBlock> selectLatestChild(SecProtocolTree<SecProtocolBlock> parent)
        {
            return parent.getChild(parent.children.Count - 1);
        }

        //Testing functionality from here on.

        public void main()
        {
            this.currentChar = 0;
            this.input = "IA,{Na,{H(Na)}Ka-1}Kb";

            this.input = " IB , { Nb , { H( Nb ) }Kb-1 }Ka \n, { Na }Kab : IA,{Na,{H(Na)}Ka-1}Kb";

            //this.input = "IA,H(IA),{IA}Kab";
            //this.input = "2";
            init();
            results();
        }

        public void results()
        {
            Console.WriteLine("");
            Console.WriteLine("List of Nonterminal Symbols found:\n");
            for (int i = 0; i < this.found.Length; i++)
            {
                Console.WriteLine(foundNames[i] + ": " + found[i]);
            }

            SecProtocolTreeTEST test = new SecProtocolTreeTEST();
            Console.WriteLine("*****************************");
            for (int i = 0; i < this.listOfTrees.Count; i++)
            {
                test.tree = this.listOfTrees.ElementAt(i);
                Console.WriteLine("Output "+(i+1)+": " + test.traverseTree());                
            }

            /*
            Console.WriteLine("*****************************");
            Console.WriteLine(this.tree.children.Count);
            SecProtocolTreeTEST test = new SecProtocolTreeTEST();
            test.tree = this.tree;
            //SecProtocolTree<SecProtocolBlock> tree2 = new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData(""));
            //tree2.addChild(new SecProtocolBlockData("Ia"));
            //tree2.addChild(new SecProtocolBlockHashing(SecProtocolBlockHashing.HASH_SHA256));
            //tree2.getChild(1).addChild(new SecProtocolBlockData("Ia"));
            //test.tree = this.tree;
            
            Console.WriteLine("Data: " + test.tree.getChild(0).block.data);
            Console.WriteLine("OUtput: " + test.traverseTree());
            String toDecode = test.traverseTree();

            Console.WriteLine("Decode: ");
            test.decodeMessageUsingTree(toDecode, this.tree);
             */
        }

        public List<SecProtocolTree<SecProtocolBlock>> parseProtocol(string input)
        {            
            
            return new List<SecProtocolTree<SecProtocolBlock>>();
        }

        

        
    }
}
