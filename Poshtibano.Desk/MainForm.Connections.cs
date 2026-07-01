using Poshtibano.Common;
using Poshtibano.Desk.Controls;
using Poshtibano.Desk.Models;
using Poshtibano.Desk.Services;
using Poshtibano.Desk.Services.Connection;
using Poshtibano.Desk.Shared.Settings;
using Poshtibano.Desk.Shared.Tools;

namespace Poshtibano.Desk
{
    partial class MainForm
    {
        private void LoadRecentConnections()
        {
            if (InvokeRequired)
            {
                Invoke(() => LoadRecentConnections());
                return;
            }

            flowLayoutPanelRecentConnections.Controls.Clear();

            var recentConnections = _recentConnectionsManager.GetRecentConnections();

            if (recentConnections.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "هنوز اتصالی ثبت نشده است",
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 10F),
                    AutoSize = true,
                    Padding = new Padding(10)
                };
                flowLayoutPanelRecentConnections.Controls.Add(emptyLabel);
                return;
            }

            foreach (var connection in recentConnections)
            {
                var control = new RecentConnectionControl
                {
                    Connection = connection,
                    Width = flowLayoutPanelRecentConnections.ClientSize.Width - 20
                };

                control.OnConnectClicked += async (conn) => await ConnectToRecent(conn);
                control.OnRenameClicked += RenameConnection;
                control.OnDeleteClicked += DeleteConnection;

                flowLayoutPanelRecentConnections.Controls.Add(control);
            }
        }

        private async Task ConnectToRecent(RecentConnection connection)
        {
            try
            {
                _currentRole = connection.Role;
                guidFormattedTextBoxIdRemote.Text = connection.SessionId;
                var sessionId = connection.SessionId;

                await Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در اتصال:  {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenameConnection(RecentConnection connection)
        {
            var inputForm = new Form
            {
                Text = "تغییر نام",
                Size = new Size(350, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            var textBox = new TextBox
            {
                Text = connection.DisplayName,
                Location = new Point(20, 20),
                Size = new Size(290, 25)
            };

            var buttonOk = new Button
            {
                Text = "تایید",
                DialogResult = DialogResult.OK,
                Location = new Point(150, 60),
                Size = new Size(75, 30)
            };

            var buttonCancel = new Button
            {
                Text = "لغو",
                DialogResult = DialogResult.Cancel,
                Location = new Point(235, 60),
                Size = new Size(75, 30)
            };

            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(buttonOk);
            inputForm.Controls.Add(buttonCancel);
            inputForm.AcceptButton = buttonOk;
            inputForm.CancelButton = buttonCancel;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                string newName = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    _recentConnectionsManager.RenameConnection(connection, newName);
                }
            }
        }

        private void DeleteConnection(RecentConnection connection)
        {
            var result = MessageBox.Show(
                $"آیا مطمئن هستید که می‌خواهید '{connection.DisplayName}' را حذف کنید؟",
                "تایید حذف",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _recentConnectionsManager.RemoveConnection(connection.Id);
            }
        }

        private void buttonClearRecentConnections_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "آیا مطمئن هستید که می‌خواهید تمام اتصالات اخیر را پاک کنید؟",
                "تایید پاک‌سازی",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                _recentConnectionsManager.ClearAll();
            }
        }

        #region Connection Management

        private async void buttonConnect_Click(object sender, EventArgs e)
        {
            if (guidFormattedTextBoxIdRemote.Text == guidFormattedTextBoxIdLocal.Text) return;

            if (_isConnecting)
                return;

            _isConnecting = true;

            _currentRole = ClientRole.Controller;
            await ConnectAsController(guidFormattedTextBoxIdRemote.Text);

            SettingsManager.SessionName = guidFormattedTextBoxIdRemote.Text;

            _isConnecting = false;
        }

        private async void buttonDisconnect_Click(object sender, EventArgs e)
        {
            guidFormattedTextBoxIdRemote.CopyOnly = false;
            guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

            await _session.SendSessionEnd("manualDisconnect");
            await Start();
        }

        private async Task ConnectAsController(string sessionId)
        {
            await FullShutdownAsync();

            try
            {
                // Reset handshake state
                _passwordVerified = false;
                _accessGranted = false;

                //buttonConnect.Enabled = false;
                //buttonDisconnect.Enabled = false;
                //buttonConnect.Visible = false;
                //buttonDisconnect.Visible = false;

                guidFormattedTextBoxIdRemote.Enabled = true;

                UpdateConnectionStatusUI(ConnectionStatus.Disconnected);

                _session = new SessionCoordinator(_currentRole, sessionId, _signalingUrl);
                AttachSessionEvents();

                await _session.InitializeAsync(SettingsManager.Instance.DisplayName, _callerSessionId);
                _session.Initialize(monitor: _monitor, renderTarget: pictureBox, parentForm: this);
                SetupControllerUI();
                SetupAgentUI();

                Console.WriteLine($"[{DateTime.Now}] ✅ Connection initiated successfully (waiting for handshake)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Connection error: {ex.Message}");
                MessageBox.Show($"اتصال ناموفق: {ex.Message}", "خطای اتصال", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await FullShutdownAsync();
            }
        }

        private async Task ConnectAsync(string sessionId)
        {
            await FullShutdownAsync();

            try
            {
                // Reset handshake state
                _passwordVerified = false;
                _accessGranted = false;

                //buttonConnect.Enabled = false;
                //buttonDisconnect.Enabled = false;
                //buttonConnect.Visible = false;
                //buttonDisconnect.Visible = false;

                guidFormattedTextBoxIdRemote.ReadOnly = true;

                UpdateConnectionStatusUI(ConnectionStatus.Disconnected);

                _session = new SessionCoordinator(_currentRole, sessionId, _signalingUrl);
                AttachSessionEvents();

                await _session.InitializeAsync(SettingsManager.Instance.DisplayName, _callerSessionId);
                _session.Initialize(monitor: _monitor, renderTarget: pictureBox, parentForm: this);
                SetupControllerUI();
                SetupAgentUI();

                Console.WriteLine($"[{DateTime.Now}] ✅ Connection initiated successfully (waiting for handshake)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Connection error: {ex.Message}");
                MessageBox.Show($"اتصال ناموفق: {ex.Message}", "خطای اتصال", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await FullShutdownAsync();
            }
        }

        private async Task DisconnectAsync()
        {
            if (InvokeRequired)
                await Invoke(DisconnectAsync);

            if (_isDisconnecting)
                return;

            _isDisconnecting = true;

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🔌 MainForm disconnecting (Role: {_currentRole})...");

                buttonConnect.Enabled = false;
                buttonDisconnect.Enabled = false;
                buttonConnect.Visible = false;
                buttonDisconnect.Visible = false;

                if (_session != null)
                {
                    ClearMonitorButtons();
                    CleanupAudioVideoUI();

                    if (_currentRole == ClientRole.Agent)
                    {
                        await _session.DisconnectAsync(keepHubAlive: true);
                        Console.WriteLine($"[{DateTime.Now}] ✅ Agent disconnected (Hub still alive)");

                        Invoke(() =>
                        {
                            UpdateConnectionStatusUI(ConnectionStatus.Disconnected);
                            labelState.Text = "در انتظار پشتیبان... ";
                            buttonConnect.Enabled = false;
                            buttonDisconnect.Enabled = true;
                            buttonConnect.Visible = false;
                            buttonDisconnect.Visible = false;
                        });
                    }
                    else
                    {
                        DetachControllerUIHandlers();

                        FullShutdownAsync();
                        //await _session.DisconnectAsync(keepHubAlive: true);
                        Console.WriteLine($"[{DateTime.Now}] ✅ Controller disconnected (Hub still alive)");

                        flowlayoutpanelChatHistory.Controls.Clear();

                        Invoke(() =>
                        {
                            UpdateConnectionStatusUI(ConnectionStatus.Disconnected);
                            labelState.Text = "قطع اتصال";
                            buttonConnect.Enabled = true;
                            buttonDisconnect.Enabled = false;
                            buttonConnect.Visible = true;
                            buttonDisconnect.Visible = false;
                        });
                    }
                }

                Console.WriteLine($"[{DateTime.Now}] ✅ MainForm disconnected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Disconnect error: {ex.Message}");
            }
            finally
            {
                _isDisconnecting = false;
            }
        }

        private async Task FullShutdownAsync()
        {
            if (_session != null)
            {
                Console.WriteLine($"[{DateTime.Now}] 🛑 {_currentRole} full shutdown...");

                DetachSessionEvents();
                await _session.DisconnectAsync(keepHubAlive: false);
                _session?.Dispose();
                _session = null;
                _inInvitation = false;
                _inviterId = Guid.Empty;

                ClearMonitorButtons();
                CleanupAudioVideoUI();

                UpdateConnectionStatusUI(ConnectionStatus.Disconnected);

                Console.WriteLine($"[{DateTime.Now}] ✅ {_currentRole} fully shutdown");
            }
        }

        private void buttonShutdown_Click(object sender, EventArgs e)
        {
            if (_currentRole == ClientRole.Agent)
            {
                var result = MessageBox.Show(
                    "آیا مطمئن هستید که می‌خواهید عامل را کاملاً خاموش کنید؟\nاین کار اتصال هاب را می‌بندد.",
                    "خاموش کردن عامل",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    _ = FullShutdownAsync();
                }
            }
        }

        #endregion
    }
}