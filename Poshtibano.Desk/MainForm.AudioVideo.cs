using Poshtibano.Common;
using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Services.Hardware;
using Poshtibano.Desk.Shared.Settings;

namespace Poshtibano.Desk
{
    public partial class MainForm
    {
        // ============================================================
        // UI CONTROLS
        // ============================================================

        private Button buttonMyMic;
        private Button buttonTheirMic;
        private Button buttonMyCam;
        private Button buttonTheirCam;
        private Button buttonMutePlayback;

        private ToolTip toolTip;

        private ContextMenuStrip micContextMenu;
        private ContextMenuStrip camContextMenu;

        private bool _avUIEventsAttached = false;


        // ============================================================
        // INITIALIZATION
        // ============================================================

        public async void InitializeWebcamAndMicrophoneAvailbilityOnSessionReady()
        {
            Console.WriteLine($"[{DateTime.Now}] 🔄 Preparation of Microphone and Webcam devices");
            var (hasMic, hasCam, _, _) = MediaDeviceDetector.GetDeviceSummary();
            await _session.SendWebcamAndMicrophoneAvailbilityToRemote(hasMic, hasCam);
        }

        private void InitializeAudioVideoUI(bool theyHaveMicrophone, bool theyHaveWebcam)
        {
            Console.WriteLine($"[{DateTime.Now}] 🎬 Initializing Audio/Video UI (4 buttons)");

            try
            {
                toolTip = new ToolTip
                {
                    AutoPopDelay = 5000,
                    InitialDelay = 500,
                    ReshowDelay = 200,
                    ShowAlways = true
                };

                var (hasMic, hasCam, _, _) = MediaDeviceDetector.GetDeviceSummary();
                CreateAudioVideoButtons(hasMic, hasCam, theyHaveMicrophone, theyHaveWebcam);
                AttachAudioVideoSessionEvents();
                SetupAudioVideoContextMenus();

                EnableAudioVideoButtons();

                Console.WriteLine($"[{DateTime.Now}] ✅ Audio/Video UI initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        private void SetupAudioVideoContextMenus()
        {
            // Mic Context Menu
            micContextMenu = new ContextMenuStrip();
            micContextMenu.Font = new Font("Segoe UI", 10F);
            micContextMenu.RightToLeft = RightToLeft.Yes;
            var micActiveItem = new ToolStripMenuItem("Active", null, async (s, e) =>
            {
                bool isActive = (bool)buttonMyMic.Tag;
                await _session.SetMyMicrophoneActiveAsync(!isActive);
                buttonMyMic.Tag = !isActive;

                if (!isActive)
                    buttonMyMic.Click -= ButtonMyMic_Click;
                else
                    buttonMyMic.Click += ButtonMyMic_Click;

                UpdateButtonState(buttonMyMic, !isActive ? MediaStreamState.Idle : MediaStreamState.Disabled, "🎙️");
            });

            micContextMenu.Opening += (sender, e) =>
            {
                bool isActive = (bool)buttonMyMic.Tag;
                micActiveItem.Text = isActive ? "❌ غیرفعال کردن" : "✅ فعال کردن";
            };

            micContextMenu.Items.Add(micActiveItem);
            if (buttonMyMic != null) buttonMyMic.ContextMenuStrip = micContextMenu;

            // Webcam Context Menu
            camContextMenu = new ContextMenuStrip();
            camContextMenu.Font = new Font("Segoe UI", 10F);
            camContextMenu.RightToLeft = RightToLeft.Yes;
            var camActiveItem = new ToolStripMenuItem("Active", null, async (s, e) =>
            {
                bool isActive = (bool)buttonMyCam.Tag;
                await _session.SetMyWebcamActiveAsync(!isActive);
                buttonMyCam.Tag = !isActive;

                if (!isActive)
                    buttonMyCam.Click -= ButtonMyCam_Click;
                else
                    buttonMyCam.Click += ButtonMyCam_Click;

                UpdateButtonState(buttonMyCam, !isActive ? MediaStreamState.Idle : MediaStreamState.Disabled, "📷");
            });

            camContextMenu.Opening += (sender, e) =>
            {
                bool isActive = (bool)buttonMyCam.Tag;
                camActiveItem.Text = isActive ? "❌ غیرفعال کردن" : "✅ فعال کردن";
            };

            camContextMenu.Items.Add(camActiveItem);
            if (buttonMyCam != null) buttonMyCam.ContextMenuStrip = camContextMenu;
        }

        private void CreateAudioVideoButtons(
            bool hasMicrophone, bool hasWebcam,
            bool theyHaveMicrophone, bool theyHaveWebcam,
            bool createMuteButton = false)
        {
            flowLayoutPanelMicAndWebcam.Controls.Clear();

            if (hasMicrophone)
            {
                buttonMyMic = CreateMediaButton("🎙️", "buttonMyMic", "Send My Audio");
                buttonMyMic.Click += ButtonMyMic_Click;
                buttonMyMic.Enabled = false;
                flowLayoutPanelMicAndWebcam.Controls.Add(buttonMyMic);
                toolTip.SetToolTip(buttonMyMic, "ارسال صدای شما به طرف مقابل");
            }

            if (theyHaveMicrophone)
            {
                buttonTheirMic = CreateMediaButton("👂", "buttonTheirMic", "Request Remote Audio");
                buttonTheirMic.Click += ButtonTheirMic_Click;
                buttonTheirMic.Enabled = false;
                flowLayoutPanelMicAndWebcam.Controls.Add(buttonTheirMic);
                toolTip.SetToolTip(buttonTheirMic, "شنیدن صدای طرف مقابل");
            }

            flowLayoutPanelMicAndWebcam.Controls.Add(CreateSeparator());

            if (hasWebcam)
            {
                buttonMyCam = CreateMediaButton("📷", "buttonMyCam", "Send My Webcam");
                buttonMyCam.Click += ButtonMyCam_Click;
                buttonMyCam.Enabled = false;
                flowLayoutPanelMicAndWebcam.Controls.Add(buttonMyCam);
                toolTip.SetToolTip(buttonMyCam, "ارسال وبکم شما به طرف مقابل");
            }

            if (theyHaveWebcam)
            {
                buttonTheirCam = CreateMediaButton("👁️", "buttonTheirCam", "Request Remote Webcam");
                buttonTheirCam.Click += ButtonTheirCam_Click;
                buttonTheirCam.Enabled = false;
                flowLayoutPanelMicAndWebcam.Controls.Add(buttonTheirCam);
                toolTip.SetToolTip(buttonTheirCam, "دیدن وبکم طرف مقابل");

                flowLayoutPanelMicAndWebcam.Controls.Add(CreateSeparator());
            }

            if (createMuteButton)
            {
                buttonMutePlayback = CreateMediaButton("🔊", "buttonMutePlayback", "Mute/Unmute");
                buttonMutePlayback.Click += ButtonMutePlayback_Click;
                buttonMutePlayback.Enabled = false;
                flowLayoutPanelMicAndWebcam.Controls.Add(buttonMutePlayback);
                toolTip.SetToolTip(buttonMutePlayback, "قطع/وصل صدای دریافتی");
            }

            Console.WriteLine($"[{DateTime.Now}] ✅ Audio/Video buttons created");
        }

        private Button CreateMediaButton(string emoji, string name, string accessibleName)
        {
            return new Button
            {
                Name = name,
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 14F),
                Size = new Size(40, 40),
                Margin = new Padding(3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Gray,
                Cursor = Cursors.Hand,
                AccessibleName = accessibleName,
                FlatAppearance = {
                    BorderSize = 1,
                    BorderColor = Color.Gray,
                    //MouseOverBackColor = Color.DarkOrange,
                    //MouseDownBackColor = Color.Salmon
                },
                Tag = true,
            };
        }

        private Label CreateSeparator()
        {
            return new Label
            {
                Text = "|",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(5, 12, 5, 0)
            };
        }

        // ============================================================
        // BUTTON CLICK HANDLERS
        // ============================================================

        private async void ButtonMyMic_Click(object sender, EventArgs e)
        {
            bool? allow = (bool?)((Button)sender).Tag;
            if (_session == null || !_session.IsConnected || !allow.Value) return;

            var state = _session.AVStateManager?.LocalAudioState ?? MediaStreamState.Idle;

            if (state == MediaStreamState.Streaming)
            {
                Console.WriteLine($"[{DateTime.Now}] 🎙️ Stopping MY audio");
                await _session.StopSendingMyAudioAsync();
                UpdateButtonState(buttonMyMic, MediaStreamState.Idle, "🎙️");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] 🎙️ Requesting to send MY audio");
                await _session.RequestToSendMyAudioAsync(SettingsManager.Instance.DisplayName);
                UpdateButtonState(buttonMyMic, MediaStreamState.WaitingPermission, "🎙️");
            }
        }

        private async void ButtonTheirMic_Click(object sender, EventArgs e)
        {
            if (_session == null || !_session.IsConnected) return;

            var state = _session.AVStateManager?.RemoteAudioState ?? MediaStreamState.Idle;

            if (state == MediaStreamState.Streaming)
            {
                Console.WriteLine($"[{DateTime.Now}] 👂 Stopping THEIR audio");
                await _session.StopReceivingTheirAudioAsync();
                UpdateButtonState(buttonTheirMic, MediaStreamState.Idle, "👂");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] 👂 Requesting THEIR audio");
                await _session.RequestTheirAudioAsync(SettingsManager.Instance.DisplayName);
                UpdateButtonState(buttonTheirMic, MediaStreamState.WaitingPermission, "👂");
            }
        }

        private async void ButtonMyCam_Click(object sender, EventArgs e)
        {
            if (_session == null || !_session.IsConnected) return;

            var state = _session.AVStateManager?.LocalWebcamState ?? MediaStreamState.Idle;

            if (state == MediaStreamState.Streaming)
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 Stopping MY webcam");
                await _session.StopSendingMyWebcamAsync();
                UpdateButtonState(buttonMyCam, MediaStreamState.Idle, "📷");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 Requesting to send MY webcam");
                await _session.RequestToSendMyWebcamAsync(SettingsManager.Instance.DisplayName);
                UpdateButtonState(buttonMyCam, MediaStreamState.WaitingPermission, "📷");
            }
        }

