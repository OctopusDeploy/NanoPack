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
using System.Linq.Expressions;
using System.Net;
using System.Runtime.InteropServices;
using Sprache;

namespace NanoPack
{
    internal class BuildVhdTask
    {
        public string VhdDestinationFolder { get; set; }
        public string InputFolder { get; set; }
        public string NanoServerInstallFiles { get; set; }
        public int Port { get; set; } = 80;

        public string ExeName { get; set; }
        public string OctopusUrl { get; set; }
        public bool Package { get; set; }
        public bool KeepPackagedVhd { get; set; }
        public bool KeepUploadedZip { get; set; }
        public string ApiKey { get; set; }
        public string PublishFolder { get; set; } = "PublishedApp";
        public string Password { get; set; } = "P@ssw0rd";
        public string MachineName { get; set; } = "NanoServer";
        public EditionType Edition { get; private set; } = EditionType.Datacenter;
        public bool Vhdx { get; set; }
        public string ScriptPaths { get; set; }

        public enum EditionType
        {
            Standard,
            Datacenter
        }

        public void SetEdition(string edition)
        {
            EditionType editionType;
            if (Enum.TryParse(edition, true, out editionType))
            {
                Edition = editionType;
            }
            else
            {
                throw new Exception($"Unable to convert {edition} into a valid edition value of Standard or Datacenter");
            }
        }

        public BuildVhdTask(string inputFolder, string nanoServerInstallFiles)
        {
            InputFolder = inputFolder;
            NanoServerInstallFiles = nanoServerInstallFiles;
        }

        public bool Generate()
        {
            if (!Extras.IsAdministrator())
            {
                LogMessage("This application must be run with elevated privileges");
                return false;
            }

            // the New-NanoServerImage cmdlet needs the parent of the NanoServer folder,
            // if we've been given a folder called NanoServer that doesn't have a 
            // NanoServer child folder, try to use the parent.
            var d = new DirectoryInfo(NanoServerInstallFiles);
            if (d.Name.ToLowerInvariant() == "nanoserver" && !Directory.Exists(Path.Combine(NanoServerInstallFiles, "NanoServer")))
            {
                NanoServerInstallFiles = d.Parent.FullName;
            }

            var exePath = FindExe(ExeName, InputFolder);
            var appName = Path.GetFileNameWithoutExtension(exePath);
            if (string.IsNullOrWhiteSpace(VhdDestinationFolder))
            {
                VhdDestinationFolder = Path.Combine(Directory.GetParent(InputFolder).FullName, "Nanopacked");
                if (!Directory.Exists(VhdDestinationFolder))
                {
                    Directory.CreateDirectory(VhdDestinationFolder);
                }
            }
            var vhdFilePath = Path.Combine(VhdDestinationFolder, appName + (Vhdx ? ".vhdx" : ".vhd"));
            var working = PrepareWorkingDirectory();

            var variables = new VariableDictionary();
            variables.Set("appName", appName);
            variables.Set("port", Port.ToString());
            variables.Set("vhd", vhdFilePath);
            variables.Set("inputFolder", InputFolder);
            variables.Set("machineName", MachineName);
            variables.Set("nanoserverFolder", NanoServerInstallFiles);
            variables.Set("edition", "Datacenter");
            variables.Set("vmpassword", Password);
            variables.Set("publishFolder", PublishFolder);
            variables.Set("firstBootScripts", ScriptPaths);

            Substitute(Path.Combine(working, "first-boot.ps1"), variables);
            Substitute(Path.Combine(working, "build-vhd.ps1"), variables);

            PowerShell.RunFile(working, "build-vhd.ps1");

            LogMessage($"VHD created at {vhdFilePath}");

            if (Package)
            {
                PackagAndUpload(exePath, appName, vhdFilePath);
            }

            LogMessage("Finished");

            return true;
        }

        private void PackagAndUpload(string exePath, string appName, string vhdFilePath)
        {
            var version = GetVersionInformation(exePath);
            var zipPath = Path.Combine(VhdDestinationFolder, $"{appName}.{version}.zip");
            LogMessage($"Packing VHD to {zipPath}");
            
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

            if (!KeepPackagedVhd)
            {
                LogMessage("Deleting packaged VHD, use --keepPackagedVhd to keep");
                File.Delete(vhdFilePath);
            }

            if (!string.IsNullOrWhiteSpace(OctopusUrl) && !string.IsNullOrWhiteSpace(ApiKey))
            {
                OctoPusher.Upload(OctopusUrl, ApiKey, zipPath, LogMessage);
                if (!KeepUploadedZip)
                {
                    LogMessage("Deleting zip that has been sent to Octopus, use --keepUploadedZip to keep");
                    File.Delete(zipPath);
                }
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
            return version.ProductVersion;
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

        void LogMessage(string message)
        {
            Console.WriteLine($"NanoPack: {message}");
        }
    }
}