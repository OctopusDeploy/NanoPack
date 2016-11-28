using System;
using System.Linq;
using NUnit.Framework;

namespace NanoPack.Tests.Helpers
{
    public class NanoPackResult
    {
        private readonly int exitCode;
        private readonly CaptureCommandOutput captured;

        public NanoPackResult(int exitCode, CaptureCommandOutput captured)
        {
            this.exitCode = exitCode;
            this.captured = captured;
        }

        private int ExitCode
        {
            get { return exitCode; }
        }

        public CaptureCommandOutput CapturedOutput { get { return captured; } }

        public void AssertSuccess()
        {
            var capturedErrors = string.Join(Environment.NewLine, captured.Errors);
            Assert.That(ExitCode, Is.EqualTo(0), string.Format("Expected command to return exit code 0{0}{0}Output:{0}{1}", Environment.NewLine, capturedErrors));
        }

        public void AssertFailure()
        {
            Assert.That(ExitCode, Is.Not.EqualTo(0), "Expected a non-zero exit code");
        }


        public void AssertFailure(int code)
        {
            Assert.That(ExitCode, Is.EqualTo(code), $"Expected an exit code of {code}");
        }

        public void AssertOutput(string expectedOutputFormat, params object[] args)
        {
            AssertOutput(String.Format(expectedOutputFormat, args));
        }

        public void AssertNoOutput(string expectedOutput)
        {
            var allOutput = string.Join(Environment.NewLine, captured.Infos);

            Assert.That(allOutput, Does.Not.Contain(expectedOutput));
        }

        public void AssertOutput(string expectedOutput)
        {
            var allOutput = string.Join(Environment.NewLine, captured.Infos);

            Assert.That(allOutput, Does.Contain(expectedOutput));
        }

        public void AssertOutputMatches(string regex)
        {
            var allOutput = string.Join(Environment.NewLine, captured.Infos);

            Assert.That(allOutput, Does.Match(regex));
        }

        public string GetOutputForLineContaining(string expectedOutput)
        {
            var found = captured.Infos.SingleOrDefault(i => i.IndexOf(expectedOutput, StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsNotNull(found);
            return found;
        }

        public void AssertErrorOutput(string expectedOutputFormat, params object[] args)
        {
            AssertErrorOutput(String.Format(expectedOutputFormat, args));
        }

        public void AssertErrorOutput(string expectedOutput, bool noNewLines = false)
        {
            var separator = noNewLines ? String.Empty : Environment.NewLine;
            var allOutput = string.Join(separator, captured.Errors);
            Assert.That(allOutput, Does.Contain(expectedOutput));
        }
    }
}