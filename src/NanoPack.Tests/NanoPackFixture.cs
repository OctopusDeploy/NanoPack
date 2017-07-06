using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using NanoPack.Tests.Helpers;

namespace NanoPack.Tests
{
    public abstract class NanoPackFixture
    {
        public static readonly string AssemblyLocalPath = typeof(NanoPackFixture).GetTypeInfo().Assembly.FullLocalPath();
        public static readonly string CurrentWorkingDirectory = Path.GetDirectoryName(AssemblyLocalPath);

        public static string GetTestPath(params string[] paths)
        {
            return Path.Combine(CurrentWorkingDirectory, Path.Combine(paths));
        }


        protected CommandLine NanoPack()
        {
            var folder = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.FullLocalPath());
            var nanoPackFullPath = Path.Combine(folder, "NanoPack.Tests.dll");
            return CommandLine.Execute("dotnet")
                .Argument(nanoPackFullPath);
        }

        protected NanoPackResult Invoke(CommandLine command)
        {
            var capture = new CaptureCommandOutput();
            var runner = new CommandLineRunner(capture);
            var result = runner.Execute(command.Build());
            return new NanoPackResult(result.ExitCode, capture);
        }
    }
}
