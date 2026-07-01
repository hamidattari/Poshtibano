using Poshtibano.Common;
using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Services.Connection;
using Poshtibano.Desk.Shared.Settings;
using Poshtibano.Desk.Shared.Tools;

namespace Poshtibano.Desk
{
    partial class MainForm
    {
        private void AttachSessionEvents()
        {
            if (_session == null)
                return;

            _session.OnHubStateChanged += Session_OnHubStateChanged;
            _session.OnPeerStateChanged += Session_OnPeerStateChanged;
            _session.OnSessionReady += Session_OnSessionReady;

            _session.OnChatMessage += Session_OnChatMessage;
            _session.OnChatEvent += Session_OnChatEvent;
            _session.OnFileProgress += Session_OnFileProgress;
            _session.OnFileReceived += Session_OnFileReceived;
            _session.OnTransferCancelled += Session_OnTransferCancelled;
            _session.OnError += Session_OnError;

            // ✅ NEW: Attach handshake events
            _session.OnRequestPasswordInfo += Session_OnRequestPasswordInfo;
            _session.OnRequestPassword += Session_OnRequestPassword;
            _session.OnPasswordIncorrect += Session_OnPasswordIncorrect;
            _session.OnPasswordCorrect += Session_OnPasswordCorrect;
            _session.OnRequestAccessPermission += Session_OnRequestAccessPermission;
            _session.OnAccessDenied += Session_OnAccessDenied;
            _session.OnSessionEnded += Session_OnSessionEnded;

            _session.OnVerifyPassword += Session_OnVerifyPassword;
            _session.OnChangeRoleRequest += Session_OnChaneRoleRequest;

            // ✅ MULTI-MONITOR events
            AttachMonitorSessionEvents();

            // ✅ Mouse on process events
            _session.OnMouseOnProcess += Session_OnMouseOnProcess;
            _session.OnMouseDeniedOnProcess += Session_OnMouseDeniedOnProcess;
            _session.OnRemoteMouseActionDenied += Session_OnRemoteMouseActionDenied;

            _session.OnInvitationRequest += Session_OnInvitationRequest;
            _session.OnInvitationResponse += Session_OnInvitationResponse;

            // ✅ NEW: Clipboard sharing events
            _session.OnClipboardRemoteTextReceived += Session_OnClipboardRemoteTextReceived;
            _session.OnClipboardRemoteFilesReceived += Session_OnClipboardRemoteFilesReceived;
            _session.OnClipboardStatusChanged += Session_OnClipboardStatusChanged;
            _session.OnClipboardError += Session_OnClipboardError;

            // ✅ NEW: Clipboard file offer event
            _session.OnClipboardFileOfferReceived += Session_OnClipboardFileOfferReceived;

            // ✅ NEW: Attach Audio/Video session events
            AttachAudioVideoSessionEvents();
        }

        private void DetachSessionEvents()
        {
            if (_session == null)
                return;

            _session.OnHubStateChanged -= Session_OnHubStateChanged;
            _session.OnPeerStateChanged -= Session_OnPeerStateChanged;
            _session.OnSessionReady -= Session_OnSessionReady;
            _session.OnChatMessage -= Session_OnChatMessage;
            _session.OnChatEvent -= Session_OnChatEvent;
            _session.OnFileProgress -= Session_OnFileProgress;
            _session.OnFileReceived -= Session_OnFileReceived;
            _session.OnTransferCancelled -= Session_OnTransferCancelled;
            _session.OnError -= Session_OnError;

            // ✅ NEW: Detach handshake events
            _session.OnRequestPasswordInfo -= Session_OnRequestPasswordInfo;
            _session.OnRequestPassword -= Session_OnRequestPassword;
            _session.OnPasswordIncorrect -= Session_OnPasswordIncorrect;
            _session.OnPasswordCorrect -= Session_OnPasswordCorrect;
            _session.OnRequestAccessPermission -= Session_OnRequestAccessPermission;
            _session.OnAccessDenied -= Session_OnAccessDenied;
            _session.OnSessionEnded -= Session_OnSessionEnded;

            _session.OnVerifyPassword -= Session_OnVerifyPassword;
            _session.OnChangeRoleRequest -= Session_OnChaneRoleRequest;

            // ✅ MULTI-MONITOR events
            DetachMonitorSessionEvents();

            // ✅ Mouse on process events
            _session.OnMouseOnProcess -= Session_OnMouseOnProcess;
            _session.OnMouseDeniedOnProcess -= Session_OnMouseDeniedOnProcess;
            _session.OnRemoteMouseActionDenied -= Session_OnRemoteMouseActionDenied;

            _session.OnInvitationRequest -= Session_OnInvitationRequest;
            _session.OnInvitationResponse -= Session_OnInvitationResponse;

            // ✅ NEW: Clipboard sharing events
            _session.OnClipboardRemoteTextReceived -= Session_OnClipboardRemoteTextReceived;
            _session.OnClipboardRemoteFilesReceived -= Session_OnClipboardRemoteFilesReceived;
            _session.OnClipboardStatusChanged -= Session_OnClipboardStatusChanged;
            _session.OnClipboardError -= Session_OnClipboardError;

            _session.OnClipboardFileOfferReceived -= Session_OnClipboardFileOfferReceived;

            // ✅ NEW:  Detach Audio/Video session events
            DetachAudioVideoSessionEvents();
        }

