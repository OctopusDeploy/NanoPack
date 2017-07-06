using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Octostache;

namespace NanoPack
{
    public class Util
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string FindExe(string exeName, string publishedAppFolder)
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
                    throw new NanoPackException($"No .exe found in the Published App Folder at {publishedAppFolder}. Application must be a self-contained ASP.NET Core app.");
                }
                else
                {
                    throw new NanoPackException($"More than one .exe found in the Published App Folder at {publishedAppFolder}. Please specify which .exe to inspect for version and naming information with the --exeName parameter.");
                }
            }
            else
            {
                path = Path.Combine(publishedAppFolder, exeName);
                if (!File.Exists(path))
                {
                    throw new NanoPackException($"No .exe found at {path} please check your --exePath setting.");
                }
            }

            return path;
        }

        public static string GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static string Substitute(string fileName, VariableDictionary variables)
        {
            string errors;
            var file = File.ReadAllText(fileName);
            var result = variables.Evaluate(file, out errors);
            File.WriteAllText(fileName, result, Encoding.ASCII);
            return errors;
        }
    }
}