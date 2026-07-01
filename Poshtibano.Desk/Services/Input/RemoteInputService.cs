using Poshtibano.Common;
using Poshtibano.Desk.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace Poshtibano.Desk.Services.Input
{
    public class RemoteInputService : IDisposable
    {
        private readonly ClientRole _localRole;
        private readonly SynchronizationContext _uiContext;
        private KeyboardHook _keyboardHook;
        private HashSet<Keys> _pressedKeys = new HashSet<Keys>();
        private DateTime _lastMouseSend = DateTime.MinValue;
        private Rectangle _renderRect = Rectangle.Empty;
        private Control _targetControl;
        private Form _parentForm;
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private const int MouseThrottleMs = 50;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public event Action<byte[]> OnInputEventGenerated;

        public Rectangle RenderRect
        {
            get => _renderRect;
            set => _renderRect = value;
        }

        public RemoteInputService(ClientRole localRole, Control targetControl = null)
        {
            _localRole = localRole;
            _uiContext = SynchronizationContext.Current;
            _targetControl = targetControl;
        }

        public void InitializeForController(Form parentForm, Control pictureBox)
        {
            if (_localRole != ClientRole.Controller)
            {
                //throw new InvalidOperationException("Remote input only for Controller role");
            }

            if (_isInitialized)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ RemoteInputService already initialized, cleaning up first");
                Cleanup(parentForm);
            }

            parentForm.Invoke((MethodInvoker)(() =>
            {
                try
                {
                    _parentForm = parentForm;
                    _targetControl = pictureBox;

                    // Setup keyboard hook
                    if (_keyboardHook == null)
                    {
                        _keyboardHook = new KeyboardHook(parentForm);
                        _keyboardHook.KeyDown += OnKeyDown;
                        _keyboardHook.KeyUp += OnKeyUp;

                        try
                        {
                            _keyboardHook.Start();
                            Console.WriteLine($"[{DateTime.Now}] ✅ Keyboard hook started");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] ❌ Failed to start keyboard hook: {ex.Message}");
                            _keyboardHook = null;
                        }
                    }

                    // Setup clipboard monitoring
                    try
                    {
                        AddClipboardFormatListener(parentForm.Handle);
                        Console.WriteLine($"[{DateTime.Now}] ✅ Clipboard listener added");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ Failed to add clipboard listener: {ex.Message}");
                    }

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error initializing RemoteInputService: {ex.Message}");
                    Cleanup(parentForm);
                    throw;
                }
            }));
        }

        public void Cleanup(Form parentForm)
        {
            if (!_isInitialized && _keyboardHook == null)
                return;

            Console.WriteLine($"[{DateTime.Now}] 🧹 Cleaning up RemoteInputService");

            try
            {
                // Stop keyboard hook
                if (_keyboardHook != null)
                {
                    try
                    {
                        _keyboardHook.KeyDown -= OnKeyDown;
                        _keyboardHook.KeyUp -= OnKeyUp;
                        _keyboardHook.Stop();
                        _keyboardHook.Dispose();
                        Console.WriteLine($"[{DateTime.Now}] ✅ Keyboard hook stopped and disposed");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ Error stopping keyboard hook: {ex.Message}");
                    }
                    finally
                    {
                        _keyboardHook = null;
                    }
                }

                // Remove clipboard listener
                try
                {
                    if (parentForm != null && !parentForm.IsDisposed)
                    {
                        RemoveClipboardFormatListener(parentForm.Handle);
                        Console.WriteLine($"[{DateTime.Now}] ✅ Clipboard listener removed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Error removing clipboard listener: {ex.Message}");
                }

                _pressedKeys.Clear();
                _isInitialized = false;
                _parentForm = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error in Cleanup: {ex.Message}");
            }
        }

        public void HandleMouseMove(MouseEventArgs e)
        {
            if (_renderRect.IsEmpty || _renderRect.Width == 0 || _renderRect.Height == 0)
                return;

            if (!_renderRect.Contains(e.Location))
                return;

            if ((DateTime.Now - _lastMouseSend).TotalMilliseconds < MouseThrottleMs)
                return;

            float nx = (float)(e.X - _renderRect.X) / _renderRect.Width;
            float ny = (float)(e.Y - _renderRect.Y) / _renderRect.Height;

            nx = Math.Max(0, Math.Min(1, nx));
            ny = Math.Max(0, Math.Min(1, ny));

            var data = new byte[9];
            data[0] = 0; // Mouse move event type
            Array.Copy(BitConverter.GetBytes(nx), 0, data, 1, 4);
            Array.Copy(BitConverter.GetBytes(ny), 0, data, 5, 4);

            _lastMouseSend = DateTime.Now;
            OnInputEventGenerated?.Invoke(data);
        }

        public void HandleMouseDown(MouseEventArgs e)
        {
            if (!_renderRect.Contains(e.Location))
                return;

            byte button = e.Button switch
            {
                MouseButtons.Left => 1,
                MouseButtons.Right => 2,
                MouseButtons.Middle => 4,
                _ => 0
            };

            if (button != 0)
            {
                // تشخیص دابل کلیک و ارسال استیت 2 به جای 0
                if (e.Clicks >= 2)
                {
                    SendMouseButton(button, 2); // 2 = Double Click State
                }
                else
                {
                    SendMouseButton(button, 0); // 0 = Down State
                }
            }
        }

        public void HandleMouseUp(MouseEventArgs e)
        {
            byte button = e.Button switch
            {
                MouseButtons.Left => 1,
                MouseButtons.Right => 2,
                MouseButtons.Middle => 4,
                _ => 0
            };

            if (button != 0)
            {
                SendMouseButton(button, 1); // 1 = Up State
            }
        }

        public void HandleMouseWheel(MouseEventArgs e, Point clientPosition)
        {
            if (_targetControl == null || !_targetControl.ClientRectangle.Contains(clientPosition))
                return;

            var data = new byte[4];
            data[0] = 1; // Mouse button event type
            data[1] = 8; // Wheel
            BitConverter.GetBytes((short)e.Delta).CopyTo(data, 2);

            OnInputEventGenerated?.Invoke(data);
        }

        // تغییر استیت بولین به بایت برای پشتیبانی از دابل کلیک
        private void SendMouseButton(byte button, byte state)
        {
            var data = new byte[3];
            data[0] = 1; // Mouse button event type
            data[1] = button;
            data[2] = state; // 0=Down, 1=Up, 2=DoubleClick

            OnInputEventGenerated?.Invoke(data);
        }

        private bool ShouldSuppressKey(Keys key)
        {
            if (_targetControl?.FindForm()?.ActiveControl is TextBox tb)
            {
                if (tb.Name == "textBoxChatInput" || tb.Multiline || tb == _targetControl?.FindForm()?.ActiveControl)
                {
                    if (key == Keys.Enter || key == Keys.Return)
                        return true;
                }
            }

            return key == Keys.Enter && IsChatInputFocused();
        }

        private bool IsChatInputFocused()
        {
            var form = _targetControl?.FindForm();
            if (form == null) return false;

            var active = form.ActiveControl;
            return active != null &&
                   (active.GetType() == typeof(RichTextBox) && ((RichTextBox)active).Multiline);
        }

        private void OnKeyDown(object sender, Keys e)
        {
            if (ShouldSuppressKey(e))
                return;
            if (!_pressedKeys.Contains(e))
            {
                _pressedKeys.Add(e);
                byte keyByte = (byte)e;
                if (keyByte == 255) return;

                var data = new byte[] { 2, keyByte, 0 }; // Key event type, key, down
                OnInputEventGenerated?.Invoke(data);
            }
        }

        private void OnKeyUp(object sender, Keys e)
        {
            if (_pressedKeys.Contains(e))
            {
                _pressedKeys.Remove(e);
                byte keyByte = (byte)e;
                if (keyByte == 255) return;

                var data = new byte[] { 2, keyByte, 1 }; // Key event type, key, up
                OnInputEventGenerated?.Invoke(data);
            }
        }

        public void HandleClipboardUpdate()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    var memory = Encoding.Unicode.GetBytes(text);
                    var data = new byte[5 + memory.Length];
                    data[0] = 3; // Clipboard text event type
                    BitConverter.GetBytes(memory.Length).CopyTo(data, 1);
                    memory.CopyTo(data, 5);

                    OnInputEventGenerated?.Invoke(data);
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    var text = JsonSerializer.Serialize(files);
                    var memory = Encoding.Unicode.GetBytes(text);
                    var data = new byte[5 + memory.Length];
                    data[0] = 4; // Clipboard files event type
                    BitConverter.GetBytes(memory.Length).CopyTo(data, 1);
                    memory.CopyTo(data, 5);

                    OnInputEventGenerated?.Invoke(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Clipboard error: {ex.Message}");
            }
        }

        public void SetKeyboardSuppression(bool suppress)
        {
            if (_keyboardHook != null)
            {
                _keyboardHook.SuppressWhenFormIsForeground = suppress;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            Console.WriteLine($"[{DateTime.Now}] 🗑️ Disposing RemoteInputService");

            Cleanup(_parentForm);

            _pressedKeys.Clear();
            OnInputEventGenerated = null;
        }
    }
}