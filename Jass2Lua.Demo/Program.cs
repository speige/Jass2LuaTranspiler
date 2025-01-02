namespace Jass2Lua.Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var (jassScript, outputFilePath) = ParseInputArguments(args);

            var transpiler = new Jass2LuaTranspiler();
            var result = transpiler.Transpile(jassScript);

            if (string.IsNullOrEmpty(outputFilePath))
            {
                Console.Clear();
                Console.Write(result);
            }
            else
            {
                File.WriteAllText(outputFilePath, result);
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

                var jassScript = File.ReadAllText(inputFilePath);

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