        private async void ButtonTheirCam_Click(object sender, EventArgs e)
        {
            if (_session == null || !_session.IsConnected) return;

            var state = _session.AVStateManager?.RemoteWebcamState ?? MediaStreamState.Idle;

            if (state == MediaStreamState.Streaming)
            {
                Console.WriteLine($"[{DateTime.Now}] 👁️ Stopping THEIR webcam");
                await _session.StopReceivingTheirWebcamAsync();
                UpdateButtonState(buttonTheirCam, MediaStreamState.Idle, "👁️");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] 👁️ Requesting THEIR webcam");
                await _session.RequestTheirWebcamAsync(SettingsManager.Instance.DisplayName);
                UpdateButtonState(buttonTheirCam, MediaStreamState.WaitingPermission, "👁️");
            }
        }

        private void ButtonMutePlayback_Click(object sender, EventArgs e)
        {
            if (_session == null) return;

            _session.ToggleAudioPlaybackMute();
            bool isMuted = _session.IsAudioPlaybackMuted;

            buttonMutePlayback.Text = isMuted ? "🔇" : "🔊";
            if (isMuted)
            {
                buttonMutePlayback.BackColor = Color.FromArgb(255, 128, 0);
                buttonMutePlayback.ForeColor = Color.Gray;
            }
            else
            {
                buttonMutePlayback.BackColor = Color.White;
                buttonMutePlayback.ForeColor = Color.Gray;
            }
        }

