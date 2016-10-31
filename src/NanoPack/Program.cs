using System;
using System.Diagnostics;
using Microsoft.Extensions.CommandLineUtils;

namespace NanoPack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: true);

            var vhdPath = app.Option("-o | --output <path>", "The folder the output VHD will be created in (required)", CommandOptionType.SingleValue);
            var appName = app.Option("-n | --name <name>", "The name of your application (required)", CommandOptionType.SingleValue);
            var publishPath = app.Option("-i | --inputpath <path>", "The path to the application folder to be packaged (required)", CommandOptionType.SingleValue);
            var nanoServerPath = app.Option("-f | --nanoServerFilesPath <path>", "The path to your nano server installation files (required)", CommandOptionType.SingleValue);
            var port = app.Option("-p | --port", "The port IIS will serve your app on", CommandOptionType.SingleValue);

            app.HelpOption("-? | -h | --help");

            app.OnExecute(() =>
            {
                int parsedPort;
                if (vhdPath.HasValue()
                    && appName.HasValue()
                    && publishPath.HasValue() 
                    && nanoServerPath.HasValue() 
                    && port.HasValue() 
                    && int.TryParse(port.Value(), out parsedPort))
                {
                    new BuildVhdTask().Generate(
                        vhdPath.Value(),
                        appName.Value(),
                        publishPath.Value(),
                        nanoServerPath.Value(),
                        parsedPort);
                }
                else
                {
                    app.ShowHelp();
                }

                if (Debugger.IsAttached)
                    Console.ReadKey();

                return 0;
            });

            app.Execute(args);
        }
    }
}
