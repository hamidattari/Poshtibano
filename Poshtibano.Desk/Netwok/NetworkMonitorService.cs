using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Poshtibano.Common;
using System.Diagnostics;

namespace NetworkMonitor
{
    public class NetworkMonitorService
    {
        private Task _monitoringTask;
        private bool _logData;
        private int _currentProcessId;

        private long _totalDownloadBytes;
        private long _totalUploadBytes;
        private long _lastDownloadBytes;
        private long _lastUploadBytes;

        public bool ShowLog { get; private set; } = false;

        public event EventHandler<NetworkUsage> OnNetworkUsageUpdated;

        public NetworkMonitorService(bool logData = false)
        {
            _logData = logData;
            _currentProcessId = Process.GetCurrentProcess().Id;
            Console.WriteLine($"Monitoring Process ID: {_currentProcessId}");
        }

        public void StartMonitoring(ClientRole role, int updateIntervalMs = 1000)
        {
            _monitoringTask = Task.Factory.StartNew(() => StartEtwSession(role), TaskCreationOptions.LongRunning);
            Task.Run(() => ReportLoop(updateIntervalMs));
        }

        private void StartEtwSession(ClientRole role)
        {
            try
            {
                string kernelSessionName = KernelTraceEventParser.KernelSessionName;

                try
                {
                    using (var existingSession = new TraceEventSession(kernelSessionName))
                    {
                        if (existingSession.IsActive)
                            existingSession.Stop(true);
                    }
                    Thread.Sleep(500);
                }
                catch { }

                using (var session = new TraceEventSession(kernelSessionName))
                {
                    session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                    session.StopOnDispose = true;

                    switch (role)
                    {
                        case ClientRole.Agent:
                            MonitorAgentTraffic(session);
                            break;
                        case ClientRole.Controller:
                            MonitorControllerTraffic(session);
                            break;
                    }

                    session.Source.Process();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
                Console.WriteLine("Did you run Visual Studio or the App as Administrator?");
            }
        }

        private void MonitorAgentTraffic(TraceEventSession session)
        {
            // ================= TCP IPv4 =================
            session.Source.Kernel.TcpIpRecv += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowTcpDataInfo(data);
            };
            session.Source.Kernel.TcpIpSend += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowTcpSendTraceDataInfo(data);
            };


