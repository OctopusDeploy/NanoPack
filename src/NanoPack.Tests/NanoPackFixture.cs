using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using NanoPack.Tests.Helpers;
using NUnit.Framework;

namespace NanoPack.Tests
{
    public abstract class NanoPackFixture
    {
        protected CommandLine NanoPack()
        {
            var folder = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.FullLocalPath());
            var nanoPackFullPath = Path.Combine(folder, "NanoPack.Tests.exe");
            return CommandLine.Execute(nanoPackFullPath);
        }

        protected NanoPackResult Invoke(CommandLine command)
        {
            var capture = new CaptureCommandOutput();
            var runner = new CommandLineRunner(capture);
            var result = runner.Execute(command.Build());
            return new NanoPackResult(result.ExitCode, capture);
        }
    }

    public class ShowHelpFixture : NanoPackFixture
    {
        [Test]
        public void ShouldShowHelpWithNoArguments()
        {
            var result = Invoke(NanoPack());
            result.AssertOutput("Usage:  [options]");
        }

        [Test]
        public void ShouldShowHelpWithNoMediaPath()
        {
            var result = Invoke(NanoPack().Flag("vhdx"));
            result.AssertOutput("Usage:  [options]");
        }
    }
}
