using NUnit.Framework;

namespace NanoPack.Tests
{
    public class ShowHelpFixture : NanoPackFixture
    {
        [Test]
        public void ShouldShowHelpWithNoArguments()
        {
            var result = Invoke(NanoPack());
            result.Dump();
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