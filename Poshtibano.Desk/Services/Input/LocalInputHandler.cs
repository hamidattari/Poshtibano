using Poshtibano.Common;
using Poshtibano.Desk.Shared.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace Poshtibano.Desk.Services.Input
{
    public class LocalInputHandler
    {
        private ProcessNameManager _processNameManager = new ProcessNameManager();
        private MonitorInfo _currentMonitor;

        public IReadOnlyList<MonitorInfo> AvailableMonitors { get; private set; } = new List<MonitorInfo>();

        public event Action<string> OnMouseOnProcess;
        public event Action<string> OnMouseDeniedOnProcess;


        public void UpdateMonitorList(List<MonitorInfo> monitors, int? activeIndex = null)
        {
            if (monitors == null || monitors.Count == 0)
                return;

            AvailableMonitors = monitors;

            if (activeIndex.HasValue && activeIndex.Value >= 0 && activeIndex.Value < monitors.Count)
                _currentMonitor = monitors[activeIndex.Value];
            else
                _currentMonitor = monitors.Find(m => m.IsActive) ?? monitors[0];
        }

        public void SetActiveMonitor(int index)
        {
            if (index >= 0 && index < AvailableMonitors.Count)
                _currentMonitor = AvailableMonitors[index];
        }

        public MonitorInfo CurrentMonitor => _currentMonitor;

        public bool ProcessEventData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return false;

            byte eventType = data[0];

            try
            {
                //var process = _processNameManager.GetProcessNameByForegroundWindow();
                var process = _processNameManager.GetProcessNameByMousePosition(Cursor.Position);
                OnMouseOnProcess?.Invoke(process);

                switch (eventType)
                {
                    case 0: // Mouse movement
                        return HandleMouseMove(data);
                    case 1: // کلMouse wheel or click
                        if (_processNameManager.IsForbidden(process))
                        {
                            OnMouseDeniedOnProcess?.Invoke(process);
                            return false;
                        }
                        return HandleMouseButton(data);
                    case 2: // Keyboard
                        return HandleKeyboard(data);
                    case 3: // Clipboard text
                        return HandleClipboardText(data);
                    case 4: // Clipboard files
                        return HandleClipboardFiles(data);
                    default:
                        Console.WriteLine($"[{DateTime.Now}] Unknown event: {eventType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error in proccesing event: {ex.Message}");
                return false;
            }
        }


        private bool HandleMouseMove(byte[] data)
        {
            if (data.Length < 9)
                return false;

            float normalizedX = BitConverter.ToSingle(data, 1); // [0, 1]
            float normalizedY = BitConverter.ToSingle(data, 5); // [0, 1]

            Rectangle monitorBounds = _currentMonitor.ScreenBounds;
            float scale = _currentMonitor.Scale;

            int virtualLeft = SystemInformation.VirtualScreen.Left;
            int virtualTop = SystemInformation.VirtualScreen.Top;
            int virtualWidth = SystemInformation.VirtualScreen.Width;
            int virtualHeight = SystemInformation.VirtualScreen.Height;

            // ✅ Conversion 1: Normalize → Logical monitor coordinates
            int logicalX = (int)(normalizedX * monitorBounds.Width) + monitorBounds.Left;
            int logicalY = (int)(normalizedY * monitorBounds.Height) + monitorBounds.Top;

            // ✅ Conversion 2: Logical coordinates → Physical coordinates (with DPI)
            int physicalX = (int)((logicalX - virtualLeft) / scale);
            int physicalY = (int)((logicalY - virtualTop) / scale);

            // ✅ Conversion 3: Physical → SendInput (0‑65535)
            int physicalVirtualWidth = (int)(virtualWidth);
            int physicalVirtualHeight = (int)(virtualHeight);

            int dx, dy;
            if (physicalVirtualWidth > 0 && physicalVirtualHeight > 0)
            {
                dx = (int)((physicalX * 65535.0f) / physicalVirtualWidth);
                dy = (int)((physicalY * 65535.0f) / physicalVirtualHeight);
            }
            else
            {
                dx = 0;
                dy = 0;
            }

            // Limit
            dx = Math.Max(0, Math.Min(65535, dx));
            dy = Math.Max(0, Math.Min(65535, dy));

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = InputTools.INPUT_MOUSE;
            inputs[0].U.mi.dx = dx;
            inputs[0].U.mi.dy = dy;
            inputs[0].U.mi.dwFlags = InputTools.MOUSEEVENTF_MOVE |
                                     InputTools.MOUSEEVENTF_ABSOLUTE |
                                     InputTools.MOUSEEVENTF_VIRTUALDESK;

            InputTools.SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            return true;
        }

        private bool HandleMouseButton(byte[] data)
        {
            if (data.Length < 3)
                return false;

            byte button = data[1];
            byte state = data[2];

            // پردازش دابل کلیک (State == 2)
            if (state == 2)
            {
                uint downFlag = 0;
                uint upFlag = 0;

                switch (button)
                {
                    case 1:
                        downFlag = InputTools.MOUSEEVENTF_LEFTDOWN;
                        upFlag = InputTools.MOUSEEVENTF_LEFTUP;
                        break;
                    case 2:
                        downFlag = InputTools.MOUSEEVENTF_RIGHTDOWN;
                        upFlag = InputTools.MOUSEEVENTF_RIGHTUP;
                        break;
                    case 4:
                        downFlag = InputTools.MOUSEEVENTF_MIDDLEDOWN;
                        upFlag = InputTools.MOUSEEVENTF_MIDDLEUP;
                        break;
                }

                if (downFlag != 0 && upFlag != 0)
                {
                    // تزریق همزمان و سریع 4 اکشن برای اطمینان از ثبت قطعی دابل کلیک در سیستم عامل
                    INPUT[] doubleClickInputs = new INPUT[4];
                    for (int i = 0; i < 4; i++) doubleClickInputs[i].type = InputTools.INPUT_MOUSE;

                    doubleClickInputs[0].U.mi.dwFlags = downFlag;
                    doubleClickInputs[1].U.mi.dwFlags = upFlag;
                    doubleClickInputs[2].U.mi.dwFlags = downFlag;
                    doubleClickInputs[3].U.mi.dwFlags = upFlag;

                    InputTools.SendInput(4, doubleClickInputs, Marshal.SizeOf(typeof(INPUT)));
                }
                return true;
            }

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = InputTools.INPUT_MOUSE;

            switch (button)
            {
                case 1: // Click
                    inputs[0].U.mi.dwFlags = (state == 0) ? InputTools.MOUSEEVENTF_LEFTDOWN : InputTools.MOUSEEVENTF_LEFTUP;
                    break;
                case 2: // Right click
                    inputs[0].U.mi.dwFlags = (state == 0) ? InputTools.MOUSEEVENTF_RIGHTDOWN : InputTools.MOUSEEVENTF_RIGHTUP;
                    break;
                case 4: // Middle click
                    inputs[0].U.mi.dwFlags = (state == 0) ? InputTools.MOUSEEVENTF_MIDDLEDOWN : InputTools.MOUSEEVENTF_MIDDLEUP;
                    break;
                case 8: // Mouse wheel
                    short delta = BitConverter.ToInt16(data, 2);
                    inputs[0].U.mi.dwFlags = InputTools.MOUSEEVENTF_WHEEL;
                    inputs[0].U.mi.mouseData = delta;
                    break;
                default:
                    return false;
            }

            InputTools.SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            return true;
        }

        private bool HandleKeyboard(byte[] data)
        {
            if (data.Length < 3)
                return false;

            byte key = data[1];
            if (key == 255)
                return false;

            byte flag = data[2];
            uint dwFlags = (flag == 1) ? InputTools.KEYEVENTF_KEYUP : 0;

            if (key == 91 || key == 27 || key == 16 || key == 17)
            {
                dwFlags |= InputTools.KEYEVENTF_EXTENDEDKEY;
            }

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = InputTools.INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = key;
            inputs[0].U.ki.dwFlags = dwFlags;

            InputTools.SendInput(1, inputs, INPUT.Size);
            return true;
        }

        private bool HandleClipboardText(byte[] data)
        {
            if (data.Length < 5)
                return false;

            try
            {
                var length = BitConverter.ToInt32(data, 1);
                var memory = new byte[length];
                Buffer.BlockCopy(data, 5, memory, 0, length);
                var text = Encoding.Unicode.GetString(memory);

                if (string.IsNullOrEmpty(text))
                    return false;

                var thread = new Thread(() =>
                {
                    try
                    {
                        Clipboard.SetText(text);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Error in Clipboard.SetText: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(1000);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Failed HandleClipboardText: {ex.Message}");
                return false;
            }
        }

        private bool HandleClipboardFiles(byte[] data)
        {
            if (data.Length < 5)
                return false;

            try
            {
                var length = BitConverter.ToInt32(data, 1);
                var memory = new byte[length];
                Buffer.BlockCopy(data, 5, memory, 0, length);
                var text = Encoding.Unicode.GetString(memory);

                if (string.IsNullOrEmpty(text))
                    return false;

                var files = JsonSerializer.Deserialize<System.Collections.Specialized.StringCollection>(text);

                var thread = new Thread(() =>
                {
                    try
                    {
                        Clipboard.SetFileDropList(files);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Error in Clipboard.SetText: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(1000);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Faild HandleClipboardFiles:  {ex.Message}");
                return false;
            }
        }
    }
}