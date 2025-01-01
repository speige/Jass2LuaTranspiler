namespace Jass2LuaTranspiler.Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var options = new Transpiler.Options();

            var (jassScript, outputFilePath) = ParseInputArguments(args);

            var result = Transpiler.parseScript(jassScript, options);

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
                Console.WriteLine("Please paste JASS script to convert. (Send Ctrl+Z on Windows / Ctrl+D on Unix to finish):");

                var jassScript = Console.In.ReadToEnd();
                return (jassScript, null);
            }
        }
    }
}
