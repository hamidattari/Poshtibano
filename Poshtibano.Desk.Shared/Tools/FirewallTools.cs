using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Shared.Tools
{
    internal class FirewallTools
    {
        public static void AddFirewall()
        {
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string ruleName = "PoshtibanoTemp";

            string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" " +
                               $"dir=in action=allow program=\"{appPath}\" enable=yes";

            var psi = new ProcessStartInfo("netsh", arguments)
            {
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
                Console.WriteLine("Firewall rule added.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
