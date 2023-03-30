using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yetAnotherObfuscator
{
    class ManipulateStrings
    {
        static MethodDefUser decrypt_methodDefUser; 
        public static void Fire(ModuleDefMD moduleDef){

            Console.WriteLine("[+] Injecting The decryption method");
            Create_decryption(moduleDef);

            Console.WriteLine("[+] Encrypting strings");
            Insert_call(moduleDef);

        }
        public static void Insert_call(ModuleDefMD moduleDef)
        {
            IEnumerable<TypeDef> types = moduleDef.GetTypes();

            foreach (dnlib.DotNet.TypeDef type in types.ToList()){
                if (!type.HasMethods)
                    continue;
                
                foreach (dnlib.DotNet.MethodDef method in type.Methods){
                    if (method.Body == null)
                        continue;
                    foreach (Instruction instr in method.Body.Instructions.ToList()){
                        if (instr.OpCode == OpCodes.Ldstr){
                            int instrIndex = method.Body.Instructions.IndexOf(instr);

                            method.Body.Instructions[instrIndex].Operand = EncryptString(method.Body.Instructions[instrIndex].Operand.ToString());
                            method.Body.Instructions.Insert(instrIndex + 1, new Instruction(OpCodes.Call, decrypt_methodDefUser));

                            // Console.WriteLine(instr.Operand);
                        }
                    }
                    method.Body.UpdateInstructionOffsets();
                    method.Body.OptimizeBranches();
                    method.Body.SimplifyBranches();
                }


            }
        }
        public static void Create_decryption(ModuleDefMD moduleDef)
        {
            decrypt_methodDefUser = new MethodDefUser("0xb1a11", MethodSig.CreateStatic(moduleDef.CorLibTypes.String, moduleDef.CorLibTypes.String), dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed, dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.HideBySig | dnlib.DotNet.MethodAttributes.ReuseSlot);
            decrypt_methodDefUser.Body = new CilBody();
            moduleDef.GlobalType.Methods.Add(decrypt_methodDefUser);

            decrypt_methodDefUser.Body.Instructions.Add(OpCodes.Nop.ToInstruction());
            decrypt_methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(moduleDef.Import(typeof(System.Text.Encoding).GetMethod("get_UTF8", new Type[] { }))));
            decrypt_methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            decrypt_methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(moduleDef.Import(typeof(System.Convert).GetMethod("FromBase64String", new Type[] { typeof(string) }))));
            decrypt_methodDefUser.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(moduleDef.Import(typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));
            decrypt_methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        }
        public static string EncryptString(string str) {
            // not secure or random but i will leave it for now because it's easier to debug
            var result = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
            return result;
        }
    }
}
