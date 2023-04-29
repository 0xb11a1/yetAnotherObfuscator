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
            HelpPage();

            Assembly Default_Assembly;
            Default_Assembly = System.Reflection.Assembly.UnsafeLoadFrom(path);
            ModuleDefMD Module = ModuleDefMD.Load(path);
            AssemblyDef Assembly = Module.Assembly;
            
            ManipulateStrings.PerformStringEncryption(Module);
            ChangeMethodsName.Fire(Module, Default_Assembly);

            Console.WriteLine("[+] Saving the obfuscated file");
            SaveToFile(Module, path);

            Console.WriteLine("[+] Changing exe GUID");
            ChangeGUID(Default_Assembly);

            Console.WriteLine("[+] All done, the obfuscated exe in: " + obf_path);
            Console.Read();
        }
        static void HelpPage() {
            Console.WriteLine(@"  __    __  ______  _____       ");
            Console.WriteLine(@" /\ \  /\ \/\  _  \/\  __`\     ");
            Console.WriteLine(@" \ `\`\\/'/\ \ \L\ \ \ \/\ \    ");
            Console.WriteLine(@"  `\ `\ /'  \ \  __ \ \ \ \ \   ");
            Console.WriteLine(@"    `\ \ \   \ \ \/\ \ \ \_\ \  ");
            Console.WriteLine(@"      \ \_\   \ \_\ \_\ \_____\ ");
            Console.WriteLine(@"       \/_/    \/_/\/_/\/_____/ ");

            do {
                Console.Write("Enter exe path: ");
                path = Console.ReadLine().Trim();
                obf_path = path + "._obf.exe";
                Console.WriteLine("[+] Working on: " + path);

            } while (path.Length == 0 );

        }

        static void SaveToFile(ModuleDefMD moduleDef, string path) {
            ModuleWriterOptions moduleWriterOption = new ModuleWriterOptions(moduleDef);
            moduleWriterOption.MetadataOptions.Flags = moduleWriterOption.MetadataOptions.Flags | MetadataFlags.KeepOldMaxStack;
            moduleWriterOption.Logger = DummyLogger.NoThrowInstance;
            moduleDef.Write(obf_path, moduleWriterOption);
        }

        static public void ChangeGUID(Assembly Default_Assembly) {
            try {
                var curr_GUID = (Default_Assembly.GetCustomAttributes(typeof(GuidAttribute), true).FirstOrDefault() as GuidAttribute).Value;
                byte[] data = File.ReadAllBytes(obf_path);
                byte[] new_guid = Guid.NewGuid().ToByteArray();
            
                List<int> positions = SearchBytePattern(Encoding.ASCII.GetBytes(curr_GUID), data);
                foreach (var item in positions) {
                    using (Stream stream = File.Open(obf_path, FileMode.Open)) {
                        stream.Position = item;
                        stream.Write(new_guid, 0, new_guid.Length);
                    }
                }
            }
            catch {
                Console.WriteLine("[-] GUID not found");
            }
        }
       
        static public List<int> SearchBytePattern(byte[] pattern, byte[] bytes) {
            List<int> positions = new List<int>();
            int patternLength = pattern.Length;
            int totalLength = bytes.Length;
            byte firstMatchByte = pattern[0];
            for (int i = 0; i < totalLength; i++) {
                if (firstMatchByte == bytes[i] && totalLength - i >= patternLength) {
                    byte[] match = new byte[patternLength];
                    Array.Copy(bytes, i, match, 0, patternLength);
                    if (match.SequenceEqual<byte>(pattern)) {
                        positions.Add(i);
                        i += patternLength - 1;
                    }
                }
            }
            return positions;
        }
    }
}
    

