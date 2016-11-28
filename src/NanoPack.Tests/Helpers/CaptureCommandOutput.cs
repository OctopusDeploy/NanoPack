using System.Collections.Generic;

namespace NanoPack.Tests.Helpers
{
    public class CaptureCommandOutput
    {
        readonly List<string> all = new List<string>();
        readonly List<string> infos = new List<string>();
        readonly List<string> errors = new List<string>();

        public void WriteInfo(string line)
        {
            all.Add(line);
            infos.Add(line);
        }

        public void WriteError(string line)
        {
            all.Add(line);
            errors.Add(line);
        }

        public IList<string> Infos
        {
            get { return infos; }
        }

        public IList<string> Errors
        {
            get { return errors; }
        }

        public IList<string> AllMessages
        {
            get { return all; }
        }
    }
}