using Poshtibano.Common;
using Poshtibano.Desk.Shared.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Poshtibano.Desk
{
    public partial class MainForm
    {
        private List<MonitorInfo> _localMonitors = new List<MonitorInfo>();
        private int _activeMonitorIndex = 0;
        private MonitorInfo _monitor;
        private readonly Dictionary<int, Button> _monitorButtons = new Dictionary<int, Button>();

        private void InitializeAndSendMonitorsToCoordinator(bool createButtons = false)
        {
            Console.WriteLine($"[{DateTime.Now}] 🖥️ Scan all screens");

            _localMonitors.Clear();
            var screens = Screen.AllScreens;

            for (int index = 0; index < screens.Length; index++)
            {
                var screen = screens[index];
                var monitorInfo = new MonitorInfo
                {
                    Scale = screen.GetScalingFactor(),
                    Index = index,
                    Name = screen.DeviceName.TrimStart('\\', '.'),
                    ScreenBounds = screen.GetPhysicalBound(),
                    IsPrimary = screen.Primary,
                    IsActive = true
                };
                _localMonitors.Add(monitorInfo);

                Console.WriteLine($"[{DateTime.Now}] ✅ Monitor #{index}:  {monitorInfo.Name} ({monitorInfo.ScreenBounds.Width}x{monitorInfo.ScreenBounds.Height})");
            }

            _activeMonitorIndex = _localMonitors.FindIndex(m => m.IsPrimary);
            if (_activeMonitorIndex < 0)
                _activeMonitorIndex = 0;

            _monitor = _localMonitors[_activeMonitorIndex];

            if (_session != null)
            {
                if (_currentRole == ClientRole.Agent)
                {
                    _session.SetLocalMonitors(_localMonitors, _activeMonitorIndex);
                    _ = _session.SendMonitorListToRemote();
                }
            }


            if (createButtons) BuildMonitorButtons();
        }

        private void AttachMonitorSessionEvents()
        {
            _session.OnMonitorListRemoteReceived += Session_OnMonitorListReceived;
            _session.OnMonitorStatusUpdateRemoteReceived += Session_OnMonitorStatusUpdateReceived;
            _session.OnMonitorChangedRemoteReceived += Session_OnMonitorChangedReceived;
            _session.OnMonitorSelectRemoteReceived += Session_OnMonitorSelectReceived;
        }

        private void DetachMonitorSessionEvents()
        {
            _session.OnMonitorListRemoteReceived -= Session_OnMonitorListReceived;
            _session.OnMonitorStatusUpdateRemoteReceived -= Session_OnMonitorStatusUpdateReceived;
            _session.OnMonitorChangedRemoteReceived -= Session_OnMonitorChangedReceived;
            _session.OnMonitorSelectRemoteReceived -= Session_OnMonitorSelectReceived;
        }


        private void BuildMonitorButtons()
        {
            foreach (var button in _monitorButtons.Values)
            {
                flowLayoutPanelAudioVideoSettings.Controls.Remove(button);
                button.Dispose();
            }
            _monitorButtons.Clear();

            if (_localMonitors.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ No Monitor founds");
                return;
            }

            int baseY = 5;
            int spacing = 95;

            for (int index = 0; index < _localMonitors.Count; index++)
            {
                var monitor = _localMonitors[index];

                var button = new Button
                {
                    Name = $"buttonMonitor{index}",
                    Tag = index,
                    Text = $"🖥️ صفحه {index + 1}",
                    Width = 80,
                    Height = 36,
                    Location = new Point(/*baseX - (index * spacing)*/0, baseY),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    AutoSize = false
                };

                UpdateMonitorButtonAppearance(button, index);

                button.Click += MonitorButton_Click;
                button.MouseDown += MonitorButton_MouseDown;
                button.MouseUp += MonitorButton_MouseUp;

                flowLayoutPanelAudioVideoSettings.Controls.Add(button);
                flowLayoutPanelAudioVideoSettings.Controls.SetChildIndex(button, 0);

                _monitorButtons[index] = button;

                Console.WriteLine($"[{DateTime.Now}] ✅ Button monitor #{index} has been created");
            }
        }

        public void ClearMonitorButtons()
        {
            Console.WriteLine($"[{DateTime.Now}] 🧹 Remove monitor buttons");

            foreach (var button in _monitorButtons.Values)
            {
                try
                {
                    flowLayoutPanelAudioVideoSettings.Controls.Remove(button);
                    button.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Error deleting button: {ex.Message}");
                }
            }

            _monitorButtons.Clear();
            _localMonitors.Clear();
            _activeMonitorIndex = 0;

            Console.WriteLine($"[{DateTime.Now}] ✅ All monitor buttons removed");
        }

        private void UpdateMonitorButtonAppearance(Button button, int monitorIndex)
        {
            if (monitorIndex < 0 || monitorIndex >= _localMonitors.Count)
                return;

            var monitor = _localMonitors[monitorIndex];
            bool isActive = monitorIndex == _activeMonitorIndex;

            if (isActive)
            {
                button.BackColor = Color.FromArgb(255, 128, 0);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Color.DarkOrange;
                button.FlatAppearance.BorderSize = 1;
            }
            else if (monitor.IsActive)
            {
                button.BackColor = Color.White;
                button.ForeColor = Color.Gray;
                button.FlatAppearance.BorderColor = Color.LightGray;
                button.FlatAppearance.BorderSize = 1;
            }
            else
            {
                button.BackColor = Color.LightGray;
                button.ForeColor = Color.DarkGray;
                button.FlatAppearance.BorderColor = Color.Gray;
                button.FlatAppearance.BorderSize = 1;
            }
        }

        private void MonitorButton_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int monitorIndex = (int)button.Tag;
            var monitor = _localMonitors[monitorIndex];
            if (!monitor.IsActive) return;

            if (_currentRole == ClientRole.Agent)
            {
                if (_session != null && monitorIndex < _localMonitors.Count && _localMonitors[monitorIndex].IsActive)
                {
                    _session.SwitchActiveMonitor(monitorIndex);
                    _activeMonitorIndex = monitorIndex;
                    _monitor = _localMonitors[_activeMonitorIndex];
                    HighlightActiveMonitorButton();
                    Console.WriteLine($"[{DateTime.Now}] ✅ Switch to monitor #{monitorIndex}");
                }
            }
            else if (_currentRole == ClientRole.Controller)
            {
                if (_session != null && monitorIndex < _session.RemoteMonitors.Count)
                {
                    var remoteMonitor = _session.RemoteMonitors[monitorIndex];
                    if (remoteMonitor.IsActive)
                    {
                        _session.RequestMonitorSelect(monitorIndex);
                        Console.WriteLine($"[{DateTime.Now}] 📤 Request for monitor #{monitorIndex}");
                    }
                }
            }
        }

        private void MonitorButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (_localMonitors.Count <= 1) return;
            
            if (_currentRole != ClientRole.Agent) return;
            if (e.Button != MouseButtons.Right) return;

            var button = sender as Button;
            if (button == null) return;

            int monitorIndex = (int)button.Tag;

            if (monitorIndex < 0 || monitorIndex >= _localMonitors.Count)
                return;

            var monitor = _localMonitors[monitorIndex];

            if (_localMonitors.Count(m => m.IsActive == true) <= 1)
            {
                if (monitor.IsActive)
                {
                    return;
                }
            }

            var contextMenu = new ContextMenuStrip();
            contextMenu.Font = new Font("Segoe UI", 10F);
            contextMenu.RightToLeft = RightToLeft.Yes;
            contextMenu.BackColor = Color.White;

            var toggleItem = new ToolStripMenuItem
            {
                Text = monitor.IsActive ? "❌ غیرفعال کردن" : "✅ فعال کردن",
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            toggleItem.Click += (s, ev) =>
            {
                if (_session != null)
                {
                    bool wasActive = _localMonitors[monitorIndex].IsActive;
                    bool isCurrentlySelected = (monitorIndex == _activeMonitorIndex);

                    _session.ToggleMonitorActive(monitorIndex);

                    if (wasActive && isCurrentlySelected)
                    {
                        int firstActiveIndex = -1;
                        for (int i = 0; i < _localMonitors.Count; i++)
                        {
                            if (i != monitorIndex && _localMonitors[i].IsActive)
                            {
                                firstActiveIndex = i;
                                break;
                            }
                        }

                        if (firstActiveIndex >= 0)
                        {
                            _activeMonitorIndex = firstActiveIndex;
                            _monitor = _localMonitors[_activeMonitorIndex];
                            _session.SwitchActiveMonitor(firstActiveIndex);
                            Console.WriteLine($"[{DateTime.Now}] 🔄 Switched to monitor #{firstActiveIndex}");
                        }
                    }

                    BuildMonitorButtons();
                    Console.WriteLine($"[{DateTime.Now}] ✅ Monitor #{monitorIndex} toggled");
                }
            };
            contextMenu.Items.Add(toggleItem);

            contextMenu.Items.Add(new ToolStripSeparator());
            var infoItem = new ToolStripMenuItem
            {
                Text = $"📋 {monitor.Name}",
                Enabled = false,
                BackColor = Color.WhiteSmoke
            };
            contextMenu.Items.Add(infoItem);

            var detailsItem = new ToolStripMenuItem
            {
                Text = $"📐 {monitor.ScreenBounds.Width}x{monitor.ScreenBounds.Height}",
                Enabled = false,
                BackColor = Color.WhiteSmoke
            };
            contextMenu.Items.Add(detailsItem);

            contextMenu.Show(button, e.Location);
        }

        private void MonitorButton_MouseUp(object sender, MouseEventArgs e)
        {
            // Can be used for other events
        }

        private void HighlightActiveMonitorButton()
        {
            if (_monitorButtons.Count == 0) return;

            foreach (var kvp in _monitorButtons)
            {
                int index = kvp.Key;
                var button = kvp.Value;

                if (index < _localMonitors.Count)
                {
                    UpdateMonitorButtonAppearance(button, index);
                }
            }
        }

        private void UpdateMonitorsFromRemote(List<MonitorInfo> remoteMonitors)
        {
            _localMonitors = remoteMonitors;
            _activeMonitorIndex = _localMonitors.FindIndex(m => m.IsActive);
            if (_activeMonitorIndex < 0)
            {
                _activeMonitorIndex = 0;
                _monitor = _localMonitors[_activeMonitorIndex];
            }

            BuildMonitorButtons();
            Console.WriteLine($"[{DateTime.Now}] 📥 List {_localMonitors.Count} monitor received from remote peer");
        }

        public void InitializeMonitorsOnSessionReady()
        {
            Console.WriteLine($"[{DateTime.Now}] 🔄 Preparation of monitors after SessionReady");
            InitializeAndSendMonitorsToCoordinator(true);
        }

        public void RefreshMonitorButton(int monitorIndex)
        {
            if (monitorIndex < 0 || !_monitorButtons.ContainsKey(monitorIndex))
                return;

            var button = _monitorButtons[monitorIndex];
            UpdateMonitorButtonAppearance(button, monitorIndex);
        }

        public void RefreshAllMonitorButtons()
        {
            foreach (var kvp in _monitorButtons)
            {
                UpdateMonitorButtonAppearance(kvp.Value, kvp.Key);
            }
        }

        public bool IsMonitorActive(int monitorIndex)
        {
            return monitorIndex >= 0 && monitorIndex < _localMonitors.Count &&
                   _localMonitors[monitorIndex].IsActive;
        }

        public int GetActiveMonitorCount()
        {
            int count = 0;
            foreach (var m in _localMonitors)
            {
                if (m.IsActive) count++;
            }
            return count;
        }

        public MonitorInfo GetFirstActiveMonitor()
        {
            foreach (var m in _localMonitors)
            {
                if (m.IsActive) return m;
            }
            return null;
        }

        public void ShowMonitorDetails(int monitorIndex)
        {
            if (monitorIndex < 0 || monitorIndex >= _localMonitors.Count)
                return;

            var monitor = _localMonitors[monitorIndex];
            string details = $"🖥️ {monitor.Name}\n\n" +
                            $"شاخص: {monitor.Index}\n" +
                            $"ابعاد: {monitor.ScreenBounds.Width}x{monitor.ScreenBounds.Height}\n" +
                            $"موقعیت: ({monitor.ScreenBounds.X}, {monitor.ScreenBounds.Y})\n" +
                            $"اصلی: {(monitor.IsPrimary ? "بله" : "خیر")}\n" +
                            $"فعال: {(monitor.IsActive ? "بله" : "خیر")}";

            MessageBox.Show(details, "اطلاعات مانیتور", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void PrintLocalMonitorsList()
        {
            Console.WriteLine($"[{DateTime.Now}] ===== List of local monitors =====");
            for (int index = 0; index < _localMonitors.Count; index++)
            {
                var monitor = _localMonitors[index];
                Console.WriteLine($"[{DateTime.Now}] #{index}:  {monitor.Name}");
                Console.WriteLine($"         Dimensions: {monitor.ScreenBounds.Width}x{monitor.ScreenBounds.Height}");
                Console.WriteLine($"         Location: ({monitor.ScreenBounds.X}, {monitor.ScreenBounds.Y})");
                Console.WriteLine($"         IsPrimary: {monitor.IsPrimary}, IsActive: {monitor.IsActive}");
            }
            Console.WriteLine($"[{DateTime.Now}] ===================================");
        }

        public void PrintRemoteMonitorsList()
        {
            if (_session == null || _session.RemoteMonitors.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ No remote monitor available");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] ===== List of remote monitors =====");
            for (int i = 0; i < _session.RemoteMonitors.Count; i++)
            {
                var m = _session.RemoteMonitors[i];
                Console.WriteLine($"[{DateTime.Now}] #{i}: {m.Name}");
                Console.WriteLine($"         Dimensions: {m.ScreenBounds.Width}x{m.ScreenBounds.Height}");
                Console.WriteLine($"         Location: ({m.ScreenBounds.X}, {m.ScreenBounds.Y})");
                Console.WriteLine($"         IsPrimary: {m.IsPrimary}, IsActive:  {m.IsActive}");
            }
            Console.WriteLine($"[{DateTime.Now}] ===================================");
        }

        public void AgentSwitchMonitor(int monitorIndex)
        {
            if (_currentRole != ClientRole.Agent)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ This method is only for Agent");
                return;
            }

            if (_session == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session is not ready");
                return;
            }

            _session.SwitchActiveMonitor(monitorIndex);
        }

        public void ControllerRequestMonitorSwitch(int monitorIndex)
        {
            if (_currentRole != ClientRole.Controller)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️This method is only for Controller");
                return;
            }

            if (_session == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session is not ready");
                return;
            }

            _session.RequestMonitorSelect(monitorIndex);
        }

        public void AgentToggleMonitor(int monitorIndex)
        {
            if (_currentRole != ClientRole.Agent)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ This method is only for Agent");
                return;
            }

            if (_session == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session is not ready");
                return;
            }

            _session.ToggleMonitorActive(monitorIndex);
            BuildMonitorButtons();
        }

        public int GetMonitorCount()
        {
            return _localMonitors.Count;
        }

        public int GetCurrentMonitorIndex()
        {
            return _activeMonitorIndex;
        }

        public MonitorInfo GetCurrentMonitor()
        {
            if (_activeMonitorIndex >= 0 && _activeMonitorIndex < _localMonitors.Count)
                return _localMonitors[_activeMonitorIndex];
            return null;
        }
    }
}