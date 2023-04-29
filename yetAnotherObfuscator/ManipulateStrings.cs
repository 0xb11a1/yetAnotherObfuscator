using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace yetAnotherObfuscator
{
    class ManipulateStrings
    {
        static string randomEncryptionKey = GetRandomString(new Random().Next(40, 60));

        public static string GetRandomString(int size)
        {
            char[] chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }


        public static void PerformStringEncryption(ModuleDef moduleDef)
        {
            ModuleDef typeModule = ModuleDefMD.Load(typeof(ManipulateStrings).Module);
            Console.WriteLine("[+] Injecting the decryption method");

            foreach (TypeDef type in typeModule.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.Name == "DecryptString")
                    {
                        method.DeclaringType = null;
                        method.Name = GetRandomString(new Random().Next(12, 24));
                        method.Parameters[0].Name = "\u0011";

                        moduleDef.GlobalType.Methods.Add(method);

                        foreach (Instruction i in method.Body.Instructions)
                        {
                            if (i.ToString().Contains("DefaultKey"))
                            {
                                i.Operand = randomEncryptionKey;
                            }
                        }

                        Console.WriteLine("[+] Encrypting all strings with encryption key: " + randomEncryptionKey);

                        foreach (dnlib.DotNet.TypeDef typedef in moduleDef.GetTypes().ToList())
                        {
                            if (!typedef.HasMethods)
                                continue;

                            foreach (dnlib.DotNet.MethodDef typeMethod in typedef.Methods)
                            {
                                if (typeMethod.Body == null)
                                    continue;
                                if (typeMethod.Name != method.Name)
                                {
                                    foreach (Instruction instr in typeMethod.Body.Instructions.ToList())
                                    {
                                        if (instr.OpCode == OpCodes.Ldstr)
                                        {
                                            int instrIndex = typeMethod.Body.Instructions.IndexOf(instr);

                                            typeMethod.Body.Instructions[instrIndex].Operand = EncryptString(typeMethod.Body.Instructions[instrIndex].Operand.ToString(), randomEncryptionKey);
                                            typeMethod.Body.Instructions.Insert(instrIndex + 1, new Instruction(OpCodes.Call, method));
                                        }
                                    }
                                    typeMethod.Body.UpdateInstructionOffsets();
                                    typeMethod.Body.OptimizeBranches();
                                    typeMethod.Body.SimplifyBranches();
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        public static string EncryptString(string plaintext, string key)
        {
            byte[] encryptedArray = UTF8Encoding.UTF8.GetBytes(plaintext);
            byte[] encryptionKey = new MD5CryptoServiceProvider().ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

            var tripleDES = new TripleDESCryptoServiceProvider();

            tripleDES.Key = encryptionKey;
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;

            var cryptoTransform = tripleDES.CreateEncryptor();

            byte[] result = cryptoTransform.TransformFinalBlock(encryptedArray, 0, encryptedArray.Length);
            tripleDES.Clear();

            return Convert.ToBase64String(result, 0, result.Length);
        }
        public static string DecryptString(string ciphertext)
        {
            byte[] decodedData = Convert.FromBase64String(ciphertext);
            byte[] encryptionKey = new MD5CryptoServiceProvider().ComputeHash(UTF8Encoding.UTF8.GetBytes("DefaultKey"));

            var tripleDES = new TripleDESCryptoServiceProvider();

            tripleDES.Key = encryptionKey;
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;

            var cryptoTransform = tripleDES.CreateDecryptor();

            byte[] result = cryptoTransform.TransformFinalBlock(decodedData, 0, decodedData.Length);
            tripleDES.Clear();

            return Encoding.UTF8.GetString(result);
        }
    }
}
