using System;
using System.IO;
using System.Reflection;
using Octostache;

namespace NanoPack
{
    internal class BuildVhdTask
    {
        private readonly IPowerShell _powerShell;
        private readonly IPackager _packager;
        private string _working;
        public string VhdDestinationFolder { get; set; }
        public string InputFolder { get; private set; }
        public string NanoServerInstallFiles { get; private set; }
        public int Port { get; set; } = 80;
        public string ExeName { get; set; }
        public string PublishFolder { get; set; } = "PublishedApp";
        public string Password { get; set; } = "P@ssw0rd";
        public string MachineName { get; set; } = "NanoServer";
        public EditionType Edition { get; private set; } = EditionType.Datacenter;
        public bool Vhdx { get; set; }
        public string ScriptPaths { get; set; }
        public string Additional { get; set; }
        public string MaxSize { get; set; } = "4GB";
        public string CopyPath { get; set; }

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
                throw new NanoPackException($"Unable to convert {edition} into a valid edition value of Standard or Datacenter");
            }
        }

        public BuildVhdTask(IPowerShell powerShell, IPackager packager, string inputFolder, string nanoServerInstallFiles)
        {
            _powerShell = powerShell;
            _packager = packager;
            InputFolder = inputFolder;
            NanoServerInstallFiles = nanoServerInstallFiles;
        }

        public int Generate()
        {
            try
            {
                if (!Util.IsAdministrator())
                {
                    LogError("This application must be run with elevated privileges");
                    return 1;
                }

                CheckWebConfig();

                // the New-NanoServerImage cmdlet needs the parent of the NanoServer folder,
                // if we've been given a folder called NanoServer that doesn't have a 
                // NanoServer child folder, try to use the parent.
                var d = new DirectoryInfo(NanoServerInstallFiles);
                if (d.Name.ToLowerInvariant() == "nanoserver" &&
                    !Directory.Exists(Path.Combine(NanoServerInstallFiles, "NanoServer")))
                {
                    NanoServerInstallFiles = d.Parent.FullName;
                }

                var exePath = Util.FindExe(ExeName, InputFolder);
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
                _working = PrepareWorkingDirectory();

                var variables = new VariableDictionary();
                variables.Set("appName", appName);
                variables.Set("port", Port.ToString());
                variables.Set("vhd", vhdFilePath);
                variables.Set("inputFolder", InputFolder);
                variables.Set("machineName", MachineName);
                variables.Set("nanoserverFolder", NanoServerInstallFiles);
                variables.Set("edition", Edition.ToString());
                variables.Set("vmpassword", Password);
                variables.Set("publishFolder", PublishFolder);
                variables.Set("firstBootScripts", ScriptPaths);
                variables.Set("additional", Additional);
                variables.Set("maxSize", MaxSize);
                variables.Set("copyPath", CopyPath);

                Util.Substitute(Path.Combine(_working, "first-boot.ps1"), variables);
                Util.Substitute(Path.Combine(_working, "build-vhd.ps1"), variables);

                _powerShell.RunFile(_working, "build-vhd.ps1");

                LogMessage($"VHD created at {vhdFilePath}");

                _packager.PackageAndUpload(exePath, appName, VhdDestinationFolder, vhdFilePath, LogMessage);

                LogMessage("Finished");

                return 0;
            }
            catch (NanoPackException e)
            {
                LogError(e.Message);
                return 1;
            }
            finally
            {
                if (Directory.Exists(_working))
                {
                    Directory.Delete(_working, recursive: true);
                }
            }
        }

        private void CheckWebConfig()
        {
            var webConfigPath = Path.Combine(InputFolder, "web.config");
            if (!File.Exists(webConfigPath))
            {
                throw new NanoPackException($"No web.config found in {InputFolder}. The folder should contain a published ASP.NET Core application");
            }

            var config = File.ReadAllText(webConfigPath);
            if (config.Contains("%LAUNCHER_PATH%"))
            {
                throw new NanoPackException("web.config still contains %LAUNCHER_PATH%. Use dotnet publish-iis in your project.json post-publish scripts, or set this manually");
            }
        }

        private string PrepareWorkingDirectory()
        {
            var tempDir = Util.GetTemporaryDirectory();
            LogMessage($"Working directory is {tempDir}");
            var assembly = typeof(BuildVhdTask).GetTypeInfo().Assembly;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                var destFileName = Path.Combine(tempDir, resourceName.Replace("NanoPack.tools.", ""));
                LogMessage($"Extracting {resourceName} to {destFileName}");
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var file = new FileStream(destFileName, FileMode.Create))
                {
                    for (var i = 0; i < stream.Length; i++)
                    {
                        file.WriteByte((byte) stream.ReadByte());
                    }
                }
            }
            return tempDir;
        }

        void LogMessage(string message)
        {
            Console.WriteLine($"NanoPack: {message}");
        }

        void LogError(string message)
        {
            Console.Error.WriteLine($"NanoPack: {message}");
        }
    }
}
