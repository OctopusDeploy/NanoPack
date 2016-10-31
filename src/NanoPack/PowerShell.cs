using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NanoPack
{
    internal static class PowerShell
    {
        internal static void RunFile(string directory, string file, Dictionary<string, string> parameters = null)
        {
            var arguments = new StringBuilder("-NoProfile -NoLogo -NonInteractive -ExecutionPolicy Unrestricted -File \"" + file + "\"");
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    arguments.Append(" -");
                    arguments.Append(p.Key);
                    arguments.Append(" \"");
                    arguments.Append(p.Value);
                    arguments.Append("\"");
                }
            }
            var code = 0;
            var errors = new StringBuilder();
            SilentProcessRunner.ExecuteCommand(GetPowerShellPath(),
                arguments.ToString(),
                directory,
                output => { if (output.StartsWith("NanoPack:")) {Console.WriteLine(output);} },
                error =>
                {
                    Console.WriteLine(error);
                    errors.Append(error);
                    code = 1;
                });

            if (code != 0)
                throw new Exception($"Script {file} failed with error {errors}");
        }

        internal static void RunCommands(string directory, params string[] commands)
        {
            var arguments = string.Join("; ", commands).Replace("\"", "\\\"");
            arguments = "-NoProfile -NoLogo -NonInteractive -ExecutionPolicy Unrestricted -Command \"& { $ErrorActionPreference = \\\"Stop\\\"; " + arguments + " }\"";
            Console.WriteLine(arguments);
            var code = 0;
            var errors = new StringBuilder();
            SilentProcessRunner.ExecuteCommand(GetPowerShellPath(),
                arguments,
                directory,
                output => Console.WriteLine(output),
                error =>
                {
                    Console.WriteLine(error);
                    errors.Append(error);
                    code = 1;
                });

            if (code != 0)
                throw new Exception($"Commands failed with error {errors}");
        }

        private static string GetPowerShellPath()
        {
            var system = Environment.GetEnvironmentVariable("SYSTEMROOT") ?? "C:\\Windows";
            var powerShellPath = Path.Combine(system, "System32", "WindowsPowershell", "v1.0", "PowerShell.exe");
            return powerShellPath;
        }
    }
}