namespace Poshtibano.Common
{
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Debug(string message);
        void WriteLine(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Info(string message) => Console.WriteLine($"[{DateTime.Now}] ℹ️  {message}");
        public void Warning(string message) => Console.WriteLine($"[{DateTime.Now}] ⚠️  {message}");
        public void Error(string message) => Console.WriteLine($"[{DateTime.Now}] ❌ {message}");
        public void Debug(string message) => Console.WriteLine($"[{DateTime.Now}] 🔍 {message}");
        public void WriteLine(string message) => Console.WriteLine($"[{DateTime.Now}] 🔍 {message}");
    }

    public static class LoggerService
    {
        private static ILogger _instance;

        public static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConsoleLogger(); 
                }
                return _instance;
            }
            set => _instance = value;
        }

        public static void Info(string message) => Instance.Info(message);
        public static void Warning(string message) => Instance.Warning(message);
        public static void Error(string message) => Instance.Error(message);
        public static void Debug(string message) => Instance.Debug(message);
        public static void WriteLine(string message) => Instance.WriteLine(message);
    }
}