        // ============================================================
        // BUTTON STATE UPDATE
        // ============================================================

        private void UpdateButtonState(Button button, MediaStreamState state, string defaultEmoji)
        {
            if (button == null) return;
            bool? allow = (bool?)button.Tag;
            if (allow.Value) return;

            button.Text = defaultEmoji;

            switch (state)
            {
                case MediaStreamState.Idle:
                case MediaStreamState.Stopped:
                    button.ForeColor = Color.Gray;
                    button.BackColor = Color.White;
                    button.FlatAppearance.BorderColor = Color.Gray;
                    break;

                case MediaStreamState.WaitingPermission:
                case MediaStreamState.Requesting:
                    //button.BackColor = Color.Yellow;
                    button.ForeColor = Color.Gray;
                    button.BackColor = Color.White;
                    button.FlatAppearance.BorderColor = Color.Gray;
                    break;

                case MediaStreamState.Streaming:
                    button.ForeColor = Color.White;
                    button.BackColor = Color.FromArgb(255, 128, 0);
                    button.FlatAppearance.BorderColor = Color.DarkOrange;

                    //if (button == buttonMyMic) button.Text = "🎤";
                    //else if (button == buttonTheirMic) button.Text = "🔊";
                    //else if (button == buttonMyCam) button.Text = "📹";
                    //else if (button == buttonTheirCam) button.Text = "👀";
                    break;

                case MediaStreamState.Disabled:
                    button.BackColor = Color.LightGray;
                    button.ForeColor = Color.DarkGray;
                    button.FlatAppearance.BorderColor = Color.Gray;

                    //if (button == buttonMyMic) button.Text = "🎤";
                    //else if (button == buttonTheirMic) button.Text = "🔊";
                    //else if (button == buttonMyCam) button.Text = "📹";
                    //else if (button == buttonTheirCam) button.Text = "👀";
                    break;

                case MediaStreamState.Denied:
                    button.ForeColor = Color.White;
                    button.BackColor = Color.Red;
                    button.FlatAppearance.BorderColor = Color.DarkRed;
                    break;
            }
        }

