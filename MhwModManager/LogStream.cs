using System;
using System.IO;

namespace MhwModManager
{
    public class LogStream
    {
        private StreamWriter writer;

        public LogStream(string path)
        {
            writer = new StreamWriter(path);
            writer.Close();
            writer = File.AppendText(path);
        }

        public void WriteLine(string value, string status = "INFO")
        {
            writer.WriteLine($"[{status}] {DateTime.Now} - {value}");
            writer.Flush();
        }

        public void Close()
        {
            writer.Flush();
            writer.Close();
        }
    }
}