            // ================= TCP IPv6 (Important for Localhost) =================
            session.Source.Kernel.TcpIpRecvIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowTcpV6DataInfo(data);

            };
            session.Source.Kernel.TcpIpSendIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowTcpV6SendTraceDataInfo(data);
            };

            // ================= UDP IPv4 =================
            session.Source.Kernel.UdpIpRecv += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowUdpDataInfo(data);

            };
            session.Source.Kernel.UdpIpSend += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowUdpDataInfo(data);
            };

            // ================= UDP IPv6 =================
            session.Source.Kernel.UdpIpRecvIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowUdpV6DataInfo(data);

            };
            session.Source.Kernel.UdpIpSendIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowUdpV6DataInfo(data);
            };
        }

        private void MonitorControllerTraffic(TraceEventSession session)
        {
            // ================= TCP IPv4 =================
            session.Source.Kernel.TcpIpRecv += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowTcpDataInfo(data);
            };
            session.Source.Kernel.TcpIpSend += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowTcpSendTraceDataInfo(data);
            };


            // ================= TCP IPv6 (Important for Localhost) =================
            session.Source.Kernel.TcpIpRecvIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowTcpV6DataInfo(data);

            };
            session.Source.Kernel.TcpIpSendIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowTcpV6SendTraceDataInfo(data);
            };

            // ================= UDP IPv4 =================
            session.Source.Kernel.UdpIpRecv += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowUdpDataInfo(data);

            };
            session.Source.Kernel.UdpIpSend += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowUdpDataInfo(data);
            };

            // ================= UDP IPv6 =================
            session.Source.Kernel.UdpIpRecvIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalDownloadBytes, data.size);
                if (_logData) ShowUdpV6DataInfo(data);

            };
            session.Source.Kernel.UdpIpSendIPV6 += data =>
            {
                if (data.ProcessID == _currentProcessId) Interlocked.Add(ref _totalUploadBytes, data.size);
                if (_logData) ShowUdpV6DataInfo(data);
            };
        }

        private static void ShowTcpDataInfo(TcpIpTraceData data)
        {
            string log = "";

            log += "{" + Environment.NewLine;
            log += "     TCP/IPv4 Event (Send or Receive)" + Environment.NewLine;
            log += $"    Timestamp          : {data.TimeStamp:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine;
            log += $"    Process Name       : {data.ProcessName ?? "Unknown"}" + Environment.NewLine;
            log += $"    Process ID (PID)   : {data.ProcessID}" + Environment.NewLine;
            log += $"    Thread ID (TID)    : {data.ThreadID}" + Environment.NewLine;
            log += $"    Processor Number   : {data.ProcessorNumber}" + Environment.NewLine;

            log += $"    Source Address     : {data.saddr}" + Environment.NewLine;
            log += $"    Source Port        : {data.sport}" + Environment.NewLine;
            log += $"    Destination Address: {data.daddr}" + Environment.NewLine;
            log += $"    Destination Port   : {data.dport}" + Environment.NewLine;

            log += $"    Packet Size (bytes): {data.size}" + Environment.NewLine;
            log += $"    Connection ID      : {data.connid}" + Environment.NewLine;
            log += $"    Sequence Number    : {data.seqnum}" + Environment.NewLine;

            if (data.GetType().GetProperty("send") != null)
            {
                bool isSend = (bool)data?.GetType()?.GetProperty("send")?.GetValue(data);
                log += $"    Direction          : {(isSend ? "Send" : "Receive")}" + Environment.NewLine;
            }
            else
                log += $"    Direction          : {data.OpcodeName}" + Environment.NewLine; 

            log += "}" + Environment.NewLine;

            Console.WriteLine(log);
        }

        private static void ShowTcpSendTraceDataInfo(TcpIpSendTraceData data)
        {
            string log = "";
            log += "{" + Environment.NewLine;
            log += "     TCP/IPv4 Send Event" + Environment.NewLine;
            log += $"    Timestamp          : {data.TimeStamp:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine;
            log += $"    Process Name       : {data.ProcessName ?? "Unknown"}" + Environment.NewLine;
            log += $"    Process ID (PID)   : {data.ProcessID}" + Environment.NewLine;
            log += $"    Thread ID (TID)    : {data.ThreadID}" + Environment.NewLine;
            log += $"    Processor Number   : {data.ProcessorNumber}" + Environment.NewLine;

            log += $"    Source Address     : {data.saddr}" + Environment.NewLine;
            log += $"    Source Port        : {data.sport}" + Environment.NewLine;
            log += $"    Destination Address: {data.daddr}" + Environment.NewLine;
            log += $"    Destination Port   : {data.dport}" + Environment.NewLine;

            log += $"    Packet Size (bytes): {data.size}" + Environment.NewLine;
            log += $"    Connection ID      : {data.connid}" + Environment.NewLine;
            log += $"    Sequence Number    : {data.seqnum}" + Environment.NewLine;

            log += "}" + Environment.NewLine;

            Console.WriteLine(log);
        }

        private static void ShowTcpV6DataInfo(TcpIpV6TraceData data)
        {
            string log = "";

            log += "{" + Environment.NewLine;
            log += "     TCP/IPv6 Event (Send or Receive)" + Environment.NewLine;
            log += $"    Timestamp          : {data.TimeStamp:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine;
            log += $"    Process Name       : {data.ProcessName ?? "Unknown"}" + Environment.NewLine;
            log += $"    Process ID (PID)   : {data.ProcessID}" + Environment.NewLine;
            log += $"    Thread ID (TID)    : {data.ThreadID}" + Environment.NewLine;
            log += $"    Processor Number   : {data.ProcessorNumber}" + Environment.NewLine;

            log += $"    Source Address     : [{data.saddr}]" + Environment.NewLine;
            log += $"    Source Port        : {data.sport}" + Environment.NewLine;
            log += $"    Destination Address: [{data.daddr}]" + Environment.NewLine;
            log += $"    Destination Port   : {data.dport}" + Environment.NewLine;

            log += $"    Packet Size (bytes): {data.size}" + Environment.NewLine;
            log += $"    Connection ID      : {data.connid}" + Environment.NewLine;
            log += $"    Sequence Number    : {data.seqnum}" + Environment.NewLine;
            log += "}" + Environment.NewLine;

            Console.WriteLine(log);
        }

        private static void ShowTcpV6SendTraceDataInfo(TcpIpV6SendTraceData data)
        {
            string log = "";

            log += "{" + Environment.NewLine;
            log += "     TCP/IPv6 Send Event" + Environment.NewLine;
            log += $"    Timestamp          : {data.TimeStamp:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine;
            log += $"    Process Name       : {data.ProcessName ?? "Unknown"}" + Environment.NewLine;
            log += $"    Process ID (PID)   : {data.ProcessID}" + Environment.NewLine;
            log += $"    Thread ID (TID)    : {data.ThreadID}" + Environment.NewLine;
            log += $"    Processor Number   : {data.ProcessorNumber}" + Environment.NewLine;

            log += $"    Source Address     : [{data.saddr}]" + Environment.NewLine;
            log += $"    Source Port        : {data.sport}" + Environment.NewLine;
            log += $"    Destination Address: [{data.daddr}]" + Environment.NewLine;
            log += $"    Destination Port   : {data.dport}" + Environment.NewLine;

            log += $"    Packet Size (bytes): {data.size}" + Environment.NewLine;
            log += $"    Connection ID      : {data.connid}" + Environment.NewLine;
            log += $"    Sequence Number    : {data.seqnum}" + Environment.NewLine;
            log += "}" + Environment.NewLine;

            Console.WriteLine(log);
        }

        private static void ShowUdpDataInfo(UdpIpTraceData data)
        {
            string log = "";

            log += "{" + Environment.NewLine;
            log += "     UDP/IPv6 Trace Event" + Environment.NewLine;
            log += $"    Timestamp          : {data.TimeStamp:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine;
            log += $"    Process Name       : {data.ProcessName ?? "Unknown"}" + Environment.NewLine;
            log += $"    Process ID (PID)   : {data.ProcessID}" + Environment.NewLine;
            log += $"    Thread ID (TID)    : {data.ThreadID}" + Environment.NewLine;
            log += $"    Processor Number   : {data.ProcessorNumber}" + Environment.NewLine;

            log += $"    Source Address     : {data.saddr}" + Environment.NewLine;
            log += $"    Source Port        : {data.sport}" + Environment.NewLine;
            log += $"    Destination Address: {data.daddr}" + Environment.NewLine;
            log += $"    Destination Port   : {data.dport}" + Environment.NewLine;

            log += $"    Packet Size (bytes): {data.size}" + Environment.NewLine;

            log += $"    Event Opcode       : {data.OpcodeName} ({data.Opcode})" + Environment.NewLine;
            log += $"    Event Level        : {data.Level}" + Environment.NewLine;
            log += "}" + Environment.NewLine;

            Console.WriteLine(log);
        }

        private static void ShowUdpV6DataInfo(UpdIpV6TraceData data)
        {
            string log = "";

            log += "{" + Environment.NewLine;
            log += "     UDP/IPv6 Trace Event" + Environment.NewLine;
            log += $"    Timestamp          : {data.TimeStamp:yyyy-MM-dd HH:mm:ss.fff}" + Environment.NewLine;
            log += $"    Process Name       : {data.ProcessName ?? "Unknown"}" + Environment.NewLine;
            log += $"    Process ID (PID)   : {data.ProcessID}" + Environment.NewLine;
            log += $"    Thread ID (TID)    : {data.ThreadID}" + Environment.NewLine;
            log += $"    Processor Number   : {data.ProcessorNumber}" + Environment.NewLine;

            log += $"    Source Address     : {data.saddr}" + Environment.NewLine;
            log += $"    Source Port        : {data.sport}" + Environment.NewLine;
            log += $"    Destination Address: {data.daddr}" + Environment.NewLine;
            log += $"    Destination Port   : {data.dport}" + Environment.NewLine;

            log += $"    Packet Size (bytes): {data.size}" + Environment.NewLine;
            log += $"    Connection ID      : {data.connid}" + Environment.NewLine;
            log += $"    Sequence Number    : {data.seqnum}" + Environment.NewLine;

            log += $"    Event Opcode       : {data.OpcodeName} ({data.Opcode})" + Environment.NewLine;
            log += $"    Event Level        : {data.Level}" + Environment.NewLine;
            log += "}" + Environment.NewLine;

            Console.WriteLine(log);
        }



        private void ReportLoop(int intervalMs)
        {
            while (true)
            {
                Thread.Sleep(intervalMs);

                long currentTotalDownload = Interlocked.Read(ref _totalDownloadBytes);
                long currentTotalUpload = Interlocked.Read(ref _totalUploadBytes);

                long downloadDiff = currentTotalDownload - _lastDownloadBytes;
                long uploadDiff = currentTotalUpload - _lastUploadBytes;

                _lastDownloadBytes = currentTotalDownload;
                _lastUploadBytes = currentTotalUpload;

                var usage = new NetworkUsage
                {
                    DownloadBytesPerSecond = Math.Max(0, downloadDiff),
                    UploadBytesPerSecond = Math.Max(0, uploadDiff)
                };

                if (ShowLog && (usage.DownloadBytesPerSecond > 0 || usage.UploadBytesPerSecond > 0))
                {
                    Console.WriteLine($"Traffic Detected -> DL: {usage.DownloadBytesPerSecond} | UP: {usage.UploadBytesPerSecond}");
                }

                OnNetworkUsageUpdated?.Invoke(this, usage);
            }
        }

        public void StopMonitoring()
        {
            _monitoringTask.Dispose();
        }
    }

    public class NetworkUsage
    {
        public long DownloadBytesPerSecond { get; set; }
        public long UploadBytesPerSecond { get; set; }

        public double DownloadKBPerSecond => DownloadBytesPerSecond / 1024.0;
        public double UploadKBPerSecond => UploadBytesPerSecond / 1024.0;

        public double DownloadMBPerSecond => DownloadBytesPerSecond / (1024.0 * 1024.0);
        public double UploadMBPerSecond => UploadBytesPerSecond / (1024.0 * 1024.0);

        public override string ToString()
        {
            return $"دانلود: {GetFormattedSize(DownloadBytesPerSecond)}/s | آپلود: {GetFormattedSize(UploadBytesPerSecond)}/s";
        }

        private string GetFormattedSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.00} {sizes[order]}";
        }
    }
}