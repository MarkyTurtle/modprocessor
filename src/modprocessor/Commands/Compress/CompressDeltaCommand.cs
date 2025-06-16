using modprocessor.libs;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace modprocessor.Commands.Compress
{
    public static class CompressSDeltaCommand
    {

        public static void AddCommand(Command parentCommand)
        {

            var cmdArgument = new Argument<string>("filename", "File to Compress.");


            var command = new Command("compressdelta", "Compress sample data (4-bit delta)")
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

            protrackerModule.PackSamplesDelta();

            File.WriteAllBytes(modFilePath + ".4del", protrackerModule.ModuleData);


        }

    }

}
