using System.Diagnostics;
using System.Text;

namespace Jass2Lua.Demo
{
    public static class Program
    {
        //NOTE: ISO-8859-1 is a 1-to-1 match of byte to char. Important to avoid corrupting unicode characters (international language symbols).
        public static void Main(string[] args)
        {
            var (jassScript, outputFilePath) = ParseInputArguments(args);

            var transpiler = new Jass2LuaTranspiler();
            var result = transpiler.Transpile(jassScript, out var warnings);

            if (string.IsNullOrEmpty(outputFilePath))
            {
                Console.Clear();
                Console.Write(result);
            }
            else
            {
                File.WriteAllText(outputFilePath, result, Encoding.GetEncoding("ISO-8859-1"));
                Console.WriteLine($"Output saved to '{outputFilePath}'");
            }
        }

        private static (string JassScript, string OutputFilePath) ParseInputArguments(string[] args)
        {
            if (args.Length > 0)
            {
                var inputFilePath = args[0];

                if (!File.Exists(inputFilePath))
                {
                    throw new FileNotFoundException($"Input file '{inputFilePath}' not found.");
                }

                var jassScript = File.ReadAllText(inputFilePath, Encoding.GetEncoding("ISO-8859-1"));

                var outputFilePath = args.Length > 1
                    ? args[1]
                    : Path.ChangeExtension(inputFilePath, ".lua");

                return (jassScript, outputFilePath);
            }
            else
            {
                Console.WriteLine("Usage: Jass2Lua <inputFilePath> [outputFilePath]");
                Console.WriteLine();
                Console.WriteLine("No options specified. Reading from standard input instead.");
                Console.WriteLine("Please paste JASS script to convert and press enter. (Afterwards, Press Ctrl+Z on Windows / Ctrl+D on Unix and enter to run):");

                var jassScript = Console.In.ReadToEnd();
                return (jassScript, null);
            }
        }
    }
}
