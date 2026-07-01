using Poshtibano.Common;
using Poshtibano.Desk.Controls;
using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Services.Connection;
using Poshtibano.Desk.Shared.Settings;
using Poshtibano.Desk.Shared.Tools;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Poshtibano.Desk
{
    partial class MainForm
    {
        private const int BorderThickness = 3;
        private readonly Color BorderColor = Color.FromArgb(255, 128, 0);

        private void ApplyModernStyles()
        {
            DoubleBuffered = true;

            ResizeRedraw = true;
            Padding = new Padding(BorderThickness);

            panelChat.Padding = new Padding(BorderThickness);

            Paint += MainForm_Paint;
            panelChat.Paint += PanelChat_Paint;
            Resize += MainForm_Resize;

            MinimumSize = new Size(800, 450);
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            int minWidth = 800;
            int minHeight = 450;

            bool needsFixing = false;
            int newWidth = Width;
            int newHeight = Height;

            if (Width < minWidth)
            {
                newWidth = minWidth;
                needsFixing = true;
            }

            if (Height < minHeight)
            {
                newHeight = minHeight;
                needsFixing = true;
            }

            if (needsFixing)
            {
                Size = new Size(newWidth, newHeight);
            }

            if (_currentRole == ClientRole.Controller)
            {
                if (pictureBox.Visible)
                {
                    pictureBox.Invalidate();
                }
            }
        }

        private void PanelChat_Paint(object? sender, PaintEventArgs e)
        {
            using (Pen borderPen = new Pen(BorderColor, BorderThickness))
            {
                borderPen.Alignment = PenAlignment.Inset;

                // Drawing the upper side (from the upper-left corner to the upper-right corner)
                e.Graphics.DrawLine(borderPen, 0, 0, Width, 0);

                // Draw the right side (from the top-left corner to the bottom-left corner)
                e.Graphics.DrawLine(borderPen, 0, Height, 0, 0);

                //Rectangle rect = new Rectangle(0, 0, panelChat.Width, panelChat.Height);
                //e.Graphics.DrawRectangle(borderPen, rect);
            }
        }

        private void MainForm_Paint(object? sender, PaintEventArgs e)
        {
            using (Pen borderPen = new Pen(BorderColor, BorderThickness))
            {
                borderPen.Alignment = PenAlignment.Inset;

                Rectangle rect = new Rectangle(0, 0, Width, Height);
                e.Graphics.DrawRectangle(borderPen, rect);
            }
        }

        private async void UpdateConnectionStatusUI(ConnectionStatus state)
        {
            _lastState = state;

            if (InvokeRequired)
            {
                Invoke(() => UpdateConnectionStatusUI(state));
                return;
            }

            Color statusColor;
            string statusText;

            switch (state)
            {
                case ConnectionStatus.Connected:
                    statusColor = Color.Green;
                    statusText = "متصل";

                    var sessionId = _inInvitation ? _inviterId.GuidToFormattedText() : guidFormattedTextBoxIdRemote.Text;

                    string displayName = $"{sessionId}";
                    _recentConnectionsManager.AddOrUpdateConnection(
                        guidFormattedTextBoxIdRemote.GuidValue,
                        displayName,
                        _currentRole,
                        sessionId
                    );

                    break;
                case ConnectionStatus.Connecting:
                case ConnectionStatus.Reconnecting:
                    if (_currentRole == ClientRole.Agent)
                    {
                        statusColor = Color.Gray;
                        statusText = "آماده (در انتظار پشتیبان)";
                    }
                    else
                    {
                        statusColor = Color.YellowGreen;
                        statusText = state == ConnectionStatus.Connecting ? "در حال اتصال..." : "در حال اتصال مجدد...";
                    }
                    break;
                case ConnectionStatus.Failed:
                    ClearMonitorButtons();

                    if (_currentRole == ClientRole.Agent && _session != null)
                    {
                        statusColor = Color.Gray;
                        statusText = "آماده (در انتظار پشتیبان)";
                    }
                    else
                    {
                        statusColor = Color.Red;
                        statusText = "ناموفق";
                    }
                    break;
                case ConnectionStatus.Disconnected:
                default:
                    ClearMonitorButtons();
                    CleanupAudioVideoUI();

                    statusColor = Color.Gray;
                    statusText = (_currentRole == ClientRole.Agent && _session != null)
                        ? "آماده (در انتظار پشتیبان)"
                        : "آماده برای اتصال";
                    break;
                case ConnectionStatus.AccessDenied:
                    statusColor = Color.Red;
                    statusText = "درخواست دسترسی شما رد شد";
                    break;
                case ConnectionStatus.AccessGranted:
                    statusColor = Color.YellowGreen;
                    statusText = "اجازه داده شد در حال اتصال ...";
                    break;
                case ConnectionStatus.PasswordCorrect:
                    statusColor = Color.YellowGreen;
                    statusText = "در حال ارسال درخواست اجازه دسترسی...";
                    break;
                case ConnectionStatus.PasswordIncorrect:
                    statusColor = Color.Red;
                    statusText = "رمز عبور نادرست است...";
                    break;
                case ConnectionStatus.WaitingPermission:
                    statusColor = Color.Yellow;
                    statusText = "در حال ارسال درخواست اجازه دسترسی...";
                    break;
                case ConnectionStatus.SessionReady:
                    statusText = $"{_callerSessionId} پیوست، آماده‌سازی...  ";
                    statusColor = Color.YellowGreen;
                    break;
            }

            labelStateIndicator.ForeColor = statusColor;
            labelState.Text = statusText;
            labelState.ForeColor = statusColor;

            //if (_currentRole == ClientRole.Agent)
            //{
            switch (state)
            {
                case ConnectionStatus.Failed:
                case ConnectionStatus.Disconnected:
                    buttonInvite.Visible = true;
                    break;
                case ConnectionStatus.Connecting:
                case ConnectionStatus.AccessDenied:
                    buttonInvite.Visible = true;
                    break;
                case ConnectionStatus.Reconnecting:
                case ConnectionStatus.AccessGranted:
                case ConnectionStatus.PasswordCorrect:
                case ConnectionStatus.PasswordIncorrect:
                case ConnectionStatus.WaitingPermission:
                case ConnectionStatus.SessionReady:
                case ConnectionStatus.Connected:
                    buttonInvite.Visible = false;
                    break;
            }
            //}

            if (state == ConnectionStatus.Connected)
            {
                buttonChat.Visible = true;
                buttonAudioVideoSettings.Visible = true;

                //buttonMaximize.Visible = true;

                if (_currentRole == ClientRole.Controller)
                {
                    panelDashboard.Visible = false;
                    pictureBox.Visible = true;
                    buttonSettings.Visible = true;
                    buttonToggleBar.Visible = true;
                }
                else
                {
                    panelDashboard.Visible = true;
                    pictureBox.Visible = false;
                    buttonSettings.Visible = true;
                    buttonToggleBar.Visible = true;
                }
            }
            else
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    buttonMaximize.Text = "⬜";
                }

                this.Size = new Size(1025, 600);
                panelDashboard.Visible = true;
                pictureBox.Visible = false;
                panelControllerSettingsCard.Visible = false;
                panelConnectionBar.Visible = true;
                buttonToggleBar.Text = "🔼";

                //buttonMaximize.Visible = false;
                buttonChat.Visible = false;
                buttonAudioVideoSettings.Visible = false;
                panelAudioVideoSettings.Visible = false;

                buttonSettings.Visible = true;
                buttonToggleBar.Visible = false;
                panelChat.Visible = false;
            }

            if (_currentRole == ClientRole.Agent)
            {
                buttonSettings.Visible = true;
                panelControllerSettingsCard.Visible = false;

                //buttonConnect.Enabled = (_session == null);
                buttonConnect.Enabled = (state == ConnectionStatus.Disconnected || state == ConnectionStatus.Failed);
                buttonDisconnect.Enabled = (_session != null);

                //buttonConnect.Visible = (_session == null);
                buttonConnect.Visible = (state == ConnectionStatus.Disconnected || state == ConnectionStatus.Failed);
                buttonDisconnect.Visible = (_session != null);

                //guidFormattedTextBoxId.Enabled = (_session == null);
                guidFormattedTextBoxIdRemote.CopyOnly = !buttonConnect.Enabled;
            }
            else
            {
                buttonConnect.Enabled = (state == ConnectionStatus.Disconnected || state == ConnectionStatus.Failed);
                buttonDisconnect.Enabled = (state == ConnectionStatus.Connected || state == ConnectionStatus.Connecting);

                buttonConnect.Visible = (state == ConnectionStatus.Disconnected || state == ConnectionStatus.Failed);
                buttonDisconnect.Visible = (state == ConnectionStatus.Connected || state == ConnectionStatus.Connecting);

                guidFormattedTextBoxIdRemote.CopyOnly = !buttonConnect.Enabled;
            }
        }

        private void MakeLabelClickable(Label label, string url)
        {
            label.ForeColor = Color.Blue;
            label.Font = new Font(label.Font, FontStyle.Underline);
            label.Cursor = Cursors.Hand;

            label.MouseEnter += (s, e) => label.ForeColor = Color.Red;
            label.MouseLeave += (s, e) => label.ForeColor = Color.Blue;

            label.Click += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            };
        }

        #region Window Management

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void buttonMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
                buttonMaximize.Text = "❐";
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                buttonMaximize.Text = "⬜";
            }
            pictureBox.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE && _currentRole == ClientRole.Controller && _session != null)
            {
                _session.HandleClipboardUpdate();
            }

            if (m.Msg == WM_DROPFILES)
            {
                HandleFileDrop(m.WParam);
                return;
            }

            if (m.Msg == WM_NCHITTEST)
            {
                if (this.WindowState != FormWindowState.Maximized)
                {
                    const int border = 6;
                    Point cursor = PointToClient(Cursor.Position);

                    if (cursor.Y < border)
                    {
                        if (cursor.X < border)
                            m.Result = (IntPtr)HTTOPLEFT;
                        else if (cursor.X > this.Width - border)
                            m.Result = (IntPtr)HTTOPRIGHT;
                        else
                            m.Result = (IntPtr)HTTOP;
                    }
                    else if (cursor.Y > this.Height - border)
                    {
                        if (cursor.X < border)
                            m.Result = (IntPtr)HTBOTTOMLEFT;
                        else if (cursor.X > this.Width - border)
                            m.Result = (IntPtr)HTBOTTOMRIGHT;
                        else
                            m.Result = (IntPtr)HTBOTTOM;
                    }
                    else if (cursor.X < border)
                    {
                        m.Result = (IntPtr)HTLEFT;
                    }
                    else if (cursor.X > this.Width - border)
                    {
                        m.Result = (IntPtr)HTRIGHT;
                    }
                    else
                    {
                        base.WndProc(ref m);
                    }
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        #endregion

        #region UI Buttons

        private void buttonToggleBar_Click(object sender, EventArgs e)
        {
            panelConnectionBar.Visible = !panelConnectionBar.Visible;
            buttonToggleBar.Text = panelConnectionBar.Visible ? "🔼" : "🔽";
            pictureBox.Invalidate();
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            switch (_lastState)
            {
                case ConnectionStatus.Connected:
                    if (_currentRole == ClientRole.Agent)
                    {
                        panelAgentSettingsCard.Visible = !panelAgentSettingsCard.Visible;
                        if (panelAgentSettingsCard.Visible)
                            panelAgentSettingsCard.BringToFront();
                    }
                    else if (_currentRole == ClientRole.Controller)
                    {
                        panelControllerSettingsCard.Visible = !panelControllerSettingsCard.Visible;
                        if (panelControllerSettingsCard.Visible)
                            panelControllerSettingsCard.BringToFront();
                    }
                    break;
                case ConnectionStatus.Disconnected:
                case ConnectionStatus.Connecting:
                case ConnectionStatus.Reconnecting:
                case ConnectionStatus.AccessDenied:
                case ConnectionStatus.AccessGranted:
                case ConnectionStatus.PasswordCorrect:
                case ConnectionStatus.PasswordIncorrect:
                case ConnectionStatus.WaitingPermission:
                case ConnectionStatus.Failed:
                case ConnectionStatus.SessionReady:
                    panelAgentSettingsCard.Visible = !panelAgentSettingsCard.Visible;
                    if (panelAgentSettingsCard.Visible)
                        panelAgentSettingsCard.BringToFront();
                    break;
            }

        }

        private void buttonCloseAudioVideoSettings_Click(object sender, EventArgs e)
        {
            panelAudioVideoSettings.Visible = !panelAudioVideoSettings.Visible;
            if (panelAudioVideoSettings.Visible)
                panelAudioVideoSettings.BringToFront();
        }

        private void buttonAudioVideoSettings_Click(object sender, EventArgs e)
        {
            panelAudioVideoSettings.Visible = !panelAudioVideoSettings.Visible;
            if (panelAudioVideoSettings.Visible)
                panelAudioVideoSettings.BringToFront();
        }

        private void buttonChat_Click(object sender, EventArgs e)
        {
            panelChat.Visible = !panelChat.Visible;

            if (_currentRole == ClientRole.Controller)
                pictureBox.Invalidate();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            if (_session != null && _session.IsConnected)
            {
                long newQuality = (long)numericUpDownQuality.Value;
                float newFps = (float)numericUpDownFps.Value;
                bool fullframe = checkBoxFullFrame.Checked;
                _ = _session.SendSettingsAsync(newQuality, newFps, fullframe);
            }

            buttonSettings_Click(sender, e);
        }

        private void buttonAgentPasswordChange_Click(object sender, EventArgs e)
        {
            var passwordForm = new PasswordMessageForm(SettingsManager.Instance.Password);
            Invoke(() =>
            {
                TopMost = true;
                Activate();
                TopMost = false;
                passwordForm.BringToFront();
                if (passwordForm.ShowDialog(this) == DialogResult.OK)
                {
                    string password = passwordForm.Password;
                    SettingsManager.Instance.SetPassword(password);
                }
            });

        }

        private void buttonDisplayNameChange_Click(object sender, EventArgs e)
        {
            var displayNameForm = new DisplayNameForm(SettingsManager.Instance.DisplayName);

            Invoke(() =>
            {
                TopMost = true;
                Activate();
                TopMost = false;
                displayNameForm.BringToFront();
                if (displayNameForm.ShowDialog(this) == DialogResult.OK)
                {
                    string name = displayNameForm.DisplayName;
                    SettingsManager.Instance.SetDisplayName(name);
                    buttonDisplayName.Text = SettingsManager.Instance.DisplayName;
                }
            });

        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine(new string('\n', 100));
                Console.WriteLine("--- Log Cleared ---");
                Console.Clear();
            }
            catch
            {
                Console.WriteLine(new string('\n', 100));
            }

            var folder = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var file in Directory.GetFiles(Path.Combine(folder, "log")))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                }
            }
        }

        private void PanelHeader_DoubleClick(object sender, EventArgs e)
        {
            buttonMaximize_Click(sender, e);
        }
        #endregion
    }
}