        private void Session_OnMonitorListReceived(List<MonitorInfo> remoteMonitors)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnMonitorListReceived(remoteMonitors));
                return;
            }

            if (_currentRole == ClientRole.Controller)
            {
                UpdateMonitorsFromRemote(remoteMonitors);
                Console.WriteLine($"[{DateTime.Now}] ✅ Controller: Monitor buttons updated");
            }
            else if (_currentRole == ClientRole.Agent)
            {
                Console.WriteLine($"[{DateTime.Now}] ℹ️ Agent: Remote list saved (UI doesn't change)");
            }
        }

        private void Session_OnMonitorStatusUpdateReceived(int monitorIndex, bool isActive)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnMonitorStatusUpdateReceived(monitorIndex, isActive));
                return;
            }

            if (monitorIndex >= 0 && monitorIndex < _localMonitors.Count)
            {
                _localMonitors[monitorIndex].IsActive = isActive;
                RefreshMonitorButton(monitorIndex);

                Console.WriteLine($"[{DateTime.Now}] 📥 #{monitorIndex} monitor status changed: {(isActive ? "active" : "inactive")}");
            }
        }

        private void Session_OnMonitorChangedReceived(int monitorIndex)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnMonitorChangedReceived(monitorIndex));
                return;
            }

            _activeMonitorIndex = monitorIndex;
            _monitor = _localMonitors[_activeMonitorIndex];

            HighlightActiveMonitorButton();

            Console.WriteLine($"[{DateTime.Now}] 📥 Active remote monitor: #{monitorIndex}");
        }

        private void Session_OnMonitorSelectReceived(int monitorIndex)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnMonitorSelectReceived(monitorIndex));
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📥 Request to select monitor #{monitorIndex}");

            if (monitorIndex >= 0 && monitorIndex < _localMonitors.Count && _localMonitors[monitorIndex].IsActive)
            {
                _activeMonitorIndex = monitorIndex;
                _monitor = _localMonitors[_activeMonitorIndex];
                HighlightActiveMonitorButton();
                RefreshAllMonitorButtons();

                Console.WriteLine($"[{DateTime.Now}] ✅ monitor #{monitorIndex} selected");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ monitor #{monitorIndex} is not valid");
            }
        }

        // ✅ NEW: Handshake event handlers
        private void Session_OnRequestPasswordInfo()
        {
            Console.WriteLine($"[{DateTime.Now}] 📋 Agent:  Server requesting password info");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnRequestPasswordInfo());
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    // ✅ Agent checks if it has a password configured and send it
                    bool hasPassword = !string.IsNullOrEmpty(SettingsManager.Instance.Password);

                    Console.WriteLine($"[{DateTime.Now}] 📤 Agent:  Sending password info (hasPassword={hasPassword})");
                    await _session.SendPasswordInfoAsync(hasPassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error sending password info: {ex.Message}");
                }
            });
        }

        private async void Session_OnChaneRoleRequest(ClientRole role)
        {
            Console.WriteLine($"{role} -------------------------------------------");
            _currentRole = role;
            _session.ChangeRole(role);
        }

        private void Session_OnVerifyPassword(string password)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 Agent:  Received password for verification");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnVerifyPassword(password));
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    // ✅ Compare password with Agent's password
                    bool isCorrect = (password == SettingsManager.Instance.Password);

                    Console.WriteLine($"[{DateTime.Now}] 🔐 Agent: Password verification - isCorrect={isCorrect}");

                    // ✅ Send verification result to server
                    await _session.SendPasswordVerificationAsync(isCorrect);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error verifying password: {ex.Message}");
                }
            });
        }

        private void Session_OnRequestPassword(string caller, string sessionId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 Controller: Server requesting password");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnRequestPassword(caller, sessionId));
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    Invoke(() =>
                    {
                        // ✅ Show password input dialog
                        var passwordForm = new PasswordMessageForm("", $"🔒 رمزعبور برای {sessionId.ReverseSessioId()} ");

                        TopMost = true;
                        Activate();
                        TopMost = false;
                        passwordForm.BringToFront();
                        if (passwordForm.ShowDialog(this) == DialogResult.OK)
                        {
                            string password = passwordForm.Password;

                            // ✅ Send password to server
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    Console.WriteLine($"[{DateTime.Now}] 📤 Controller: Sending password");
                                    await _session.SendPasswordAsync(password);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[{DateTime.Now}] ❌ Error sending password: {ex.Message}");
                                }
                            });
                        }
                        else
                        {
                            guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

                            Start();
                            //// ✅ User cancelled, disconnect
                            //_ = Task.Run(async () => await FullShutdownAsync());
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error in password dialog: {ex.Message}");
                }
            });
        }


        private void Session_OnPasswordCorrect()
        {
            Console.WriteLine($"[{DateTime.Now}] ✅ Controller: Password correct");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnPasswordCorrect());
                return;
            }

            UpdateConnectionStatusUI(ConnectionStatus.PasswordCorrect);
        }

        private async void Session_OnPasswordIncorrect()
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ Controller: Password incorrect");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnPasswordIncorrect());
                return;
            }

            UpdateConnectionStatusUI(ConnectionStatus.PasswordIncorrect);

            var form = new MessageBoxForm("رمز عبور نادرست است");
            Invoke(() =>
            {
                TopMost = true;
                Activate();
                TopMost = false;
                form.BringToFront();
                form.ShowDialog(this);
            });

            guidFormattedTextBoxIdRemote.CopyOnly = false;
            guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

            await Start();
            //_ = Task.Run(async () =>
            //{
            //    await FullShutdownSessionAsync();
            //});
        }

        private async Task FullShutdownSessionAsync()
        {
            if (_session != null)
            {
                Console.WriteLine($"[{DateTime.Now}] 🛑 Performing full session cleanup due to Auth Failure...");

                // ✅ NEW: Disable Audio/Video buttons first
                Invoke(() => DisableAudioVideoButtons());

                DetachSessionEvents();
                if (_currentRole == ClientRole.Controller)
                {
                    DetachControllerUIHandlers();
                }

                await _session.DisconnectAsync(keepHubAlive: false);
                _session.Dispose();
                _session = null;

                Invoke(() =>
                {
                    UpdateConnectionStatusUI(ConnectionStatus.Disconnected);
                    labelState.Text = "قطع اتصال";
                    buttonConnect.Enabled = true;
                    buttonConnect.Visible = true;
                    buttonDisconnect.Visible = false;
                });

                Console.WriteLine($"[{DateTime.Now}] ✅ Full session cleanup completed.");
            }
        }

        private void Session_OnRequestAccessPermission(string caller, string sessionId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 Agent: Server requesting access permission");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnRequestAccessPermission(caller, sessionId));
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    Invoke(() =>
                    {
                        bool isAllowed = false;

                        if (!_inInvitation)
                        {
                            // ✅ Show access request form
                            var form = new AccessRequestForm($"{caller} : {sessionId.ReverseSessioId()}");
                            Invoke(() =>
                            {
                                TopMost = true;
                                Activate();
                                TopMost = false;
                                form.ShowInTaskbar = true;
                                form.ShowIcon = true;
                                form.BringToFront();
                                var result = form.ShowDialog(this);
                                isAllowed = form.AllowAccess;
                            });
                        }
                        else isAllowed = true;

                        // ✅ Send response to server
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Console.WriteLine($"[{DateTime.Now}] 📤 Agent: Sending access response (allowed={isAllowed})");
                                await _session.SendAccessResponseAsync(isAllowed);

                                _caller = caller;
                                _callerSessionId = sessionId;

                                guidFormattedTextBoxIdRemote.Text = _callerSessionId;
                                SettingsManager.CallerName = _caller;
                                SettingsManager.SessionName = _callerSessionId;

                                if (!isAllowed)
                                {
                                    guidFormattedTextBoxIdRemote.CopyOnly = false;
                                    guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

                                    SettingsManager.CallerName = string.Empty;
                                    SettingsManager.SessionName = string.Empty;
                                    _callerSessionId = string.Empty;
                                    _caller = string.Empty;
                                    await Start();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending access response:  {ex.Message}");
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error in access request:  {ex.Message}");
                }
            });
        }

        private async void Session_OnAccessDenied()
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ Controller: Access denied");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnAccessDenied());
                return;
            }

            UpdateConnectionStatusUI(ConnectionStatus.AccessDenied);

            var form = new MessageBoxForm("درخواست دسترسی شما رد شد");
            Invoke(() =>
            {
                guidFormattedTextBoxIdRemote.CopyOnly = false;
                guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

                TopMost = true;
                Activate();
                TopMost = false;
                form.BringToFront();
                form.ShowDialog(this);
            });

            await Start();
        }

        private async void Session_OnSessionEnded(string reason)
        {
            Console.WriteLine($"[{DateTime.Now}] 📢 Session ended");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnSessionEnded(reason));
                return;
            }

            guidFormattedTextBoxIdRemote.CopyOnly = false;
            guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

            if (reason == "manualDisconnect" || reason == "peerDisconnected" || reason == "reconnected")
            {
                await Start();
            }
        }

        private void Session_OnHubStateChanged(ConnectionStatus state)
        {
            Invoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] Hub state changed: {state}");
            });
        }

        private void Session_OnPeerStateChanged(ConnectionStatus state)
        {
            Invoke(() =>
            {
                if (_currentRole == ClientRole.Agent)
                {
                    switch (state)
                    {
                        case ConnectionStatus.Disconnected:
                            Console.WriteLine($"[{DateTime.Now}] 🔄 Agent peer disconnected, waiting for new Controller..  .");
                            UpdateConnectionStatusUI(ConnectionStatus.Disconnected);
                            labelState.Text = "اتصال قطع شد... ";
                            labelState.ForeColor = Color.Orange;
                            buttonConnect.Enabled = false;
                            buttonDisconnect.Enabled = true;
                            buttonConnect.Visible = false;
                            buttonDisconnect.Visible = true;

                            CleanupChatAndFile();

                            break;

                        case ConnectionStatus.Failed:
                            Console.WriteLine($"[{DateTime.Now}] ⚠️ Agent peer failed (timeout), waiting for Controller.. .");
                            UpdateConnectionStatusUI(ConnectionStatus.Disconnected);
                            labelState.Text = "اتصال قطع شد...  ";
                            labelState.ForeColor = Color.Orange;
                            buttonConnect.Enabled = false;
                            buttonDisconnect.Enabled = true;
                            buttonConnect.Visible = false;
                            buttonDisconnect.Visible = true;

                            CleanupChatAndFile();

                            break;

                        case ConnectionStatus.Connecting:
                            UpdateConnectionStatusUI(state);
                            labelState.Text = $"اتصال به {_callerSessionId.ReverseSessioId()}...";
                            labelState.ForeColor = Color.FromArgb(241, 196, 15);
                            break;

                        case ConnectionStatus.Connected:
                            UpdateConnectionStatusUI(state);
                            labelState.Text = $"متصل به {_callerSessionId.ReverseSessioId()}";
                            labelState.ForeColor = Color.FromArgb(46, 204, 113);
                            buttonDisconnect.Enabled = true;

                            break;
                    }
                }
                else
                {
                    UpdateConnectionStatusUI(state);

                    switch (state)
                    {
                        case ConnectionStatus.Disconnected:
                        case ConnectionStatus.Failed:
                            break;
                    }
                }
            });
        }

        private void Session_OnSessionReady()
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnSessionReady());
                return;
            }

            CleanupChatAndFile();

            Console.WriteLine($"[{DateTime.Now}] ✅ Session ready (MainForm handler)");

            _accessGranted = true;
            InitializeWebcamAndMicrophoneAvailbilityOnSessionReady();

            if (_currentRole == ClientRole.Controller)
            {
                UpdateConnectionStatusUI(ConnectionStatus.AccessGranted);

                SetupControllerUI();
                //await _session.ReconnectControllerAsync(pictureBox, this);
            }
            else if (_currentRole == ClientRole.Agent)
            {
                InitializeMonitorsOnSessionReady();

                UpdateConnectionStatusUI(ConnectionStatus.SessionReady);
            }
        }

        private void CleanupChatAndFile()
        {
            Invoke(() =>
            {
                _fileTrnsferInProgress = false;
                _inCancelingFileTransfer = false;

                buttonCancelTransfer.Visible = false;
                progressBarTransfer.Visible = false;

                progressBarTransfer.Value = 0;

                labelTransferStatus.Text = string.Empty;
                labelTransferStatus.Visible = false;
                labelTransferStatus.ForeColor = Color.Gray;

                buttonUploadFile.Visible = true;

                flowlayoutpanelChatHistory.Controls.Clear();
                flowlayoutpanelChatHistory.Controls.Add(buttonUploadFile);
            });
        }

        private void Session_OnError(string error)
        {
            Invoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] Session error: {error}");
            });
        }
    }
}