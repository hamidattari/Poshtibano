using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Poshtibano.Desk.Services
{
    /// <summary>
    /// Monitors Windows clipboard changes using the AddClipboardFormatListener API.
    /// Fires an event when the clipboard content changes.
    /// </summary>
    public class ClipboardMonitor : NativeWindow, IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private bool _isDisposed = false;
        private bool _isListening = false;

        /// <summary>
        /// Fires when the clipboard content changes
        /// </summary>
        public event Action OnClipboardChanged;

        public ClipboardMonitor()
        {
            CreateHandle(new CreateParams());
        }

        /// <summary>
        /// Start listening for clipboard changes
        /// </summary>
        public void Start()
        {
            if (_isListening || _isDisposed) return;

            if (AddClipboardFormatListener(Handle))
            {
                _isListening = true;
                Console.WriteLine($"[{DateTime.Now}] 📋 ClipboardMonitor started");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                Console.WriteLine($"[{DateTime.Now}] ❌ ClipboardMonitor failed to start (error: {error})");
            }
        }

        /// <summary>
        /// Stop listening for clipboard changes
        /// </summary>
        public void Stop()
        {
            if (!_isListening || _isDisposed) return;

            RemoveClipboardFormatListener(Handle);
            _isListening = false;
            Console.WriteLine($"[{DateTime.Now}] 📋 ClipboardMonitor stopped");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                try
                {
                    OnClipboardChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ ClipboardMonitor WndProc error: {ex.Message}");
                }
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Stop();

            OnClipboardChanged = null;

            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }

            Console.WriteLine($"[{DateTime.Now}] 📋 ClipboardMonitor disposed");
        }
    }
}