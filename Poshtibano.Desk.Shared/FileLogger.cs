using System.Text;
using System.Text.RegularExpressions;

namespace Poshtibano.Desk.Shared
{
    public class FileLogger : TextWriter
    {
        private readonly TextWriter _consoleOut;
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileLogger(string filename)
        {
            _consoleOut = Console.Out;
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);

            if (!File.Exists(_filePath))
            {
                string header = "===========================================================\n" +
                   $"--- Log Started at {DateTime.Now} ---\n";

                File.WriteAllText(_filePath, header, Encoding.UTF8);
            }
        }

        public override void WriteLine(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            string timestampPattern = @"^\[\d{4}[-\/]\d{1,2}[-\/]\d{1,2}\s+\d{1,2}:\d{1,2}:\d{1,2}(?:\s*[AP]M)?\]\s*";

            string cleanMessage = Regex.Replace(value, timestampPattern, "").Trim();

            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {cleanMessage}";

            lock (_lock)
            {
                _consoleOut.WriteLine(logLine);
                try
                {
                    File.AppendAllText(_filePath, logLine + Environment.NewLine, Encoding.UTF8);
                }
                catch {  }
            }
        }

        public override void Write(string value)
        {
            lock (_lock)
            {
                _consoleOut.Write(value);
                File.AppendAllText(_filePath, value, Encoding.UTF8);
            }
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
