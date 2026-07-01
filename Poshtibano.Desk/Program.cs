using Poshtibano.Desk.Shared;

namespace Poshtibano.Desk
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (sender, e) =>
            {
                Console.WriteLine($"[THREAD EXCEPTION] {e.Exception}");
                Console.WriteLine(e.Exception.StackTrace);
                MessageBox.Show($"Fatal Error:\n{e.Exception.Message}\n{e.Exception.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine($"[UNHANDLED EXCEPTION] {e.ExceptionObject}");
                if (e.ExceptionObject is Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (IsRunningInRealConsole()) Console.OutputEncoding = System.Text.Encoding.UTF8;

            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var filePrefix = $"{DateTime.Now:yyyy-MM-dd HH-mm-ss}";
            Console.SetOut(new FileLogger($"log\\{filePrefix}_log.txt"));

            DeleteOldLogFiles();

            Application.Run(new MainForm());
        }

        private static void DeleteOldLogFiles()
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            string twoDaysAgo = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd");

            string searchPattern = $"{twoDaysAgo} *_log.txt";
            string[] oldFiles = Directory.GetFiles(directory, searchPattern);

            if (oldFiles.Length > 20)
            {
                Array.Sort(oldFiles, (a, b) => string.Compare(b, a));

                for (int index = 20; index < oldFiles.Length; index++)
                {
                    File.Delete(oldFiles[index]);
                }
            }
        }

        public static bool IsRunningInRealConsole()
        {
            try
            {
                // Method 1: Strongest method in .NET 6+
                if (Console.OpenStandardInput() == Stream.Null)
                    return false;

                // Method 2: If the console window doesn't exist (double-click in Windows)
                if (OperatingSystem.IsWindows())
                {
                    _ = Console.WindowHeight; // Throws an exception if not a real console
                    _ = Console.GetCursorPosition();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}