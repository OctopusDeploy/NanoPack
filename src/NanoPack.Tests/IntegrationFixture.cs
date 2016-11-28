using System;
using System.IO;
using System.Text;
using FluentAssertions;
using NanoPack.Tests.Helpers;
using NUnit.Framework;

namespace NanoPack.Tests
{
    public class IntegrationFixture : NanoPackFixture
    {
        private const string NanoServerFilePath = @"C:\vm";

        [OneTimeSetUp]
        public void Setup()
        {
            if (string.IsNullOrWhiteSpace(NanoServerFilePath))
                throw new Exception("Set the path to your copy of the nanoserver build files to run tests");
        }

        [Test]
        public void MinimalTestRunSucceeds()
        {
            using (var tempDir = new TemporaryDirectory(Util.GetTemporaryDirectory()))
            {
                var result = Invoke(NanoPack()
                    .Argument("outputPath", tempDir.Path)
                    .Argument("mediaPath", NanoServerFilePath)
                    .Argument("inputPath", GetTestPath("TestApp")));

                result.Dump();
                result.AssertSuccess();
                var imagePath = Path.Combine(tempDir.Path, "HelloWorld.vhd");
                File.Exists(imagePath).Should().BeTrue();

                //mount the created VHD and check for some files
                var mountPath = Path.Combine(tempDir.Path, "Mount");
                Directory.CreateDirectory(mountPath);
                var powerShell = new PowerShell();
                try
                {
                    powerShell.RunCommands(tempDir.Path, $"Mount-WindowsImage -ImagePath \"{imagePath}\" -Path \"{mountPath}\" -Index 1");
                    File.Exists(Path.Combine(mountPath, "first-boot.ps1")).Should().BeTrue();
                    File.Exists(Path.Combine(mountPath, "PublishedApp", "HelloWorld.exe")).Should().BeTrue();
                }
                finally
                {
                    powerShell.RunCommands(tempDir.Path, $"Dismount-WindowsImage -Path \"{mountPath}\" -Discard");
                }
            }
        }

        [Test]
        public void EveryOptionSucceeds()
        {
            using (var tempDir = new TemporaryDirectory(Util.GetTemporaryDirectory()))
            {
                var result = Invoke(NanoPack()
                        .Argument("outputPath", tempDir.Path)
                        .Argument("mediaPath", NanoServerFilePath)
                        .Argument("inputPath", GetTestPath("TestApp"))
                        .Argument("maxSize", "2GB")
                        .Argument("publishPath", "PublishedApps\\HelloWorld")
                        .Argument("executableName", "HelloWorld.exe")
                        .Argument("port", "8080")
                        .Argument("computerName", "OctopusMachine")
                        .Flag("package")
                        .Flag("vhdx")
                        .Flag("keepPackagedVhd")
                        .Argument("edition", "Standard")
                        .Argument("firstBootScript", Path.Combine(GetTestPath("TestFiles", "TestFile1.txt")))
                        .Argument("copyPath", GetCopyPathString())
                        .Argument("additional", "-Development")
                );

                result.Dump();
                result.AssertSuccess();
                result.AssertOutput("-Development");
                result.AssertOutput("-Edition Standard");
                result.AssertOutput("-ComputerName OctopusMachine");
                result.AssertOutput("-MaxSize 2GB");

                var imagePath = Path.Combine(tempDir.Path, "HelloWorld.vhdx");
                File.Exists(imagePath).Should().BeTrue();
                File.Exists(Path.Combine(tempDir.Path, "HelloWorld.1.0.5.zip")).Should().BeTrue();

                //mount the created VHD and check for some files
                var mountPath = Path.Combine(tempDir.Path, "Mount");
                Directory.CreateDirectory(mountPath);
                var powerShell = new PowerShell();
                try
                {
                    powerShell.RunCommands(tempDir.Path, $"Mount-WindowsImage -ImagePath \"{imagePath}\" -Path \"{mountPath}\" -Index 1");
                    File.Exists(Path.Combine(mountPath, "first-boot.ps1")).Should().BeTrue();
                    File.Exists(Path.Combine(mountPath, "FirstBootScripts", "TestFile1.txt")).Should().BeTrue();
                    File.Exists(Path.Combine(mountPath, "CopiedFiles", "TestFile2.txt")).Should().BeTrue();
                    File.Exists(Path.Combine(mountPath, "CopiedFiles", "TestFile3.txt")).Should().BeTrue();
                    File.Exists(Path.Combine(mountPath, "PublishedApps", "HelloWorld", "HelloWorld.exe")).Should().BeTrue();
                }
                finally
                {
                    powerShell.RunCommands(tempDir.Path, $"Dismount-WindowsImage -Path \"{mountPath}\" -Discard");
                }
            }
        }

        private string GetCopyPathString()
        {
            var file2 = Path.Combine(GetTestPath("TestFiles", "TestFile2.txt"));
            var file3 = Path.Combine(GetTestPath("TestFiles", "TestFile3.txt"));

            var copyPath = $"@{{ \"{file2}\" = \"CopiedFiles\"; \"{file3}\" = \"CopiedFiles\" }}";

            return copyPath;
        }
    }
}