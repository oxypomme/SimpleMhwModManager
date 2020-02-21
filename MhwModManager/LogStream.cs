using System;
using System.IO;

namespace MhwModManager
{
    public class LogStream
    {
        private TextWriter writer;

        public LogStream(string path)
        {
            writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        }

        public void Close()
        {
            writer.Close();
        }

        public void Error(object value) => Log(value, "ERROR");

        public void Log(object value, string status = "INFO")
        {
            writer.WriteLine($"[{status}] {DateTime.Now} - {value.ToString()}");
        }

        public void Warning(object value) => Log(value, "WARNING");

        [Obsolete]
        public void WriteLine(object value, string status = "INFO") => Log(value, status);
    }
}