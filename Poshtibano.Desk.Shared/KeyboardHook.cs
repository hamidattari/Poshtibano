using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Poshtibano.Desk.Shared
{
    public class KeyboardHook : IDisposable
    {

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static KeyboardHook _currentInstance;

        public event EventHandler<Keys> KeyDown;
        public event EventHandler<Keys> KeyUp;

        public bool SuppressWhenFormIsForeground { get; set; } = false;

        private static bool showMessages = false;

        private IntPtr _targetTopLevelHandle = IntPtr.Zero;
        private Form _targetForm;

        public KeyboardHook(Form targetForm)
        {
            if (_currentInstance != null)
                throw new InvalidOperationException("Only one instance of KeyboardHook is allowed.");

            _currentInstance = this;
            SetTargetForm(targetForm);
            _targetForm = targetForm;
        }
        public void SetTargetForm(Form form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            _targetTopLevelHandle = GetAncestor(form.Handle, GA_ROOT);
        }

        public void Start()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(currentModule.ModuleName), 0);
                if (_hookID == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Exception($"Failed to set keyboard hook. Error code: {errorCode}");
                }
                if (showMessages) Console.WriteLine("Keyboard hook started successfully.");
            }
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                if (showMessages) Console.WriteLine("Keyboard hook stopped.");
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _currentInstance != null)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                bool handled = false;
                
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    handled = _currentInstance.ShouldSuppressNow();
                    if (handled)
                    {
                        _currentInstance.KeyDown?.Invoke(_currentInstance, key);
                        if (showMessages) Console.WriteLine($"Hook KeyDown: {key} (Value: {vkCode})");
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    handled = _currentInstance.ShouldSuppressNow();
                    if (handled)
                    {
                        _currentInstance.KeyUp?.Invoke(_currentInstance, key);
                        if (showMessages) Console.WriteLine($"Hook KeyUp: {key} (Value: {vkCode})");
                    }
                }

                if (handled)
                {
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool ShouldSuppressNow()
        {
            if (!SuppressWhenFormIsForeground) return false;
            if (_targetTopLevelHandle == IntPtr.Zero) return false;

            if (!_targetForm.Visible || _targetForm.WindowState == FormWindowState.Minimized)
                return false;

            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            IntPtr fgRoot = GetAncestor(fg, GA_ROOT);
            return fgRoot == _targetTopLevelHandle;
        }


        #region WinAPI

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const uint GA_ROOT = 2;
        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        #endregion

        #region IDisposable
        public void Dispose()
        {
            Stop();
            _currentInstance = null;
            GC.SuppressFinalize(this);
        }
        ~KeyboardHook()
        {
            Dispose();
        }
        #endregion
    }
}
