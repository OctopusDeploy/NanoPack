using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using NanoPack;
using Octostache;
using System.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace NanoPack
{
    internal class BuildVhdTask : AbstractTask
    {
        // Placeholder to be roughly the shape of a Microsoft Build Task if we need to port
        //public string VhdDestinationFolder { get; set; }
        //public string AppName { get; set; }
        //public string PublishedAppFolder { get; set; }
        //public string NanoServerInstallFiles { get; set; }
        //public string ExeName { get; set; }
        //public int Port { get; set; }
        //public bool Package { get; set; }
        //public bool KeepPackagedVhd { get; set; }
        //public string OctopusUrl { get; set; }
        //public string ApiKey { get; set; }

        public override bool Execute()
        {
            return false; //Generate(VhdDestinationFolder, AppName, NanoServerInstallFiles, ExeName, Port, Package, KeepPackagedVhd, OctopusUrl, ApiKey);
        }

        public bool Generate(string vhdDestinationFolder, string inputFolder, string nanoServerInstallFiles, string exeName, int port, bool package, bool keepPackagedVhd, string octopusUrl, string apiKey, string publishFolder, string password)
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

            var exePath = FindExe(exeName, inputFolder);
            var appName = Path.GetFileNameWithoutExtension(exePath);
            var vhdFilePath = Path.Combine(vhdDestinationFolder, appName + ".vhd");
            var working = PrepareWorkingDirectory();
            if (string.IsNullOrWhiteSpace(publishFolder))
            {
                publishFolder = "PublishedApps\\appName";
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                password = "P@ssw0rd";
            }

            var variables = new VariableDictionary();
            variables.Set("appName", appName);
            variables.Set("port", port.ToString());
            variables.Set("vhd", vhdFilePath);
            variables.Set("inputFolder", inputFolder);
            variables.Set("machineName", "aspnetcore");
            variables.Set("nanoserverFolder", nanoServerInstallFiles);
            variables.Set("edition", "Datacenter");
            variables.Set("vmpassword", password);
            variables.Set("publishFolder", publishFolder);

            Substitute(Path.Combine(working, "first-boot.ps1"), variables);
            Substitute(Path.Combine(working, "build-vhd.ps1"), variables);

            PowerShell.RunFile(working, "build-vhd.ps1");

            LogMessage($"VHD created at {vhdFilePath}");

            if (package)
            {
                Package(vhdDestinationFolder, keepPackagedVhd, octopusUrl, apiKey, exePath, appName, vhdFilePath);
            }

            LogMessage("Finished");

            return true;
        }

        private void Package(string vhdDestinationFolder, bool keepPackagedVhd, string octopusUrl, string apiKey, string exePath,
            string appName, string vhdFilePath)
        {
            LogMessage($"Packing VHD");
            var version = GetVersionInformation(exePath);
            var zipPath = Path.Combine(vhdDestinationFolder, $"{appName}.{version}.zip");

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var arch = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                arch.CreateEntryFromFile(vhdFilePath, Path.GetFileName(vhdFilePath), CompressionLevel.Optimal);
            }

            LogMessage($"VHD packaged to {zipPath}");

            if (!keepPackagedVhd)
            {
                LogMessage("Deleting VHD, use --keepPackagedVhd to keep");
                File.Delete(vhdFilePath);
            }

            if (!string.IsNullOrWhiteSpace(octopusUrl) && !string.IsNullOrWhiteSpace(apiKey))
            {
                OctoPusher.Upload(octopusUrl, apiKey, zipPath, LogMessage);
            }
        }

        private static string FindExe(string exeName, string publishedAppFolder)
        {
            string path;
            if (string.IsNullOrWhiteSpace(exeName))
            {
                var paths = Directory.GetFiles(publishedAppFolder, "*.exe").ToArray();
                if (paths.Length == 1)
                {
                    path = paths[0];
                }
                else if (paths.Length == 0)
                {
                    throw new Exception($"No .exe found in the Published App Folder at {publishedAppFolder}. Application must be a self-contained ASP.NET Core app.");
                }
                else
                {
                    throw new Exception($"More than one .exe found in the Published App Folder at {publishedAppFolder}. Please specify which .exe to inspect for version and naming information with the --exeName parameter.");
                }
            }
            else
            {
                path = Path.Combine(publishedAppFolder, exeName);
                if (!File.Exists(path))
                {
                    throw new Exception($"No .exe found at {path} please check your --exePath setting.");
                }
            }

            return path;
        }

        private string GetVersionInformation(string exePath)
        {
            // .NET Core doesn't seem to set the version on a published exe as of 1.0.1, only it's underlying dll.
            var dllPath = exePath.Remove(exePath.Length - 3) + "dll";
            var checkPath = File.Exists(dllPath) ? dllPath : exePath;
            LogMessage($"Extracting version information from {checkPath}");
            var version = FileVersionInfo.GetVersionInfo(checkPath);
            return $"{version.ProductMajorPart}.{version.ProductMinorPart}.{version.ProductBuildPart}";
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
            var assembly = typeof(BuildVhdTask).GetTypeInfo().Assembly;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                LogMessage($"Extracting {resourceName} to {tempDir}");
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var file = new FileStream(Path.Combine(tempDir, resourceName), FileMode.Create))
                {
                    for (var i = 0; i < stream.Length; i++)
                    {
                        file.WriteByte((byte) stream.ReadByte());
                    }
                }
            }
            return tempDir;
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