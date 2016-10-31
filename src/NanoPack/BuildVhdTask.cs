using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using NanoPack;
using Octostache;
using System.Linq;

namespace NanoPack
{
    internal class BuildVhdTask : AbstractTask
    {
        // Placeholder to be roughly the shape of a Microsoft Build Task if we need to port
        public string VhdDestinationFolder { get; set; }
        public string AppName { get; set; }
        public string PublishedAppFolder { get; set; }
        public string NanoServerInstallFiles { get; set; }
        public int Port { get; set; }

        public override bool Execute()
        {
            return Generate(VhdDestinationFolder, AppName, PublishedAppFolder, NanoServerInstallFiles, Port);
        }

        public bool Generate(string vhdDestinationFolder, string publishedAppName, string publishedAppFolder, string nanoServerInstallFiles, int port)
        {
            if (!Extras.IsAdministrator())
            {
                LogMessage("This application must be run with elevated privileges");
                return false;
            }

            // the New-NanoServerImage cmdlet needs the parent of the NanoServer folder,
            // if we've been given a folder called NanoServer that doesn't have a 
            // NanoServer child folder, try to use the parent.
            var d = new DirectoryInfo(nanoServerInstallFiles);
            if (d.Name.ToLowerInvariant() == "nanoserver" && !Directory.Exists(Path.Combine(nanoServerInstallFiles, "NanoServer")))
            {
                nanoServerInstallFiles = d.Parent.FullName;
            }

            var appName = publishedAppName.Replace(" ", "-");
            var vhdFilePath = Path.Combine(vhdDestinationFolder, appName + ".vhd");
            var working = PrepareWorkingDirectory();

            var variables = new VariableDictionary();
            variables.Set("appName", appName);
            variables.Set("port", port.ToString());
            variables.Set("vhd", vhdFilePath);
            variables.Set("publishPath", publishedAppFolder);
            variables.Set("machineName", "aspnetcore");
            variables.Set("nanoserverFolder", nanoServerInstallFiles);
            variables.Set("edition", "Datacenter");
            variables.Set("vmpassword", "P@ssw0rd");

            Substitute(Path.Combine(working, "first-boot.ps1"), variables);
            Substitute(Path.Combine(working, "build-vhd.ps1"), variables);

            PowerShell.RunFile(working, "build-vhd.ps1");

            LogMessage("Complete");
            return true;
        }

        private static string GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private string PrepareWorkingDirectory()
        {
            var tempDir = GetTemporaryDirectory();
            LogMessage($"Working directory is {tempDir}");
            foreach (var f in Directory.EnumerateFiles(GetToolsDirectory()))
            {
                File.Copy(f, Path.Combine(tempDir, Path.GetFileName(f)));
            }
            return tempDir;
        }

        private static string GetToolsDirectory()
        {
            var currDir = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);
            var tools = Path.Combine(currDir, "tools");
            return tools;
        }

        private static string Substitute(string fileName, VariableDictionary variables)
        {
            string errors;
            var file = File.ReadAllText(fileName);
            var result = variables.Evaluate(file, out errors);
            File.WriteAllText(fileName, result, Encoding.ASCII);
            return errors;
        }
    }
}