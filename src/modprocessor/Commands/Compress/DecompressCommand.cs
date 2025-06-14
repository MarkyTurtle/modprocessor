using modprocessor.libs;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace modprocessor.Commands.Compress
{

    public static class DecompressCommand
    {
        public static void AddCommand(Command parentCommand)
        {

            var cmdArgument = new Argument<string>("filename", "File to Decompress.");


            var command = new Command("decompress", "Decompress sample data (4-bit delta)")
            {
                cmdArgument
            };

            parentCommand.AddCommand(command);


            command.SetHandler((cmdArgument) =>
            {
                CommandHandler(cmdArgument);
            },
            cmdArgument);

        }

        private static void CommandHandler(string modFilePath)
        {

            if (!File.Exists(modFilePath))
            {
                Console.WriteLine("File does not exist!");
                return;
            }

            byte[] modBytes = File.ReadAllBytes(modFilePath);
            var protrackerModule = ProtrackerModule.Create(modBytes);

            protrackerModule.DepackSamples();

            File.WriteAllBytes(modFilePath + ".decompress.mod", protrackerModule.ModuleData);


        }
    }
}