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
            var inputPath = app.Option("-i | --inputpath <path>", "The path to the application folder to be packaged (required)", CommandOptionType.SingleValue);
            var nanoServerPath = app.Option("-f | --nanoServerFilesPath <path>", "The path to your nano server installation files (required)", CommandOptionType.SingleValue);
            var publishPath = app.Option("-a | --publishPath <path>", "The path within the VHD to copy your application to (optional). Default is PublishedApps\\YourAppName", CommandOptionType.SingleValue);
            var exeName = app.Option("-e | --executableName <name>", "The name of the executable file in the publish folder to extract version information from (optional, if not provided NanoPack will scan the target folder for an executable)", CommandOptionType.SingleValue);
            var port = app.Option("-p | --port", "The port IIS will serve your app on", CommandOptionType.SingleValue);
            var package = app.Option("--package", "Package the VHD into a Zip file with the app version in its name, ready to be pushed to Octopus", CommandOptionType.NoValue);
            var keepPackagedVhd = app.Option("--keepPackagedVhd", "Do not delete the original VHD after it has been packaged with the --package option", CommandOptionType.NoValue);
            var octopusUrl = app.Option("-u | --octopusUrl", "The URL of your Octopus server. If provided, and --package is set, NanoPack will push the packaged VHD to the built in package feed (optional)", CommandOptionType.SingleValue);
            var apiKey = app.Option("-k | --apiKey", "An Octopus API key with the XXX permission (required if --octopusUrl is set)", CommandOptionType.SingleValue);
            var password = app.Option("--password", "The Administrator password of the resulting NanoServer image (Optional). Default is P@ssw0rd", CommandOptionType.SingleValue);

            app.HelpOption("-? | -h | --help");

            app.OnExecute(() =>
            {
                int parsedPort;
                if (vhdPath.HasValue()
                    && inputPath.HasValue() 
                    && nanoServerPath.HasValue() 
                    && port.HasValue() 
                    && int.TryParse(port.Value(), out parsedPort))
                {
                    new BuildVhdTask().Generate(
                        vhdPath.Value(),
                        inputPath.Value(),
                        nanoServerPath.Value(),
                        exeName.Value(),
                        parsedPort,
                        package.HasValue(),
                        keepPackagedVhd.HasValue(),
                        octopusUrl.Value(),
                        apiKey.Value(),
                        publishPath.Value(),
                        password.Value()
                        );
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
