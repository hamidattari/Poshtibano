using Microsoft.Diagnostics.Tracing.StackSources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services
{
    public class ProcessNameManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point p);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr ChildWindowFromPoint(IntPtr hWnd, Point p);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const int WM_NCHITTEST = 0x0084;

        private static IntPtr MakeLParam(int x, int y)
        {
            return (IntPtr)((y << 16) | (x & 0xFFFF));
        }

        public string GetChildWindowFromPoint(Point point)
        {
            IntPtr hWnd = WindowFromPoint(point);
            IntPtr child = ChildWindowFromPoint(hWnd, point);
            GetWindowThreadProcessId(child, out int pid);

            var className = new StringBuilder(256);
            GetClassName(child, className, className.Capacity);

            string windowClass = className.ToString();

            return className.ToString();
        }

        public string GetHitNameByMousePosition(Point point)
        {
            Point mousePos = Cursor.Position;
            IntPtr hWnd = WindowFromPoint(mousePos);

            IntPtr result = SendMessage(hWnd, WM_NCHITTEST, IntPtr.Zero, MakeLParam(mousePos.X, mousePos.Y));

            return $"{(HitTestValues)result}";
        }

        public string GetProcessNameByMousePosition(Point point)
        {
            IntPtr hwnd = WindowFromPoint(point);
            return GetProcessNameByHandle(hwnd);
        }

        public string GetProcessNameByForegroundWindow()
        {
            IntPtr hwnd = GetForegroundWindow();
            return GetProcessNameByHandle(hwnd);
        }

        private string GetProcessNameByHandle(IntPtr hwnd)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out int pid);

                var process = Process.GetProcessById(pid).ProcessName.ToLower();
                return process;
            }
            catch (Exception ex)
            {
                return $"Error in ProcessNameManager: {ex.Message}";
            }
        }


        public bool IsForbidden(string processName)
        {
            foreach (var item in GetForbiddenProcess())
            {
                if (item.ToLower().Trim().Contains(processName.ToLower().Trim())) return true;
            }

            return false;
        }

        private string[] GetForbiddenProcess()
        {
            string[] restrictedProcs = {
                    //"poshtibano.desk",
                    // Windows Defender
                    "msmpeng",           // Microsoft Network Realtime Inspection Service
                    "smartscreen",       // Windows SmartScreen
                    "nisssrv",           // Windows Defender Antivirus Network Inspection Service
                    "secHealthui",       // Windows Defender Antivirus UI
                    "securityhealthservice",

                    // ESET (NOD32)
                    "egui",              // ESET Main GUI
                    "ekrn",              // ESET Kernel Service

                    // Kaspersky
                    "avp",               // Kaspersky Main Process
                    "avpui",             // Kaspersky User Interface

                    // Avast & AVG
                    "avastsvc",          // Avast Service
                    "avastui",           // Avast GUI
                    "avgsvc",            // AVG Service
                    "avgui",             // AVG GUI

                    // Bitdefender
                    "vsserv",            // Bitdefender Communication Server
                    "bdagent",           // Bitdefender Agent
                    "bdredline",         // Bitdefender Redline Service

                    // McAfee
                    "mcshield",          // McAfee Real-Time Scanner
                    "mfevtps",           // McAfee Validation Trust Protection
                    "mcupdmgr",          // McAfee Update Manager
                    "mfewch",            // McAfee Host Intrusion Prevention

                    // Norton / Symantec
                    "navapsvc",          // Norton AntiVirus Auto-Protect Service
                    "symcorpui",         // Symantec Endpoint Protection GUI
                    "ccsvchst",          // Symantec Service Framework

                    // Trend Micro
                    "tmproxy",           // Trend Micro Proxy Service
                    "pccntmon",          // Trend Micro Client Main Console

                    // Avira
                    "avguard",           // Avira Real-Time Protection
                    "avgnt",             // Avira System Tray

                    // Malwarebytes
                    "mbamservice",       // Malwarebytes Service
                    "taskmgr",           // Task Manager
                    "resmon",             // Resource Monitor
                    "mmc",                // Microsoft Management Console (Services.msc)
                    //"powershell",       // PowerShell
                    //"cmd"               // Command Prompt
                };
            return restrictedProcs;
        }
    }

    public enum HitTestValues
    {
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,     // Title bar
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTMENU = 5,
        HTHSCROLL = 6,
        HTVSCROLL = 7,
        HTMINBUTTON = 8,   // Minimize
        HTMAXBUTTON = 9,   // Maximize
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTCLOSE = 20       // 🔴 Close button
    }
}