        // ============================================================
        // SESSION EVENT HANDLERS
        // ============================================================

        private void AttachAudioVideoSessionEvents()
        {
            if (_session == null || _avUIEventsAttached) return;

            _session.OnTheyWantToSendAudio += HandleTheyWantToSendAudio;
            _session.OnTheyWantToSendWebcam += HandleTheyWantToSendWebcam;
            _session.OnTheyWantToReceiveMyAudio += HandleTheyWantToReceiveMyAudio;
            _session.OnTheyWantToReceiveMyWebcam += HandleTheyWantToReceiveMyWebcam;

            _session.OnMyAudioSendPermissionResponse += HandleMyAudioSendPermissionResponse;
            _session.OnMyWebcamSendPermissionResponse += HandleMyWebcamSendPermissionResponse;
            _session.OnTheirAudioPermissionResponse += HandleTheirAudioPermissionResponse;
            _session.OnTheirWebcamPermissionResponse += HandleTheirWebcamPermissionResponse;

            _session.OnLocalAudioStateChanged += HandleLocalAudioStateChanged;
            _session.OnLocalWebcamStateChanged += HandleLocalWebcamStateChanged;
            _session.OnRemoteAudioStateChanged += HandleRemoteAudioStateChanged;
            _session.OnRemoteWebcamStateChanged += HandleRemoteWebcamStateChanged;
            _session.OnTheirMicrophoneActiveChanged += HandleTheirMicrophoneActiveChanged;
            _session.OnTheirWebcamActiveChanged += HandleTheirWebcamActiveChanged;

            _session.OnTheirMicophoneAndWebcamAvailabilityReceived += HandleTheirMicophoneAndWebcamAvailabilityReceived;

            _avUIEventsAttached = true;
            Console.WriteLine($"[{DateTime.Now}] ✅ Audio/Video UI events attached");
        }

        private void DetachAudioVideoSessionEvents()
        {
            if (_session == null || !_avUIEventsAttached) return;

            _session.OnTheyWantToSendAudio -= HandleTheyWantToSendAudio;
            _session.OnTheyWantToSendWebcam -= HandleTheyWantToSendWebcam;
            _session.OnTheyWantToReceiveMyAudio -= HandleTheyWantToReceiveMyAudio;
            _session.OnTheyWantToReceiveMyWebcam -= HandleTheyWantToReceiveMyWebcam;

            _session.OnTheyWantToSendAudio -= HandleTheyWantToSendAudio;
            _session.OnTheyWantToSendWebcam -= HandleTheyWantToSendWebcam;
            _session.OnTheyWantToReceiveMyAudio -= HandleTheyWantToReceiveMyAudio;
            _session.OnTheyWantToReceiveMyWebcam -= HandleTheyWantToReceiveMyWebcam;
            _session.OnMyAudioSendPermissionResponse -= HandleMyAudioSendPermissionResponse;
            _session.OnMyWebcamSendPermissionResponse -= HandleMyWebcamSendPermissionResponse;
            _session.OnTheirAudioPermissionResponse -= HandleTheirAudioPermissionResponse;
            _session.OnTheirWebcamPermissionResponse -= HandleTheirWebcamPermissionResponse;

            _session.OnLocalAudioStateChanged -= HandleLocalAudioStateChanged;
            _session.OnLocalWebcamStateChanged -= HandleLocalWebcamStateChanged;
            _session.OnRemoteAudioStateChanged -= HandleRemoteAudioStateChanged;
            _session.OnRemoteWebcamStateChanged -= HandleRemoteWebcamStateChanged;
            _session.OnTheirMicrophoneActiveChanged -= HandleTheirMicrophoneActiveChanged;
            _session.OnTheirWebcamActiveChanged -= HandleTheirWebcamActiveChanged;

            _session.OnTheirMicophoneAndWebcamAvailabilityReceived -= HandleTheirMicophoneAndWebcamAvailabilityReceived;

            _avUIEventsAttached = false;
        }

        // ============================================================
        // PERMISSION REQUEST HANDLERS 
        // ============================================================

