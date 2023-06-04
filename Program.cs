using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace yetAnotherObfuscator
{
    class Program
    {
        public static string path = "";
        public static string obf_path = "";
        static void Main(string[] args){
            HelpPage(args);

            Assembly Default_Assembly;
            Default_Assembly = System.Reflection.Assembly.UnsafeLoadFrom(path);
            ModuleDefMD Module = ModuleDefMD.Load(path);
            
            ManipulateStrings.PerformStringEncryption(Module);
            ChangeMethodsName.Fire(Module, Default_Assembly);

            Console.WriteLine("[+] Changing executable GuidAttribute if it exists");
            ChangeGUID(Module);

            Console.WriteLine("[+] Saving the obfuscated file");
            SaveToFile(Module, path);

            Console.WriteLine("[+] All done, the obfuscated exe in: " + obf_path);
            Console.Read();
        }
        static void HelpPage(string[] args) {
            Console.WriteLine(@"  __    __  ______  _____       ");
            Console.WriteLine(@" /\ \  /\ \/\  _  \/\  __`\     ");
            Console.WriteLine(@" \ `\`\\/'/\ \ \L\ \ \ \/\ \    ");
            Console.WriteLine(@"  `\ `\ /'  \ \  __ \ \ \ \ \   ");
            Console.WriteLine(@"    `\ \ \   \ \ \/\ \ \ \_\ \  ");
            Console.WriteLine(@"      \ \_\   \ \_\ \_\ \_____\ ");
            Console.WriteLine(@"       \/_/    \/_/\/_/\/_____/ ");
            
            if (args.Length == 1) {
                path = args[0].ToString();
            }
            else {
                do {
                    Console.Write("Enter exe path: ");
                    path = Console.ReadLine().Trim();

                } while (path.Length == 0 );
            }
            if (! File.Exists(path)) {
                Console.WriteLine("[-] the path '" + path + "' does not exists");
                System.Environment.Exit(1);
            }
            
            obf_path = path + "._obf.exe";
            Console.WriteLine("[+] Working on: " + path);

        }

        static public void ChangeGUID(ModuleDefMD moduleDef)
        {
            foreach (var customAttribute in moduleDef.Assembly.CustomAttributes)
            {
                if (customAttribute.AttributeType.FullName == "System.Runtime.InteropServices.GuidAttribute")
                {
                    CAArgument arg = customAttribute.ConstructorArguments[0];
                    arg.Value = Guid.NewGuid().ToString();
                    customAttribute.ConstructorArguments[0] = arg;

                    Console.WriteLine("[+] Found and changed GuidAttribute");
                }
            }
        }

        static void SaveToFile(ModuleDefMD moduleDef, string path) {
            ModuleWriterOptions moduleWriterOption = new ModuleWriterOptions(moduleDef);
            moduleWriterOption.MetadataOptions.Flags = moduleWriterOption.MetadataOptions.Flags | MetadataFlags.KeepOldMaxStack;
            moduleWriterOption.Logger = DummyLogger.NoThrowInstance;
            moduleDef.Write(obf_path, moduleWriterOption);
        }
    }
}
    

