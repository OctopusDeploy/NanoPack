using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NanoPack.Tests.Helpers
{
    public class TemporaryDirectory : IDisposable
    {
        private readonly string _directoryPath;

        public TemporaryDirectory(string directoryPath)
        {
            this._directoryPath = directoryPath;
        }

        public string Path => _directoryPath;

        public void Dispose()
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }
}