        private void HandleTheyWantToSendAudio(MediaPermissionRequest request)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 🎤 {request.RequesterName} wants to send THEIR audio to you");

                using (var dialog = new MediaPermissionRequestForm(MediaType.Audio, request.RequesterName, request.RequesterRole, true))
                {
                    TopMost = true;
                    Activate();
                    TopMost = false;

                    dialog.BringToFront();

                    dialog.SetMessage($"🎤 {request.RequesterName} می‌خواهد صدایش را برای شما بفرستد");
                    dialog.ShowDialog(this);
                    _ = _session?.RespondToTheirAudioSendRequestAsync(dialog.PermissionGranted);
                }
            });
        }

        private void HandleTheyWantToSendWebcam(MediaPermissionRequest request)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 {request.RequesterName} wants to send THEIR webcam to you");

                using (var dialog = new MediaPermissionRequestForm(MediaType.Webcam, request.RequesterName, request.RequesterRole, true))
                {
                    TopMost = true;
                    Activate();
                    TopMost = false;

                    dialog.BringToFront();

                    dialog.SetMessage($"📷 {request.RequesterName} می‌خواهد تصویر وبکم و صدا را برای شما بفرستد");
                    dialog.ShowDialog(this);
                    _ = _session?.RespondToTheirWebcamSendRequestAsync(dialog.PermissionGranted, SettingsManager.Instance.DisplayName);
                }
            });
        }

        private void HandleTheyWantToReceiveMyAudio(MediaPermissionRequest request)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 👂 {request.RequesterName} wants to hear YOUR audio");

                using (var dialog = new MediaPermissionRequestForm(MediaType.Audio, request.RequesterName, request.RequesterRole))
                {
                    dialog.SetMessage($"👂 {request.RequesterName} می‌خواهد صدای شما را بشنود");
                    dialog.ShowDialog(this);
                    _ = _session?.RespondToTheirReceiveMyAudioRequestAsync(dialog.PermissionGranted);
                }
            });
        }

        private void HandleTheyWantToReceiveMyWebcam(MediaPermissionRequest request)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 👁️ {request.RequesterName} wants to see YOUR webcam");

                using (var dialog = new MediaPermissionRequestForm(MediaType.Webcam, request.RequesterName, request.RequesterRole))
                {
                    dialog.SetMessage($"👁️ {request.RequesterName} می‌خواهد تصویر وبکم و صدای شما را دریافت کند");
                    dialog.ShowDialog(this);
                    _ = _session?.RespondToTheirReceiveMyWebcamRequestAsync(SettingsManager.Instance.DisplayName, dialog.PermissionGranted);
                }
            });
        }

        // ============================================================
        // PERMISSION RESPONSE HANDLERS
        // ============================================================

        private void HandleMyAudioSendPermissionResponse(bool allowed)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 🎙️ My audio send permission:  {allowed}");
                UpdateButtonState(buttonMyMic, allowed ? MediaStreamState.Streaming : MediaStreamState.Denied, "🎙️");

                if (!allowed)
                {
                    var form = new MessageBoxForm("درخواست دسترسی شما رد شد");
                    Invoke(() =>
                    {
                        TopMost = true;
                        Activate();
                        TopMost = false;
                        form.BringToFront();
                        form.ShowDialog(this);
                    });
                }
            });
        }

        private void HandleMyWebcamSendPermissionResponse(bool allowed)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 My webcam send permission: {allowed}");
                UpdateButtonState(buttonMyCam, allowed ? MediaStreamState.Streaming : MediaStreamState.Denied, "📷");

                if (!allowed)
                {
                    var form = new MessageBoxForm("درخواست دسترسی شما رد شد");
                    Invoke(() =>
                    {
                        TopMost = true;
                        Activate();
                        TopMost = false;
                        form.BringToFront();
                        form.ShowDialog(this);
                    });
                }
            });
        }

        private void HandleTheirAudioPermissionResponse(bool allowed)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 👂 Their audio permission:  {allowed}");
                UpdateButtonState(buttonTheirMic, allowed ? MediaStreamState.Streaming : MediaStreamState.Denied, "👂");
                if (allowed && buttonMutePlayback != null) buttonMutePlayback.Enabled = true;

                if (!allowed)
                {
                    var form = new MessageBoxForm("درخواست دسترسی شما رد شد");
                    Invoke(() =>
                    {
                        TopMost = true;
                        Activate();
                        TopMost = false;
                        form.BringToFront();
                        form.ShowDialog(this);
                    });
                }
            });
        }

        private void HandleLocalAudioStateChanged(MediaStreamState state)
        {
            SafeInvoke(() => UpdateButtonState(buttonMyMic, state, "🎙️"));
        }

        private void HandleLocalWebcamStateChanged(MediaStreamState state)
        {
            SafeInvoke(() => UpdateButtonState(buttonMyCam, state, "📷"));
        }

        private void HandleRemoteAudioStateChanged(MediaStreamState state)
        {
            SafeInvoke(() => UpdateButtonState(buttonTheirMic, state, "👂"));
        }

        private void HandleTheirMicophoneAndWebcamAvailabilityReceived(bool hasMicrophone, bool hasWebcam)
        {
            SafeInvoke(() => InitializeAudioVideoUI(hasMicrophone, hasWebcam));
        }

        private void HandleRemoteWebcamStateChanged(MediaStreamState state)
        {
            SafeInvoke(() => UpdateButtonState(buttonTheirCam, state, "👁️"));
        }

        private void HandleTheirMicrophoneActiveChanged(bool available)
        {
            SafeInvoke(() =>
            {
                buttonTheirMic.Enabled = available;
                UpdateButtonState(buttonTheirMic, available ? MediaStreamState.Idle : MediaStreamState.Disabled, "👂");
            });
        }

        private void HandleTheirWebcamActiveChanged(bool available)
        {
            SafeInvoke(() =>
            {
                buttonTheirCam.Enabled = available;
                UpdateButtonState(buttonTheirCam, available ? MediaStreamState.Idle : MediaStreamState.Disabled, "👁️");
            });
        }

        private void HandleTheirWebcamPermissionResponse(bool allowed)
        {
            SafeInvoke(() =>
            {
                Console.WriteLine($"[{DateTime.Now}] 👁️ Their webcam permission: {allowed}");
                UpdateButtonState(buttonTheirCam, allowed ? MediaStreamState.Streaming : MediaStreamState.Denied, "👁️");

                if (!allowed)
                {
                    var form = new MessageBoxForm("درخواست دسترسی شما رد شد");
                    Invoke(() =>
                    {
                        TopMost = true;
                        Activate();
                        TopMost = false;
                        form.BringToFront();
                        form.ShowDialog(this);
                    });
                }
            });
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        private void EnableAudioVideoButtons()
        {
            SafeInvoke(() =>
            {
                if (buttonMyMic != null) buttonMyMic.Enabled = true;
                if (buttonTheirMic != null) buttonTheirMic.Enabled = true;
                if (buttonMyCam != null) buttonMyCam.Enabled = true;
                if (buttonTheirCam != null) buttonTheirCam.Enabled = true;
                if (buttonMutePlayback != null) buttonMutePlayback.Enabled = true;
                Console.WriteLine($"[{DateTime.Now}] ✅ Audio/Video buttons enabled");
            });
        }

        private void DisableAudioVideoButtons()
        {
            SafeInvoke(() =>
            {
                //todo
                if (buttonMyMic != null) { buttonMyMic.Dispose(); buttonMyMic = null; }
                if (buttonTheirMic != null) { buttonTheirMic.Dispose(); buttonTheirMic = null; }
                if (buttonMyCam != null) { buttonMyCam.Dispose(); buttonMyCam = null; }
                if (buttonTheirCam != null) { buttonTheirCam.Dispose(); buttonTheirCam = null; }
                if (buttonMutePlayback != null) { buttonMutePlayback.Dispose(); buttonMutePlayback = null; }

                //if (buttonMyMic != null) { buttonMyMic.Enabled = false; UpdateButtonState(buttonMyMic, MediaStreamState.Idle, "🎙️"); }
                //if (buttonTheirMic != null) { buttonTheirMic.Enabled = false; UpdateButtonState(buttonTheirMic, MediaStreamState.Idle, "👂"); }
                //if (buttonMyCam != null) { buttonMyCam.Enabled = false; UpdateButtonState(buttonMyCam, MediaStreamState.Idle, "📷"); }
                //if (buttonTheirCam != null) { buttonTheirCam.Enabled = false; UpdateButtonState(buttonTheirCam, MediaStreamState.Idle, "👁️"); }
                //if (buttonMutePlayback != null) buttonMutePlayback.Enabled = false;
                Console.WriteLine($"[{DateTime.Now}] 🔒 Audio/Video buttons disabled");
            });
        }

        private void CleanupAudioVideoUI()
        {
            DisableAudioVideoButtons();
            toolTip?.Dispose();
        }
    }
}