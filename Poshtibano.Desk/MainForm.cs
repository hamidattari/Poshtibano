using NetworkMonitor;
using Poshtibano.Common;
using Poshtibano.Desk.Controls;
using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Services;
using Poshtibano.Desk.Services.Connection;
using Poshtibano.Desk.Shared;
using Poshtibano.Desk.Shared.Settings;
using Poshtibano.Desk.Shared.Tools;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Windows.Forms.Timer;

namespace Poshtibano.Desk
{
    public partial class MainForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private const uint WM_DROPFILES = 0x0233;
        private const uint MSGFLT_ALLOW = 1;


        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        // For drag & drop files in "Run as admin" mode
        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hwnd, bool fAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);

        [DllImport("user32.dll")]
        public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, ref CHANGEFILTERSTRUCT pChangeFilterStruct);

        [StructLayout(LayoutKind.Sequential)]
        public struct CHANGEFILTERSTRUCT
        {
            public uint cbSize;
            public uint ExtStatus;
        }

        // Core session coordinator
        private SessionCoordinator _session;
        private bool _isConnecting = false;
        private bool _isDisconnecting = false;
        private bool _inInvitation;
        private ConnectionStatus _lastState;

        private Timer _processTimer = null;
        private bool _inStart;
        private Guid _inviterId;

        // Session settings
        private ClientRole _currentRole = ClientRole.Controller;
        private string _signalingUrl = "";

        private string _activeTransferId = null;
        private bool _fileTrnsferInProgress;

        // ✅ NEW:  Handshake state
        private bool _passwordVerified = false;
        private bool _accessGranted = false;
        private string _caller = "کاربر";
        private string _callerSessionId;

        // App Session ID
        public static Guid ApplicationGuid { get; private set; }
        //public static SettingsManager SettingsManager { get; private set; }
        private RecentConnectionsManager _recentConnectionsManager;

        private NetworkMonitorService _networkMonitor;

        public MainForm()
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            ApplyModernStyles();

            // Set App Guid as SessionID
            ApplicationGuid = Guid.NewGuid();// SettingsManager.Settings.ApplicationGuid.Value;
            //ApplicationGuid = SettingsManager.Settings.ApplicationGuid.Value;

            _currentRole = SettingsManager.Instance.ClientRole;
            _currentRole = ClientRole.Controller;
            _signalingUrl = SettingsManager.Instance.HubAddress;

            guidFormattedTextBoxIdLocal.Text = ApplicationGuid.GuidToFormattedText();
            guidFormattedTextBoxIdLocal.CopyOnly = true;    

            _callerSessionId = guidFormattedTextBoxIdLocal.Text;

            _recentConnectionsManager = new RecentConnectionsManager();
            _recentConnectionsManager.OnConnectionsChanged += LoadRecentConnections;
            LoadRecentConnections();

            InitializeAndSendMonitorsToCoordinator();
            UpdateConnectionStatusUI(ConnectionStatus.Disconnected);

            MakeLabelClickable(labelWebsite, "https://poshtibano.website");
            MakeLabelClickable(labelInsta, "https://www.instagram.com/poshtibano.website");
            MakeLabelClickable(labelEmail, "mailto:poshtibano.website@gmail.com");

            buttonDisplayName.Text = SettingsManager.Instance.DisplayName;
            
            SettingsManager.Instance.SetDisplayName(buttonDisplayName.Text);

            _networkMonitor = new NetworkMonitorService();
            _networkMonitor.OnNetworkUsageUpdated += (sender, usage) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        labelSpeedInfo.Text = usage.ToString();
                    }));
                }
                else
                {
                    labelSpeedInfo.Text = usage.ToString();
                }
            };
            _networkMonitor.StartMonitoring(_currentRole, 1000);

            checkBoxSaceIncomingSession.Checked = SettingsManager.Instance.RecordIncomingSessions;
            checkBoxSaceOutcomingSession.Checked = SettingsManager.Instance.RecordOutcomingSessions;

            InitializeAllowDragAndDropInRunAsAdmin();

            //InitializeMouseOverWindowAnnouncment();
            //SynchronizationContext.SetSynchronizationContext(new SynchronizationContext() { });

            GotFocus += MainForm_GotFocus;
            LostFocus += MainForm_LostFocus;
        }

        private void MainForm_LostFocus(object? sender, EventArgs e)
        {
            if (_session != null && _session.IsConnected)
                _session.SetKeyboardSuppression(true);
        }

        private void MainForm_GotFocus(object? sender, EventArgs e)
        {
            //if (_session != null && _session.IsConnected)
            //    _session.SetKeyboardSuppression(false);
        }

        private void InitializeMouseOverWindowAnnouncment()
        {
            var processNameManager = new ProcessNameManager();
            var mouseTimer = new System.Windows.Forms.Timer();
            mouseTimer.Tick += (sender, e) =>
            {
                var mousePosition = Cursor.Position;
                //var process = $"{processNameManager.GetProcessNameByForegroundWindow()}" +
                var process = $"{processNameManager.GetProcessNameByMousePosition(mousePosition)}" +
                $", {processNameManager.GetChildWindowFromPoint(mousePosition)}" +
                $", {processNameManager.GetHitNameByMousePosition(mousePosition)}";
                labelProcess.Text = process;
            };
            mouseTimer.Start();
        }

        private void Session_OnMouseOnProcess(string process)
        {
            if (InvokeRequired) BeginInvoke(() => Session_OnMouseOnProcess(process));
            else labelProcess.Text = process;
        }

        private async void Session_OnMouseDeniedOnProcess(string process)
        {
            await _session.SendMouseActionOnPorcessDenined(SettingsManager.Instance.DisplayName, process);
        }

        private void Session_OnRemoteMouseActionDenied(string name, string process)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => Session_OnRemoteMouseActionDenied(name, process));
            }
            else
            {
                labelProcess.Text = $"شما دسترسی به {process}  ندارید";

                if (_processTimer == null)
                {
                    _processTimer = new System.Windows.Forms.Timer();
                    _processTimer.Interval = 5000;
                    _processTimer.Tick += (sender, e) =>
                    {
                        if (InvokeRequired) BeginInvoke(() => labelProcess.Text = $"...");
                        else labelProcess.Text = $"...";
                    };
                    _processTimer.Start();
                }
            }
        }

        private void InitializeAllowDragAndDropInRunAsAdmin()
        {
            DragAcceptFiles(this.Handle, true);

            CHANGEFILTERSTRUCT cfs = new CHANGEFILTERSTRUCT();
            cfs.cbSize = (uint)Marshal.SizeOf(typeof(CHANGEFILTERSTRUCT));

            ChangeWindowMessageFilterEx(this.Handle, WM_DROPFILES, MSGFLT_ALLOW, ref cfs);
            ChangeWindowMessageFilterEx(this.Handle, 0x0049, MSGFLT_ALLOW, ref cfs); // WM_COPYGLOBALDATA
            ChangeWindowMessageFilterEx(this.Handle, 0x004A, MSGFLT_ALLOW, ref cfs); // WM_COPYDATA
        }

        protected override async void OnShown(EventArgs e)
        {
            await Start();

            base.OnShown(e);
        }

        protected async Task Start()
        {
            if (_inStart) return;
            _inStart = true;

            if (_currentRole == ClientRole.Agent)
            {
                buttonInvite.Visible = false;
                panelRecentConnections.Visible = true;
                await ConnectAsync(guidFormattedTextBoxIdLocal.Text);
            }
            else
            {
                buttonInvite.Visible = false;
                panelRecentConnections.Visible = true;
                await ConnectAsController(guidFormattedTextBoxIdLocal.Text);
            }
            _inStart = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now}] 🚪 Form closing...");

            try
            {
                if (_session != null)
                {
                    Console.WriteLine($"[{DateTime.Now}] 📎 Detaching session events before close");
                    DetachSessionEvents();
                }

                if (_currentRole == ClientRole.Agent)
                {
                    // Agent: Full shutdown on close
                    FullShutdownAsync().Wait(2000);
                }
                else
                {
                    // Controller: Normal disconnect
                    DisconnectAsync().Wait(2000);
                }

                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error during form close: {ex.Message}");
            }

            base.OnFormClosing(e);
        }

        private async void buttonChange_Click(object sender, EventArgs e)
        {
            var message = _currentRole == ClientRole.Agent ? "پشتیبان" : "کاربر";
            var result = MessageBox.Show($" به نقش {message} تغییر کاربری انجام بشود", "تغییر نوع اتصال؟", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (_currentRole == ClientRole.Agent)
                {
                    await FullShutdownAsync();
                    SettingsManager.Instance.SetClientRole(ClientRole.Controller);
                    _currentRole = ClientRole.Controller;
                    guidFormattedTextBoxIdRemote.Enabled = true;
                }
                else
                {
                    SettingsManager.Instance.SetClientRole(ClientRole.Agent);
                    _currentRole = ClientRole.Agent;
                }

                if (_recentConnectionsManager != null) _recentConnectionsManager.OnConnectionsChanged -= LoadRecentConnections;
                _recentConnectionsManager = new RecentConnectionsManager();
                _recentConnectionsManager.OnConnectionsChanged += LoadRecentConnections;
                LoadRecentConnections();

                UpdateConnectionStatusUI(ConnectionStatus.Disconnected);

                Start();
            }
        }

        private async void buttonInvite_Click(object sender, EventArgs e)
        {
            var dialog = new InvitationForm();
            bool allow = true;
            Invoke(() =>
            {
                TopMost = true;
                Activate();
                TopMost = false;
                dialog.BringToFront();
                if (dialog.ShowDialog() == DialogResult.Cancel) allow = false;
            });

            if (!allow) return;

            var sessionId = dialog.InviteeId;
            var inviteeId = dialog.InviteeId;
            var myId = ApplicationGuid.GuidToFormattedText();

            _currentRole = ClientRole.Agent;
            await _session.SendInvitationToRemote(sessionId, inviteeId, myId, SettingsManager.Instance.DisplayName);
            //await Start();
        }

        private async void Session_OnInvitationRequest(string inviteeId, string inviterId, string inviterName)
        {
            BeginInvoke(async () =>
            {
                var form = new InvitationRequestForm(inviterName);
                bool isAllowed = false;
                TopMost = true;
                Activate();
                TopMost = false;
                form.ShowInTaskbar = true;
                form.ShowIcon = true;
                form.BringToFront();
                var result = form.ShowDialog(this);
                isAllowed = form.AllowAccess;

                await _session.SendInvitationResponse(inviterId, inviteeId, inviterId, SettingsManager.Instance.DisplayName, isAllowed);

                if (!isAllowed)
                {
                    guidFormattedTextBoxIdRemote.CopyOnly = false;
                    guidFormattedTextBoxIdRemote.Text = "0-000-000-000";

                    await Start();
                    return;
                }

                _currentRole = ClientRole.Controller;
                await ConnectAsController(inviterId);

                _inInvitation = true;
                var number = long.Parse(inviterId.Replace(GuidExtention.GlobalSpacer.ToString(), ""));
                var id = GuidExtention.ConvertNumberToGuid(number);
                _inviterId = id;
            });
        }

        private async void Session_OnInvitationResponse(string inviteeId, string inviterId, string inviteeName, bool accepted)
        {
            SafeInvoke(() =>
            {
                var acceptedMessage = accepted ? "has been accepted" : "has not been accepted";
                Console.WriteLine($"[{DateTime.Now}] ⚡ My invitation {acceptedMessage}");

                _inInvitation = accepted;

                if (!accepted)
                {
                    var form = new MessageBoxForm($"درخواست دعوت شما توسط {inviteeId} شما رد شد");
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

            if (accepted) UpdateConnectionStatusUI(ConnectionStatus.Connecting);
            else UpdateConnectionStatusUI(ConnectionStatus.Failed);
        }

        private void buttonMyDeviceId_Click(object sender, EventArgs e)
        {
            guidFormattedTextBoxIdLocal.Text = ApplicationGuid.GuidToFormattedText();
            Clipboard.SetText(guidFormattedTextBoxIdLocal.Text);
        }

        private void buttonOpenFolderIncoming_Click(object sender, EventArgs e)
        {
            var incomingRecordingDirectory = Path.Combine(SettingsManager.Instance.RecordOutcomingFolder);

            var filePrefix = $"{DateTime.Now:yyyy-MM-dd HH-mm-ss}";
            Console.SetOut(new FileLogger($"log\\{filePrefix}_log.txt"));

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = incomingRecordingDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void buttonOpenFolderOutcoming_Click(object sender, EventArgs e)
        {
            var outcomingRecordingDirectory = Path.Combine(SettingsManager.Instance.RecordIncomingFolder);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = outcomingRecordingDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void checkBoxSaceIncomingSession_CheckedChanged(object sender, EventArgs e)
        {
            SettingsManager.Instance.SetRecordIncomingSessions(checkBoxSaceIncomingSession.Checked);
        }

        private void checkBoxSaceOutcomingSession_CheckedChanged(object sender, EventArgs e)
        {
            SettingsManager.Instance.SetRecordOutcomingSessions(checkBoxSaceOutcomingSession.Checked);
        }
    }
}