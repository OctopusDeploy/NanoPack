using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.CommandLineUtils;

namespace NanoPack
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: true);

            var vhdPath =         app.Option("-o | --outputPath <folder>", "The folder the output VHD will be created (optional). If not supplied a /Nanopacked folder will be created one level up from your inputpath folder", CommandOptionType.SingleValue);
            var inputPath =       app.Option("-i | --inputPath <folder>", "The path to the application folder to be packaged (required)", CommandOptionType.SingleValue);
            var nanoServerPath =  app.Option("-m | --mediaPath <folder>", "The path to your nano server installation files (required)", CommandOptionType.SingleValue);
            var maxSize =         app.Option("   | --maxSize <size>", "The maximum size of the VHD (optional). Default is 4GB", CommandOptionType.SingleValue);
            var publishPath =     app.Option("   | --publishPath <folder>", "The path within the VHD to copy your application to (optional). Default is /PublishedApp", CommandOptionType.SingleValue);
            var exeName =         app.Option("-e | --executableName <name>", "The name of the executable file in the publish folder to extract version information from (optional, if not provided NanoPack will scan the target folder for an executable)", CommandOptionType.SingleValue);
            var port =            app.Option("-p | --port", "The port IIS will serve your app on (optional). Default is 80", CommandOptionType.SingleValue);
            var machine =         app.Option("-n | --computerName", "The name of your NanoServer (optional). Default is NanoServer", CommandOptionType.SingleValue);
            var package =         app.Option("     --package", "Package the VHD into a Zip file with the app version in its name, ready to be pushed to Octopus", CommandOptionType.NoValue);
            var keepPackagedVhd = app.Option("     --keepPackagedVhd", "Do not delete the original VHD after it has been packaged with the --package option", CommandOptionType.NoValue);
            var keepUploadedZip = app.Option("     --keepUploadedZip", "Do not delete the zipped VHD after it has been uploaded to Octopus", CommandOptionType.NoValue);
            var octopusUrl =      app.Option("-u | --octopusUrl", "The URL of your Octopus server. If provided, and --package is set, NanoPack will push the packaged VHD to the built in package feed (optional)", CommandOptionType.SingleValue);
            var apiKey =          app.Option("-k | --apiKey", "An Octopus API key with the XXX permission (required if --octopusUrl is set)", CommandOptionType.SingleValue);
            var password =        app.Option("     --password", "The Administrator password of the resulting NanoServer image (optional). Default is P@ssw0rd", CommandOptionType.SingleValue);
            var vhdx =            app.Option("-x | --vhdx", "Build a VHDX rather than a VHD", CommandOptionType.NoValue);
            var edition =         app.Option("     --edition", "The windows server edition. Standard or Datacenter", CommandOptionType.SingleValue);
            var scripts =         app.Option("-s | --firstbootscript", "Path to a PowerShell script that will be copied to the VHD and run on its first boot. Multiple allowed.", CommandOptionType.MultipleValue);
            var copyPath =        app.Option("   | --copyPath", "Copy files to your VHD. This arguement is passed through to the  New-NanoServerImage cmdlet and must be a string that evals to a PowerShell array or hash map.", CommandOptionType.SingleValue);
            var additional =      app.Option("-a | --additional", "Extra options to pass to New-NanoServerImage, for example: -a \"-Ipv4Address \\\"172.21.22.101\\\"\". Multiple allowed.", CommandOptionType.MultipleValue);

            app.HelpOption("-? | --help");

            app.OnExecute(() =>
            {
                if (inputPath.HasValue() && nanoServerPath.HasValue())
                {
                    var packager = new Packager(new Pusher(octopusUrl.Value(), apiKey.Value()), package.HasValue(), keepPackagedVhd.HasValue(), keepUploadedZip.HasValue());
                    var task = new BuildVhdTask(new PowerShell(), packager, inputPath.Value(), nanoServerPath.Value());

                    if (port.HasValue())
                    {
                        int parsedPort;
                        if (int.TryParse(port.Value(), out parsedPort))
                        {
                            task.Port = parsedPort;
                        }
                        else
                        {
                            throw new ArgumentException($"Unable to parse value {port.Value()} to an integer");
                        }
                    }
                    if (vhdPath.HasValue())
                    {
                        task.VhdDestinationFolder = vhdPath.Value();
                    }
                    if (publishPath.HasValue())
                    {
                        task.PublishFolder = publishPath.Value();
                    }
                    if (exeName.HasValue())
                    {
                        task.ExeName = exeName.Value();
                    }
                    if (machine.HasValue())
                    {
                        task.MachineName = machine.Value();
                    }
                    if (edition.HasValue())
                    {
                        task.SetEdition(edition.Value());
                    }
                    if (maxSize.HasValue())
                    {
                        task.MaxSize = maxSize.Value();
                    }
                    if (password.HasValue())
                    {
                        task.Password = password.Value();
                    }
                    task.Vhdx = vhdx.HasValue();

                    var foundScripts = new List<string>();
                    foreach (var scriptPath in scripts.Values)
                    {
                        var path = Path.GetFullPath(scriptPath);
                        if (!File.Exists(path))
                        {
                            throw new ArgumentException($"No file found at {path}, check your --firstbootscript parameters");
                        }
                        foundScripts.Add(scriptPath);
                    }
                    task.ScriptPaths = string.Join(";", foundScripts);

                    task.CopyPath = "\"" + copyPath.Value().Replace("\"", "\"\"") + "\"";
                    task.Additional = string.Join(" ", additional.Values);

                    task.Generate();
                }
                else
                {
                    app.ShowHelp();
                }

                if (Debugger.IsAttached)
                    Console.ReadKey();

                return 0;
            });

            return app.Execute(args);
        }
    }
}
