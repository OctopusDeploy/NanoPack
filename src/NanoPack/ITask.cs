using System;

namespace NanoPack
{
    public interface ITask
    {
        bool Execute();
    }

    public enum MessageImportance
    {
        High,
        Normal,
        Low,
    }

    public abstract class AbstractTask
    {
        public abstract bool Execute();

        protected void LogMessage(string message)
        {
            Console.WriteLine($"NanoPack: {message}");
        }
    }
}