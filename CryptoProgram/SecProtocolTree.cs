using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoProgram
{
    public class SecProtocolTree<SecProtocolBlock>
    {
        public SecProtocolBlock block;
        public LinkedList<SecProtocolTree<SecProtocolBlock>> children;
        public SecProtocolTree<SecProtocolBlock> parent;        
        public int level;
        public int id;

        public SecProtocolTree(SecProtocolBlock block)
        {
            this.block = block;
            this.children = new LinkedList<SecProtocolTree<SecProtocolBlock>>();
            this.level = 0;
            this.id = 0;
        }

        public SecProtocolTree(SecProtocolTree<SecProtocolBlock> tree)
        {
            this.block = tree.block;
            this.children = tree.children;
            this.level = tree.level;
            this.id = tree.id;
        }

        /*
         * Add child creating a new subtree containing the specified SecProtocolBlock
         */ 
        public void addChild(SecProtocolBlock child)
        {
            this.children.AddLast(new SecProtocolTree<SecProtocolBlock>(child) { parent = this, level = this.level + 1, id = this.id + this.children.Count + 1 });
        }

        public void addChild(SecProtocolTree<SecProtocolBlock> child)
        {
            this.children.AddLast(child);
        }

        public SecProtocolTree<SecProtocolBlock> getChild(int i)
        {
            return children.ElementAt(i);
        }
                
    }

    public class SecProtocolTreeFunctions
    {
        /*
         * From a tree(protocol) get encoded result.
         */
        public static string encodeMessageUsingProtocol(SecProtocolTree<SecProtocolBlock> tree)
        {
            string result = String.Empty;

            result = traverseTree(tree, "");

            return result;
        }        

        /*
         * From a node get the doWork() result of each child's block and concatenate the results
         */
        public static String traverseTree(SecProtocolTree<SecProtocolBlock> node, String data)
        {
            String result = data;
            //Console.WriteLine("traversing: " + node.children.Count);
            //If node has children
            if (node.children.Count != 0)
            {
                List<string> results = new List<string>();
                //Get results from children
                foreach (SecProtocolTree<SecProtocolBlock> child in node.children)
                {
                    results.Add(traverseTree(child, result));                    
                }

                //Concatenate results from children.
                for (int i = 0; i < results.Count; i++)
                {
                    if (i == results.Count - 1)
                    {
                        //result += results.ElementAt(i);
                        //if parent is Hash, dont append message size?
                        String msgSize = "" + (results.ElementAt(i).Length * sizeof(Char)).ToString("00000000");
                        result += msgSize + results.ElementAt(i);
                    }
                    else
                    {
                        //Replace "," with appropriate delimiter/sizeofmessage
                        String msgSize = "" + (results.ElementAt(i).Length * sizeof(Char)).ToString("00000000");
                        result += msgSize + results.ElementAt(i);
                    }
                }
            }

            //If we are at root simply return the result.
            if (node.level == 0)
            {
                return result;
            }
            else
            {
                return node.block.doWork(result);                
            }
        }

        //Holds result for decodeMessageUsingTree(), needs to be instantiated before calling the method, and ideally cleared/nulled after use
        public static List<string> decodeResult;

        /*
         * When a message using a certain protocol is received, this can be used to decode.
         */
        public static void decodeMessageUsingTree(string input, SecProtocolTree<SecProtocolBlock> tree)
        {
            //String example = "00000002A00000090{00000004Na00000048{00000026H(00000004Na)}K}K";


            int MsgLength = input.Length;
            int currentlyRead = 0;


            int headerCount = 0;
            List<string> decoded = new List<string>();
            

            //Check the current string for series headers and data sections
            while (currentlyRead < MsgLength)
            {
                int len = 0;
                String subset = input.Substring(currentlyRead, SecProtocolTreeTEST.MSG_LEN);
                if (Int32.TryParse(subset, out len))
                {
                    //Get length in number of bytes                
                    len = len / sizeof(Char);
                    currentlyRead += SecProtocolTreeTEST.MSG_LEN;
                    headerCount++;
                }

                //if the data section is reversible, reverse it (DecryptionBlock)
                if (tree.getChild(headerCount - 1).block.isReversible)
                {
                    decoded.Add(tree.getChild(headerCount - 1).block.reverseWork(input.Substring(currentlyRead, len)));
                    //Console.WriteLine("Decoded Section: " + tree.getChild(headerCount - 1).block.reverseWork(input.Substring(currentlyRead, len)));
                }
                //Otherwise it is a final piece of info, such as a DataBlock or a HashBlock.
                else
                {
                    decoded.Add("");
                    //Console.WriteLine("Final: " + input.Substring(currentlyRead, len));
                    decodeResult.Add(input.Substring(currentlyRead, len));
                }

                currentlyRead += len;
            }

            //For each reversible child element keep looking for more data to decode.
            for (int i = 0; i < headerCount; i++)
            {
                if (tree.getChild(i).block.isReversible)
                {
                    decodeMessageUsingTree(decoded.ElementAt(i), tree.getChild(i));
                }
            }
        }
    }

    class SecProtocolTreeTEST
    {
        public SecProtocolTree<SecProtocolBlock> tree;
        public SecProtocolTreeTEST()
        {
            this.tree = new SecProtocolTree<SecProtocolBlock>(new SecProtocolBlockData(""));
        }

        public void populateTree()
        {
            this.tree.addChild(new SecProtocolBlockData("identA"));
            this.tree.getChild(0).addChild(new SecProtocolBlockData("child of A"));
            this.tree.addChild(new SecProtocolBlockHashing(SecProtocolBlockHashing.HASH_SHA256));
            this.tree.getChild(1).addChild(new SecProtocolBlockData("My secret message"));

            //recursively do work from deepest nodes..
            String myMsg = this.tree.getChild(1).getChild(0).block.doWork("");            

            //Everytime we go a lvl closer to root concatenate all child nodes together.
            String concat = this.tree.getChild(0).block.doWork("") + "," + this.tree.getChild(1).block.doWork(myMsg);

            //stop at root.
            //output string:
            Console.WriteLine("Output:" + concat);

            Console.WriteLine("Levels: " + this.tree.level + "," + this.tree.getChild(0).level + "," +
                this.tree.getChild(1).level + "," + this.tree.getChild(1).getChild(0).level);

            //reverseTree(concat);
            Console.WriteLine("Trav tree:\n" + traverseTree());
        }

        public void populateTree2()
        {
            this.tree.addChild(new SecProtocolBlockData("A"));
            this.tree.addChild(new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, "0123456789ABCDEF0123456789ABCDEF"));
            this.tree.getChild(1).addChild(new SecProtocolBlockData("Na"));
            this.tree.getChild(1).addChild(new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, "0123456789ABCDEF0123456789ABCDEF"));
            this.tree.getChild(1).getChild(1).addChild(new SecProtocolBlockHashing(SecProtocolBlockHashing.HASH_SHA256));
            this.tree.getChild(1).getChild(1).getChild(0).addChild(new SecProtocolBlockData("Na"));

            Console.WriteLine("Trav tree:\n" + traverseTree());
        }

        public void decodeMessage()
        {
            this.tree.addChild(new SecProtocolBlockData("A"));
            this.tree.addChild(new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, "0123456789ABCDEF0123456789ABCDEF"));
            this.tree.getChild(1).addChild(new SecProtocolBlockData("Na"));
            this.tree.getChild(1).addChild(new SecProtocolBlockEncryption(SecProtocolBlockEncryption.ENC_AES, "0123456789ABCDEF0123456789ABCDEF"));
            this.tree.getChild(1).getChild(1).addChild(new SecProtocolBlockHashing(SecProtocolBlockHashing.HASH_SHA256));
            this.tree.getChild(1).getChild(1).getChild(0).addChild(new SecProtocolBlockData("Na"));
            
            String message = "A,{Na,{H(Na)}K}K";
            message = "00000002A00000090{00000004Na00000048{00000026H(00000004Na)}K}K";

            //uses aes
            message = "00000002A00000176PPQeH1pPUGyEVbT9xnxWcFPABOa4mNQZfFqgWlfQuncgjbQv1l1GmRd4+OsbXRG/uRtHhIFFsaWxObxWvUTJXQ==";

            decodeMessageUsingTree(message, this.tree);

        }

        public String traverseTree()//List<int> endPoint)
        {
            String result = String.Empty;

            result = traverseTree(this.tree, "");
            
            return result;
        }
        
        /*
         * From a node get the doWork() result of each child's block and concatenate the results
         */ 
        public String traverseTree(SecProtocolTree<SecProtocolBlock> node, String data)
        {
            String result = data;

            //If node has children
            if (node.children.Count != 0)
            {
                List<string> results = new List<string>();
                //Get results from children
                foreach (SecProtocolTree<SecProtocolBlock> child in node.children)
                {
                    results.Add(traverseTree(child, result));
                }

                //Concatenate results from children.
                for (int i = 0; i < results.Count; i++)
                {
                    if (i == results.Count - 1)
                    {
                        //result += results.ElementAt(i);
                        String msgSize = "" + (results.ElementAt(i).Length * sizeof(Char)).ToString("00000000");
                        result += msgSize + results.ElementAt(i);
                    }
                    else
                    {
                        //Replace "," with appropriate delimiter/sizeofmessage
                        String msgSize = "" + (results.ElementAt(i).Length * sizeof(Char)).ToString("00000000");
                        result += msgSize + results.ElementAt(i);
                    }
                }
            }

            //If we are at root simply return the result.
            if (node.level == 0)
            {
                return result;
            }
            else
            {
                return node.block.doWork(result);
            }
        }

        public static int MSG_LEN = 8;

        /*
         * When a message using a certain protocol is received, this can be used to decode.
         */
        public void decodeMessageUsingTree(string input, SecProtocolTree<SecProtocolBlock> tree)
        {
            //String example = "00000002A00000090{00000004Na00000048{00000026H(00000004Na)}K}K";
                 

            int MsgLength = input.Length;
            int currentlyRead = 0;

            
            int headerCount = 0;
            List<string> decoded = new List<string>();

            //Check the current string for series headers and data sections
            while (currentlyRead < MsgLength)
            {
                int len = 0;
                String subset = input.Substring(currentlyRead, SecProtocolTreeTEST.MSG_LEN);
                if (Int32.TryParse(subset, out len))
                {
                    //Get length in number of bytes                
                    len = len / sizeof(Char);
                    currentlyRead += SecProtocolTreeTEST.MSG_LEN;
                    headerCount++;
                }

                //if the data section is reversible, reverse it (DecryptionBlock)
                if (tree.getChild(headerCount - 1).block.isReversible)
                {
                    decoded.Add(tree.getChild(headerCount - 1).block.reverseWork(input.Substring(currentlyRead, len)));
                    Console.WriteLine("Decoded Section: " + tree.getChild(headerCount - 1).block.reverseWork(input.Substring(currentlyRead, len)));
                }
                //Otherwise it is a final piece of info, such as a DataBlock or a HashBlock.
                else
                {
                    decoded.Add("");
                    Console.WriteLine("Final: " + input.Substring(currentlyRead, len));
                }

                currentlyRead += len;
            }

            //For each reversible child element keep looking for more data to decode.
            for (int i = 0; i < headerCount; i++)
            {
                if (tree.getChild(i).block.isReversible)
                {
                    decodeMessageUsingTree(decoded.ElementAt(i), tree.getChild(i));
                }
            }            
        }



        public void reverseTree(string data)
        {
            String[] splitData = data.Split(',');
            String res1 = this.tree.getChild(0).block.reverseWork(splitData[0]);

            String res2 = this.tree.getChild(1).block.reverseWork(splitData[1]);

            String res3 = this.tree.getChild(1).getChild(0).block.reverseWork(res2);

            Console.WriteLine(res1 +"," + res2 + "," + res3);
        }

        public void parseExpression()
        {

        }
    }
}
