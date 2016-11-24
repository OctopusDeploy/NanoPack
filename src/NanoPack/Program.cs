﻿using System;
using System.Diagnostics;
using Microsoft.Extensions.CommandLineUtils;

namespace NanoPack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: true);

            var vhdPath = app.Option("-o | --output <folder>", "The folder the output VHD will be created (optional). If not supplied a /Nanopacked folder will be created one level up from your inputpath folder", CommandOptionType.SingleValue);
            var inputPath = app.Option("-i | --inputpath <folder>", "The path to the application folder to be packaged (required)", CommandOptionType.SingleValue);
            var nanoServerPath = app.Option("-f | --nanoServerFilesPath <folder>", "The path to your nano server installation files (required)", CommandOptionType.SingleValue);
            var publishPath = app.Option("-a | --publishPath <folder>", "The path within the VHD to copy your application to (optional). Default is PublishedApp", CommandOptionType.SingleValue);
            var exeName = app.Option("-e | --executableName <name>", "The name of the executable file in the publish folder to extract version information from (optional, if not provided NanoPack will scan the target folder for an executable)", CommandOptionType.SingleValue);
            var port = app.Option("-p | --port", "The port IIS will serve your app on (optional). Default is 80", CommandOptionType.SingleValue);
            var machine = app.Option("-n| --computerName", "The name of your NanoServer (optional). Default is NanoServer", CommandOptionType.SingleValue);
            var package = app.Option("--package", "Package the VHD into a Zip file with the app version in its name, ready to be pushed to Octopus", CommandOptionType.NoValue);
            var keepPackagedVhd = app.Option("--keepPackagedVhd", "Do not delete the original VHD after it has been packaged with the --package option", CommandOptionType.NoValue);
            var keepUploadedZip = app.Option("--keepUploadedZip", "Do not delete the zipped VHD after it has been uploaded to Octopus", CommandOptionType.NoValue);
            var octopusUrl = app.Option("-u | --octopusUrl", "The URL of your Octopus server. If provided, and --package is set, NanoPack will push the packaged VHD to the built in package feed (optional)", CommandOptionType.SingleValue);
            var apiKey = app.Option("-k | --apiKey", "An Octopus API key with the XXX permission (required if --octopusUrl is set)", CommandOptionType.SingleValue);
            var password = app.Option("--password", "The Administrator password of the resulting NanoServer image (optional). Default is P@ssw0rd", CommandOptionType.SingleValue);
            var vhdx = app.Option("-x | --vhdx", "Build a VHDX rather than a VHD", CommandOptionType.NoValue);
            var edition = app.Option("--edition", "The windows server edition. Standard or Datacenter", CommandOptionType.SingleValue);


            app.HelpOption("-? | -h | --help");

            app.OnExecute(() =>
            {
                if (inputPath.HasValue() 
                    && nanoServerPath.HasValue())
                {
                    var task = new BuildVhdTask(inputPath.Value(), nanoServerPath.Value());

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
                    task.Package = package.HasValue();
                    task.KeepPackagedVhd = keepPackagedVhd.HasValue();
                    task.KeepUploadedZip = keepUploadedZip.HasValue();
                    task.OctopusUrl = octopusUrl.Value();
                    task.ApiKey = apiKey.Value();
                    if (password.HasValue())
                    {
                        task.Password = password.Value();
                    }
                    task.Vhdx = vhdx.HasValue();

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

            app.Execute(args);
        }
    }
}
