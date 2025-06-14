using modprocessor.Commands.Compress;
using System.CommandLine;

namespace modprocessor
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("modprocessor is a command line tool for processing protracker mod files.");
            CompressCommand.AddCommand(rootCommand);
            DecompressCommand.AddCommand(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
