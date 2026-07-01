using System.Net;
using System.Net.Sockets;

namespace Poshtibano.Desk.Shared.Tools
{
    public class InternetTools
    {

        public static string GetLocalIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public static async Task<string> GetPublicIPv4()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
            };
            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    string ip = await client.GetStringAsync("https://api.ipify.org");
                    return ip.Trim();
                }
                catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            }
        }

        public static string GetLocalIPv4()
        {
            string localIP = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return string.IsNullOrEmpty(localIP) ? "IPv4 not found!" : localIP;
        }
    }
}

