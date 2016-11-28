using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace NanoPack
{
    internal interface IPackager
    {
        void PackageAndUpload(string exePath, string appName, string vhdDestinationFolder, string vhdFilePath, Action<string> logMessage);
    }

    internal class Packager : IPackager
    {
        private readonly IPusher _pusher;
        private readonly bool _package;
        private readonly bool _keepPackagedVhd;
        private readonly bool _keepUploadedZip;

        public Packager(IPusher pusher, bool package, bool keepPackagedVhd, bool keepUploadedZip)
        {
            _pusher = pusher;
            _package = package;
            _keepPackagedVhd = keepPackagedVhd;
            _keepUploadedZip = keepUploadedZip;
        }

        public void PackageAndUpload(string exePath, string appName, string vhdDestinationFolder, string vhdFilePath, Action<string> logMessage)
        {
            if (!_package)
                return;

            var version = GetVersionInformation(exePath, logMessage);
            var zipPath = Path.Combine(vhdDestinationFolder, $"{appName}.{version}.zip");
            logMessage($"Packing VHD to {zipPath}");

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var arch = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                arch.CreateEntryFromFile(vhdFilePath, Path.GetFileName(vhdFilePath), CompressionLevel.Optimal);
            }

            logMessage($"VHD packaged to {zipPath}");

            if (!_keepPackagedVhd)
            {
                logMessage("Deleting packaged VHD, use --keepPackagedVhd to keep");
                File.Delete(vhdFilePath);
            }

            if (_pusher.CanPush)
            {
                _pusher.Upload(zipPath, logMessage);
                if (!_keepUploadedZip)
                {
                    logMessage("Deleting zip that has been sent to Octopus, use --keepUploadedZip to keep");
                    File.Delete(zipPath);
                }
            }
        }

        private string GetVersionInformation(string exePath, Action<string> logMessage)
        {
            // .NET Core doesn't seem to set the version on a published exe as of 1.0.1, only it's underlying dll.
            var dllPath = exePath.Remove(exePath.Length - 3) + "dll";
            var checkPath = File.Exists(dllPath) ? dllPath : exePath;
            logMessage($"Extracting version information from {checkPath}");
            var version = FileVersionInfo.GetVersionInfo(checkPath);
            return version.ProductVersion;
        }
    }
}
