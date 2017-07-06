using System;

namespace NanoPack.Tests.Helpers
{
    public class CommandLineRunner
    {
        private readonly CaptureCommandOutput commandOutput;

        public CommandLineRunner(CaptureCommandOutput commandOutput)
        {
            this.commandOutput = commandOutput;
        }

        public CommandResult Execute(CommandLineInvocation invocation)
        {
            try
            {
                Console.WriteLine("Executing: " + invocation);
                var exitCode = SilentProcessRunner.ExecuteCommand(
                    invocation.Executable,
                    invocation.Arguments,
                    invocation.WorkingDirectory,
                    commandOutput.WriteInfo,
                    commandOutput.WriteError);

                return new CommandResult(invocation.ToString(), exitCode, null);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine("The command that caused the exception was: " + invocation);
                return new CommandResult(invocation.ToString(), -1, ex.ToString());
            }
        }
    }
}