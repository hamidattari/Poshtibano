using Poshtibano.Desk.Shared.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Poshtibano.Desk
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Button buttonMinimize;
        private System.Windows.Forms.Button buttonMaximize;
        private System.Windows.Forms.Button buttonClose;

        private System.Windows.Forms.Button buttonChat;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.Button buttonToggleBar;

        private System.Windows.Forms.Panel panelConnectionBar;
        private System.Windows.Forms.Button buttonDisconnect;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Panel panelIdInput;
        private System.Windows.Forms.Label labelCurrentIdConst;
        private System.Windows.Forms.Panel panelDashboard;
        private System.Windows.Forms.Panel panelAgentCard;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.Label labelState;
        private System.Windows.Forms.Label labelStateIndicator;
        private System.Windows.Forms.Label labelAgentMonitorConst;
        private System.Windows.Forms.Panel panelControllerSettingsCard;
        private System.Windows.Forms.Label labelControllerSettingsConst;
        private System.Windows.Forms.Label labelFpsConst;
        private System.Windows.Forms.Label labelQualityConst;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.PictureBox pictureBox;

        private System.Windows.Forms.CheckBox checkBoxClipboardSharing;

        // Chat Components redesigned
        private System.Windows.Forms.Panel panelChat;
        private System.Windows.Forms.FlowLayoutPanel flowlayoutpanelChatHistory;
        private System.Windows.Forms.Panel panelChatInput;
        private System.Windows.Forms.RichTextBox textBoxChatInput;
        private System.Windows.Forms.Button buttonChatSend;
        private System.Windows.Forms.ProgressBar progressBarTransfer;
        private System.Windows.Forms.Label labelTransferStatus;
        private System.Windows.Forms.Panel panelTransfer;
        private System.Windows.Forms.Panel panelRecentConnections;
        private System.Windows.Forms.Label labelRecentConnectionsTitle;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelRecentConnections;
        private System.Windows.Forms.Button buttonClearRecentConnections;

        private System.Windows.Forms.Button buttonCancelTransfer;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            buttonCancelTransfer = new Button();
            labelTransferStatus = new Label();
            panelHeader = new Panel();
            buttonClearLog = new Button();
            buttonChange = new Button();
            pictureBoxLogo = new PictureBox();
            labelTitle = new Label();
            buttonToggleBar = new Button();
            buttonSettings = new Button();
            buttonChat = new Button();
            buttonMinimize = new Button();
            buttonMaximize = new Button();
            buttonClose = new Button();
            buttonAudioVideoSettings = new Button();
            panelConnectionBar = new Panel();
            buttonDisconnect = new Button();
            buttonConnect = new Button();
            panelIdInput = new Panel();
            labelRemoteIdConst = new Label();
            buttonMyDeviceId = new Button();
            buttonInvite = new Button();
            guidFormattedTextBoxIdLocal = new Poshtibano.Desk.Controls.GuidFormattedTextBox();
            guidFormattedTextBoxIdRemote = new Poshtibano.Desk.Controls.GuidFormattedTextBox();
            labelCurrentIdConst = new Label();
            panelDashboard = new Panel();
            panelRecentConnections = new Panel();
            buttonClearRecentConnections = new Button();
            flowLayoutPanelRecentConnections = new FlowLayoutPanel();
            labelRecentConnectionsTitle = new Label();
            panelAgentCard = new Panel();
            richTextBoxNews = new RichTextBox();
            labelWebsite = new Label();
            labelInsta = new Label();
            labelReportConst = new Label();
            labelEmail = new Label();
            labelInfo = new Label();
            labelState = new Label();
            pictureBoxInstaLogo = new PictureBox();
            labelStateIndicator = new Label();
            labelAgentMonitorConst = new Label();
            panelControllerSettingsCard = new Panel();
            buttonCloseControllerSettings = new Button();
            checkBoxFullFrame = new CheckBox();
            numericUpDownQuality = new NumericUpDown();
            numericUpDownFps = new NumericUpDown();
            labelControllerSettingsConst = new Label();
            labelFpsConst = new Label();
            labelQualityConst = new Label();
            buttonApply = new Button();
            panelAgentSettingsCard = new Panel();
            buttonOpenFolderOutcoming = new Button();
            buttonOpenFolderIncoming = new Button();
            checkBoxSaceOutcomingSession = new CheckBox();
            checkBoxSaceIncomingSession = new CheckBox();
            buttonCloseAgentSettings = new Button();
            labelAgentSettingsConst = new Label();
            buttonAgentPasswordChange = new Button();
            panelAudioVideoSettings = new Panel();
            flowLayoutPanelMicAndWebcam = new FlowLayoutPanel();
            flowLayoutPanelAudioVideoSettings = new FlowLayoutPanel();
            buttonCloseAudioVideoSettings = new Button();
            labelAudioVideoSettingsConst = new Label();
            checkBoxClipboardSharing = new CheckBox();
            pictureBox = new PictureBox();
            panelChat = new Panel();
            flowlayoutpanelChatHistory = new FlowLayoutPanel();
            buttonUploadFile = new Button();
            panelTransfer = new Panel();
            progressBarTransfer = new ProgressBar();
            panelChatInput = new Panel();
            buttonSendChat = new Button();
            textBoxChatInput = new RichTextBox();
            buttonChatSend = new Button();
            panelInfo = new Panel();
            labelProcess = new Label();
            buttonDisplayName = new Button();
            labelSpeedInfo = new Label();
            toolTipCopyMyComputerId = new ToolTip(components);
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            panelConnectionBar.SuspendLayout();
            panelIdInput.SuspendLayout();
            panelDashboard.SuspendLayout();
            panelRecentConnections.SuspendLayout();
            panelAgentCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxInstaLogo).BeginInit();
            panelControllerSettingsCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownQuality).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownFps).BeginInit();
            panelAgentSettingsCard.SuspendLayout();
            panelAudioVideoSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            panelChat.SuspendLayout();
            flowlayoutpanelChatHistory.SuspendLayout();
            panelTransfer.SuspendLayout();
            panelChatInput.SuspendLayout();
            panelInfo.SuspendLayout();
            SuspendLayout();
            // 
            // buttonCancelTransfer
            // 
            buttonCancelTransfer.Location = new Point(227, 34);
            buttonCancelTransfer.Name = "buttonCancelTransfer";
            buttonCancelTransfer.Size = new Size(63, 30);
            buttonCancelTransfer.TabIndex = 0;
            buttonCancelTransfer.Text = "❌ لغو انتقال";
            buttonCancelTransfer.Visible = false;
            buttonCancelTransfer.Click += ButtonCancelTransfer_Click;
            // 
            // labelTransferStatus
            // 
            labelTransferStatus.Font = new Font("Segoe UI", 8F);
            labelTransferStatus.ForeColor = Color.Gray;
            labelTransferStatus.Location = new Point(8, 29);
            labelTransferStatus.Name = "labelTransferStatus";
            labelTransferStatus.Size = new Size(211, 36);
            labelTransferStatus.TabIndex = 3;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(255, 128, 0);
            panelHeader.Controls.Add(buttonClearLog);
            panelHeader.Controls.Add(buttonChange);
            panelHeader.Controls.Add(pictureBoxLogo);
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(buttonToggleBar);
            panelHeader.Controls.Add(buttonSettings);
            panelHeader.Controls.Add(buttonChat);
            panelHeader.Controls.Add(buttonMinimize);
            panelHeader.Controls.Add(buttonMaximize);
            panelHeader.Controls.Add(buttonClose);
            panelHeader.Controls.Add(buttonAudioVideoSettings);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Margin = new Padding(3, 4, 3, 4);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1025, 60);
            panelHeader.TabIndex = 0;
            panelHeader.DoubleClick += PanelHeader_DoubleClick;
            panelHeader.MouseDown += Header_MouseDown;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClearLog.FlatAppearance.BorderSize = 0;
            buttonClearLog.FlatStyle = FlatStyle.Flat;
            buttonClearLog.Font = new Font("Segoe UI Emoji", 12F);
            buttonClearLog.ForeColor = Color.White;
            buttonClearLog.Location = new Point(619, -1);
            buttonClearLog.Margin = new Padding(3, 4, 3, 4);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(50, 60);
            buttonClearLog.TabIndex = 8;
            buttonClearLog.Text = "\U0001f9f9";
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Visible = false;
            buttonClearLog.Click += buttonClearLog_Click;
            // 
            // buttonChange
            // 
            buttonChange.FlatAppearance.BorderSize = 0;
            buttonChange.FlatStyle = FlatStyle.Flat;
            buttonChange.Font = new Font("Segoe UI Emoji", 12F);
            buttonChange.ForeColor = Color.White;
            buttonChange.Location = new Point(317, -29);
            buttonChange.Margin = new Padding(3, 4, 3, 4);
            buttonChange.Name = "buttonChange";
            buttonChange.Size = new Size(304, 51);
            buttonChange.TabIndex = 7;
            buttonChange.Text = "🔄 Change Controller Type";
            buttonChange.UseVisualStyleBackColor = true;
            buttonChange.Visible = false;
            buttonChange.Click += buttonChange_Click;
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.BackColor = Color.FromArgb(255, 128, 0);
            pictureBoxLogo.Image = (Image)resources.GetObject("pictureBoxLogo.Image");
            pictureBoxLogo.Location = new Point(-4, 0);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Size = new Size(55, 55);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.TabIndex = 2;
            pictureBoxLogo.TabStop = false;
            pictureBoxLogo.MouseDown += Header_MouseDown;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(55, 13);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(256, 28);
            labelTitle.TabIndex = 2;
            labelTitle.Text = "Poshtibanodesk Beta 0.45";
            labelTitle.MouseDown += Header_MouseDown;
            // 
            // buttonToggleBar
            // 
            buttonToggleBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonToggleBar.FlatAppearance.BorderSize = 0;
            buttonToggleBar.FlatStyle = FlatStyle.Flat;
            buttonToggleBar.Font = new Font("Segoe UI Emoji", 12F);
            buttonToggleBar.ForeColor = Color.White;
            buttonToggleBar.Location = new Point(724, 0);
            buttonToggleBar.Margin = new Padding(3, 4, 3, 4);
            buttonToggleBar.Name = "buttonToggleBar";
            buttonToggleBar.Size = new Size(50, 60);
            buttonToggleBar.TabIndex = 6;
            buttonToggleBar.Text = "🔼";
            buttonToggleBar.UseVisualStyleBackColor = true;
            buttonToggleBar.Visible = false;
            buttonToggleBar.Click += buttonToggleBar_Click;
            // 
            // buttonSettings
            // 
            buttonSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSettings.FlatAppearance.BorderSize = 0;
            buttonSettings.FlatStyle = FlatStyle.Flat;
            buttonSettings.Font = new Font("Segoe UI Emoji", 14F);
            buttonSettings.ForeColor = Color.White;
            buttonSettings.Location = new Point(774, 0);
            buttonSettings.Margin = new Padding(3, 4, 3, 4);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(50, 60);
            buttonSettings.TabIndex = 5;
            buttonSettings.Text = "🛠";
            buttonSettings.UseVisualStyleBackColor = true;
            buttonSettings.Click += buttonSettings_Click;
            // 
            // buttonChat
            // 
            buttonChat.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonChat.FlatAppearance.BorderSize = 0;
            buttonChat.FlatStyle = FlatStyle.Flat;
            buttonChat.Font = new Font("Segoe UI Emoji", 12F);
            buttonChat.ForeColor = Color.White;
            buttonChat.Location = new Point(824, 0);
            buttonChat.Margin = new Padding(3, 4, 3, 4);
            buttonChat.Name = "buttonChat";
            buttonChat.Size = new Size(50, 60);
            buttonChat.TabIndex = 4;
            buttonChat.Text = "🗪";
            buttonChat.UseVisualStyleBackColor = true;
            buttonChat.Visible = false;
            buttonChat.Click += buttonChat_Click;
            // 
            // buttonMinimize
            // 
            buttonMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonMinimize.FlatAppearance.BorderSize = 0;
            buttonMinimize.FlatStyle = FlatStyle.Flat;
            buttonMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonMinimize.ForeColor = Color.White;
            buttonMinimize.Location = new Point(874, 0);
            buttonMinimize.Margin = new Padding(3, 4, 3, 4);
            buttonMinimize.Name = "buttonMinimize";
            buttonMinimize.Size = new Size(50, 60);
            buttonMinimize.TabIndex = 1;
            buttonMinimize.Text = "—";
            buttonMinimize.UseVisualStyleBackColor = true;
            buttonMinimize.Click += buttonMinimize_Click;
            // 
            // buttonMaximize
            // 
            buttonMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonMaximize.FlatAppearance.BorderSize = 0;
            buttonMaximize.FlatStyle = FlatStyle.Flat;
            buttonMaximize.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonMaximize.ForeColor = Color.White;
            buttonMaximize.Location = new Point(924, 0);
            buttonMaximize.Margin = new Padding(3, 4, 3, 4);
            buttonMaximize.Name = "buttonMaximize";
            buttonMaximize.Size = new Size(50, 60);
            buttonMaximize.TabIndex = 3;
            buttonMaximize.Text = "⬜";
            buttonMaximize.UseVisualStyleBackColor = true;
            buttonMaximize.Click += buttonMaximize_Click;
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClose.BackColor = Color.FromArgb(255, 128, 0);
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.FlatStyle = FlatStyle.Flat;
            buttonClose.Font = new Font("Segoe UI", 9F);
            buttonClose.ForeColor = Color.White;
            buttonClose.Location = new Point(974, 0);
            buttonClose.Margin = new Padding(3, 4, 3, 4);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(50, 60);
            buttonClose.TabIndex = 0;
            buttonClose.Text = "✕";
            buttonClose.UseVisualStyleBackColor = true;
            buttonClose.Click += buttonClose_Click;
            // 
            // buttonAudioVideoSettings
            // 
            buttonAudioVideoSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonAudioVideoSettings.FlatAppearance.BorderSize = 0;
            buttonAudioVideoSettings.FlatStyle = FlatStyle.Flat;
            buttonAudioVideoSettings.Font = new Font("Segoe UI Emoji", 12F);
            buttonAudioVideoSettings.ForeColor = Color.White;
            buttonAudioVideoSettings.Location = new Point(675, 0);
            buttonAudioVideoSettings.Margin = new Padding(3, 4, 3, 4);
            buttonAudioVideoSettings.Name = "buttonAudioVideoSettings";
            buttonAudioVideoSettings.Size = new Size(50, 60);
            buttonAudioVideoSettings.TabIndex = 8;
            buttonAudioVideoSettings.Text = "🎦";
            buttonAudioVideoSettings.UseVisualStyleBackColor = true;
            buttonAudioVideoSettings.Visible = false;
            buttonAudioVideoSettings.Click += buttonAudioVideoSettings_Click;
            // 
            // panelConnectionBar
            // 
            panelConnectionBar.BackColor = Color.Gainsboro;
            panelConnectionBar.Controls.Add(buttonDisconnect);
            panelConnectionBar.Controls.Add(buttonConnect);
            panelConnectionBar.Controls.Add(panelIdInput);
            panelConnectionBar.Dock = DockStyle.Top;
            panelConnectionBar.Location = new Point(0, 60);
            panelConnectionBar.Margin = new Padding(3, 4, 3, 4);
            panelConnectionBar.Name = "panelConnectionBar";
            panelConnectionBar.Size = new Size(1025, 108);
            panelConnectionBar.TabIndex = 1;
            // 
            // buttonDisconnect
            // 
            buttonDisconnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonDisconnect.BackColor = Color.White;
            buttonDisconnect.FlatAppearance.BorderColor = Color.FromArgb(255, 128, 0);
            buttonDisconnect.FlatStyle = FlatStyle.Flat;
            buttonDisconnect.Font = new Font("Segoe UI", 11F);
            buttonDisconnect.ForeColor = Color.FromArgb(255, 128, 0);
            buttonDisconnect.Location = new Point(580, 21);
            buttonDisconnect.Margin = new Padding(3, 4, 3, 4);
            buttonDisconnect.Name = "buttonDisconnect";
            buttonDisconnect.Size = new Size(145, 60);
            buttonDisconnect.TabIndex = 4;
            buttonDisconnect.Text = "قطع اتصال";
            buttonDisconnect.UseVisualStyleBackColor = false;
            buttonDisconnect.Click += buttonDisconnect_Click;
            // 
            // buttonConnect
            // 
            buttonConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonConnect.BackColor = Color.FromArgb(255, 128, 0);
            buttonConnect.FlatAppearance.BorderSize = 0;
            buttonConnect.FlatStyle = FlatStyle.Flat;
            buttonConnect.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonConnect.ForeColor = Color.White;
            buttonConnect.Location = new Point(730, 21);
            buttonConnect.Margin = new Padding(3, 4, 3, 4);
            buttonConnect.Name = "buttonConnect";
            buttonConnect.Size = new Size(139, 60);
            buttonConnect.TabIndex = 3;
            buttonConnect.Text = "اتصال";
            buttonConnect.UseVisualStyleBackColor = false;
            buttonConnect.Click += buttonConnect_Click;
            // 
            // panelIdInput
            // 
            panelIdInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelIdInput.BackColor = Color.FromArgb(245, 246, 250);
            panelIdInput.Controls.Add(labelRemoteIdConst);
            panelIdInput.Controls.Add(buttonMyDeviceId);
            panelIdInput.Controls.Add(buttonInvite);
            panelIdInput.Controls.Add(guidFormattedTextBoxIdLocal);
            panelIdInput.Controls.Add(guidFormattedTextBoxIdRemote);
            panelIdInput.Controls.Add(labelCurrentIdConst);
            panelIdInput.Location = new Point(17, 13);
            panelIdInput.Margin = new Padding(3, 4, 3, 4);
            panelIdInput.Name = "panelIdInput";
            panelIdInput.Size = new Size(557, 79);
            panelIdInput.TabIndex = 2;
            // 
            // labelRemoteIdConst
            // 
            labelRemoteIdConst.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelRemoteIdConst.AutoSize = true;
            labelRemoteIdConst.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelRemoteIdConst.ForeColor = Color.Gray;
            labelRemoteIdConst.Location = new Point(314, 4);
            labelRemoteIdConst.Name = "labelRemoteIdConst";
            labelRemoteIdConst.Size = new Size(163, 23);
            labelRemoteIdConst.TabIndex = 21;
            labelRemoteIdConst.Text = "شماره دستگاه ریموت";
            // 
            // buttonMyDeviceId
            // 
            buttonMyDeviceId.BackColor = Color.FromArgb(255, 128, 0);
            buttonMyDeviceId.FlatAppearance.BorderSize = 0;
            buttonMyDeviceId.FlatStyle = FlatStyle.Flat;
            buttonMyDeviceId.Font = new Font("Segoe UI", 9F);
            buttonMyDeviceId.ForeColor = Color.White;
            buttonMyDeviceId.Location = new Point(17, 31);
            buttonMyDeviceId.Margin = new Padding(3, 4, 3, 4);
            buttonMyDeviceId.Name = "buttonMyDeviceId";
            buttonMyDeviceId.Size = new Size(28, 28);
            buttonMyDeviceId.TabIndex = 20;
            buttonMyDeviceId.Text = "🖥️";
            toolTipCopyMyComputerId.SetToolTip(buttonMyDeviceId, "ریست و کپی کردن شماره این دستگاه");
            buttonMyDeviceId.UseVisualStyleBackColor = true;
            buttonMyDeviceId.Click += buttonMyDeviceId_Click;
            // 
            // buttonInvite
            // 
            buttonInvite.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonInvite.BackColor = Color.FromArgb(255, 128, 0);
            buttonInvite.FlatAppearance.BorderSize = 0;
            buttonInvite.FlatStyle = FlatStyle.Flat;
            buttonInvite.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonInvite.ForeColor = Color.White;
            buttonInvite.Location = new Point(501, 19);
            buttonInvite.Margin = new Padding(3, 4, 3, 4);
            buttonInvite.Name = "buttonInvite";
            buttonInvite.Size = new Size(40, 40);
            buttonInvite.TabIndex = 5;
            buttonInvite.Text = "⚡";
            buttonInvite.UseVisualStyleBackColor = false;
            buttonInvite.Click += buttonInvite_Click;
            // 
            // guidFormattedTextBoxIdLocal
            // 
            guidFormattedTextBoxIdLocal.BackColor = Color.White;
            guidFormattedTextBoxIdLocal.BorderStyle = BorderStyle.None;
            guidFormattedTextBoxIdLocal.CopyOnly = false;
            guidFormattedTextBoxIdLocal.Font = new Font("Segoe UI", 14F);
            guidFormattedTextBoxIdLocal.ForeColor = Color.Black;
            guidFormattedTextBoxIdLocal.GuidValue = new Guid("0dfcea7e-0000-0000-8899-aabbccddeeff");
            guidFormattedTextBoxIdLocal.Location = new Point(61, 30);
            guidFormattedTextBoxIdLocal.Margin = new Padding(3, 4, 3, 4);
            guidFormattedTextBoxIdLocal.MaxLength = 15;
            guidFormattedTextBoxIdLocal.Name = "guidFormattedTextBoxIdLocal";
            guidFormattedTextBoxIdLocal.ReadOnly = true;
            guidFormattedTextBoxIdLocal.Size = new Size(210, 32);
            guidFormattedTextBoxIdLocal.Spacer = '-';
            guidFormattedTextBoxIdLocal.TabIndex = 2;
            guidFormattedTextBoxIdLocal.Text = "1-234-678-910";
            guidFormattedTextBoxIdLocal.TextAlign = HorizontalAlignment.Center;
            // 
            // guidFormattedTextBoxIdRemote
            // 
            guidFormattedTextBoxIdRemote.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            guidFormattedTextBoxIdRemote.BackColor = Color.White;
            guidFormattedTextBoxIdRemote.BorderStyle = BorderStyle.None;
            guidFormattedTextBoxIdRemote.CopyOnly = false;
            guidFormattedTextBoxIdRemote.Font = new Font("Segoe UI", 14F);
            guidFormattedTextBoxIdRemote.ForeColor = Color.Black;
            guidFormattedTextBoxIdRemote.GuidValue = new Guid("c4653600-ffff-ffff-8899-aabbccddeeff");
            guidFormattedTextBoxIdRemote.Location = new Point(285, 31);
            guidFormattedTextBoxIdRemote.Margin = new Padding(3, 4, 3, 4);
            guidFormattedTextBoxIdRemote.MaxLength = 15;
            guidFormattedTextBoxIdRemote.Name = "guidFormattedTextBoxIdRemote";
            guidFormattedTextBoxIdRemote.Size = new Size(210, 32);
            guidFormattedTextBoxIdRemote.Spacer = '-';
            guidFormattedTextBoxIdRemote.TabIndex = 2;
            guidFormattedTextBoxIdRemote.Text = "0-000-000-000";
            guidFormattedTextBoxIdRemote.TextAlign = HorizontalAlignment.Center;
            // 
            // labelCurrentIdConst
            // 
            labelCurrentIdConst.AutoSize = true;
            labelCurrentIdConst.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelCurrentIdConst.ForeColor = Color.Gray;
            labelCurrentIdConst.Location = new Point(104, 4);
            labelCurrentIdConst.Name = "labelCurrentIdConst";
            labelCurrentIdConst.Size = new Size(135, 23);
            labelCurrentIdConst.TabIndex = 0;
            labelCurrentIdConst.Text = "شماره این دستگاه";
            // 
            // panelDashboard
            // 
            panelDashboard.BackColor = Color.FromArgb(245, 246, 250);
            panelDashboard.Controls.Add(panelRecentConnections);
            panelDashboard.Controls.Add(panelAgentCard);
            panelDashboard.Dock = DockStyle.Fill;
            panelDashboard.Location = new Point(0, 168);
            panelDashboard.Margin = new Padding(3, 4, 3, 4);
            panelDashboard.Name = "panelDashboard";
            panelDashboard.Size = new Size(725, 399);
            panelDashboard.TabIndex = 2;
            // 
            // panelRecentConnections
            // 
            panelRecentConnections.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panelRecentConnections.BackColor = Color.White;
            panelRecentConnections.Controls.Add(buttonClearRecentConnections);
            panelRecentConnections.Controls.Add(flowLayoutPanelRecentConnections);
            panelRecentConnections.Controls.Add(labelRecentConnectionsTitle);
            panelRecentConnections.Location = new Point(17, 239);
            panelRecentConnections.Name = "panelRecentConnections";
            panelRecentConnections.Size = new Size(705, 136);
            panelRecentConnections.TabIndex = 2;
            // 
            // buttonClearRecentConnections
            // 
            buttonClearRecentConnections.BackColor = Color.FromArgb(231, 76, 60);
            buttonClearRecentConnections.FlatAppearance.BorderSize = 0;
            buttonClearRecentConnections.FlatStyle = FlatStyle.Flat;
            buttonClearRecentConnections.Font = new Font("Segoe UI", 9F);
            buttonClearRecentConnections.ForeColor = Color.White;
            buttonClearRecentConnections.Location = new Point(380, 12);
            buttonClearRecentConnections.Name = "buttonClearRecentConnections";
            buttonClearRecentConnections.Size = new Size(95, 35);
            buttonClearRecentConnections.TabIndex = 1;
            buttonClearRecentConnections.Text = "پاک کردن همه";
            buttonClearRecentConnections.UseVisualStyleBackColor = false;
            buttonClearRecentConnections.Click += buttonClearRecentConnections_Click;
            // 
            // flowLayoutPanelRecentConnections
            // 
            flowLayoutPanelRecentConnections.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            flowLayoutPanelRecentConnections.AutoScroll = true;
            flowLayoutPanelRecentConnections.BackColor = Color.FromArgb(250, 250, 250);
            flowLayoutPanelRecentConnections.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelRecentConnections.Location = new Point(10, 55);
            flowLayoutPanelRecentConnections.Name = "flowLayoutPanelRecentConnections";
            flowLayoutPanelRecentConnections.Padding = new Padding(5);
            flowLayoutPanelRecentConnections.Size = new Size(471, 70);
            flowLayoutPanelRecentConnections.TabIndex = 2;
            flowLayoutPanelRecentConnections.WrapContents = false;
            // 
            // labelRecentConnectionsTitle
            // 
            labelRecentConnectionsTitle.AutoSize = true;
            labelRecentConnectionsTitle.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            labelRecentConnectionsTitle.ForeColor = Color.FromArgb(52, 73, 94);
            labelRecentConnectionsTitle.Location = new Point(17, 15);
            labelRecentConnectionsTitle.Name = "labelRecentConnectionsTitle";
            labelRecentConnectionsTitle.Size = new Size(117, 28);
            labelRecentConnectionsTitle.TabIndex = 0;
            labelRecentConnectionsTitle.Text = "اتصالات اخیر";
            // 
            // panelAgentCard
            // 
            panelAgentCard.BackColor = Color.White;
            panelAgentCard.Controls.Add(richTextBoxNews);
            panelAgentCard.Controls.Add(labelWebsite);
            panelAgentCard.Controls.Add(labelInsta);
            panelAgentCard.Controls.Add(labelReportConst);
            panelAgentCard.Controls.Add(labelEmail);
            panelAgentCard.Controls.Add(labelInfo);
            panelAgentCard.Controls.Add(labelState);
            panelAgentCard.Controls.Add(pictureBoxInstaLogo);
            panelAgentCard.Controls.Add(labelStateIndicator);
            panelAgentCard.Controls.Add(labelAgentMonitorConst);
            panelAgentCard.Location = new Point(17, 20);
            panelAgentCard.Margin = new Padding(3, 4, 3, 4);
            panelAgentCard.Name = "panelAgentCard";
            panelAgentCard.Size = new Size(705, 212);
            panelAgentCard.TabIndex = 1;
            // 
            // richTextBoxNews
            // 
            richTextBoxNews.BackColor = SystemColors.ButtonHighlight;
            richTextBoxNews.BorderStyle = BorderStyle.None;
            richTextBoxNews.Font = new Font("Segoe UI", 9F);
            richTextBoxNews.Location = new Point(315, 1);
            richTextBoxNews.Name = "richTextBoxNews";
            richTextBoxNews.ReadOnly = true;
            richTextBoxNews.RightToLeft = RightToLeft.Yes;
            richTextBoxNews.Size = new Size(254, 153);
            richTextBoxNews.TabIndex = 15;
            richTextBoxNews.Text = "نسخه جدید 0.45 بتا در تاریخ 1404/12/02 منتشر شد\nدر این نسخه قابلیت اشتراک‌گذاری کلیپ‌بورد و امکان ذخیره ویدئویی سشن های ورودی و خروجی را دارد ";
            // 
            // labelWebsite
            // 
            labelWebsite.AutoSize = true;
            labelWebsite.Cursor = Cursors.Hand;
            labelWebsite.Location = new Point(409, 182);
            labelWebsite.Name = "labelWebsite";
            labelWebsite.RightToLeft = RightToLeft.Yes;
            labelWebsite.Size = new Size(293, 20);
            labelWebsite.TabIndex = 4;
            labelWebsite.Text = "توسعه داده شده توسط poshtibano.website";
            // 
            // labelInsta
            // 
            labelInsta.AutoSize = true;
            labelInsta.Location = new Point(446, 153);
            labelInsta.Name = "labelInsta";
            labelInsta.RightToLeft = RightToLeft.Yes;
            labelInsta.Size = new Size(218, 20);
            labelInsta.TabIndex = 4;
            labelInsta.Text = "اینستاگرام poshtibano.website@";
            labelInsta.Visible = false;
            // 
            // labelReportConst
            // 
            labelReportConst.AutoSize = true;
            labelReportConst.Font = new Font("Segoe UI", 8F);
            labelReportConst.Location = new Point(17, 157);
            labelReportConst.Name = "labelReportConst";
            labelReportConst.RightToLeft = RightToLeft.Yes;
            labelReportConst.Size = new Size(387, 19);
            labelReportConst.TabIndex = 14;
            labelReportConst.Text = "لطفا مشکلات نرم افزار را از طریق آدرس ایمیل زیر گزارش فرمایید";
            // 
            // labelEmail
            // 
            labelEmail.AutoSize = true;
            labelEmail.Cursor = Cursors.Hand;
            labelEmail.Location = new Point(75, 182);
            labelEmail.Name = "labelEmail";
            labelEmail.Size = new Size(222, 20);
            labelEmail.TabIndex = 4;
            labelEmail.Text = "poshtibano.website@gmail.com";
            // 
            // labelInfo
            // 
            labelInfo.AutoSize = true;
            labelInfo.Font = new Font("Consolas", 8F);
            labelInfo.ForeColor = Color.Gray;
            labelInfo.Location = new Point(17, 333);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(62, 17);
            labelInfo.TabIndex = 13;
            labelInfo.Text = "بیکار... ";
            // 
            // labelState
            // 
            labelState.AutoSize = true;
            labelState.Font = new Font("Segoe UI", 9F);
            labelState.ForeColor = Color.Gray;
            labelState.Location = new Point(50, 66);
            labelState.Name = "labelState";
            labelState.RightToLeft = RightToLeft.Yes;
            labelState.Size = new Size(42, 20);
            labelState.TabIndex = 12;
            labelState.Text = "آماده";
            // 
            // pictureBoxInstaLogo
            // 
            pictureBoxInstaLogo.Image = (Image)resources.GetObject("pictureBoxInstaLogo.Image");
            pictureBoxInstaLogo.Location = new Point(575, 0);
            pictureBoxInstaLogo.Name = "pictureBoxInstaLogo";
            pictureBoxInstaLogo.Size = new Size(128, 128);
            pictureBoxInstaLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxInstaLogo.TabIndex = 3;
            pictureBoxInstaLogo.TabStop = false;
            // 
            // labelStateIndicator
            // 
            labelStateIndicator.AutoSize = true;
            labelStateIndicator.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            labelStateIndicator.ForeColor = Color.Gray;
            labelStateIndicator.Location = new Point(17, 54);
            labelStateIndicator.Name = "labelStateIndicator";
            labelStateIndicator.Size = new Size(28, 37);
            labelStateIndicator.TabIndex = 11;
            labelStateIndicator.Text = "•";
            // 
            // labelAgentMonitorConst
            // 
            labelAgentMonitorConst.AutoSize = true;
            labelAgentMonitorConst.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelAgentMonitorConst.ForeColor = Color.Gray;
            labelAgentMonitorConst.Location = new Point(17, 20);
            labelAgentMonitorConst.Name = "labelAgentMonitorConst";
            labelAgentMonitorConst.Size = new Size(113, 23);
            labelAgentMonitorConst.TabIndex = 7;
            labelAgentMonitorConst.Text = "وضعیت ارتباط";
            // 
            // panelControllerSettingsCard
            // 
            panelControllerSettingsCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelControllerSettingsCard.BackColor = Color.White;
            panelControllerSettingsCard.BorderStyle = BorderStyle.FixedSingle;
            panelControllerSettingsCard.Controls.Add(buttonCloseControllerSettings);
            panelControllerSettingsCard.Controls.Add(checkBoxFullFrame);
            panelControllerSettingsCard.Controls.Add(numericUpDownQuality);
            panelControllerSettingsCard.Controls.Add(numericUpDownFps);
            panelControllerSettingsCard.Controls.Add(labelControllerSettingsConst);
            panelControllerSettingsCard.Controls.Add(labelFpsConst);
            panelControllerSettingsCard.Controls.Add(labelQualityConst);
            panelControllerSettingsCard.Controls.Add(buttonApply);
            panelControllerSettingsCard.Location = new Point(766, 70);
            panelControllerSettingsCard.Margin = new Padding(3, 4, 3, 4);
            panelControllerSettingsCard.Name = "panelControllerSettingsCard";
            panelControllerSettingsCard.Size = new Size(251, 224);
            panelControllerSettingsCard.TabIndex = 3;
            panelControllerSettingsCard.Visible = false;
            // 
            // buttonCloseControllerSettings
            // 
            buttonCloseControllerSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonCloseControllerSettings.BackColor = Color.FromArgb(255, 128, 0);
            buttonCloseControllerSettings.FlatAppearance.BorderSize = 0;
            buttonCloseControllerSettings.FlatStyle = FlatStyle.Flat;
            buttonCloseControllerSettings.Font = new Font("Segoe UI", 9F);
            buttonCloseControllerSettings.ForeColor = Color.White;
            buttonCloseControllerSettings.Location = new Point(200, 4);
            buttonCloseControllerSettings.Margin = new Padding(3, 4, 3, 4);
            buttonCloseControllerSettings.Name = "buttonCloseControllerSettings";
            buttonCloseControllerSettings.Size = new Size(34, 32);
            buttonCloseControllerSettings.TabIndex = 17;
            buttonCloseControllerSettings.Text = "✕";
            buttonCloseControllerSettings.UseVisualStyleBackColor = true;
            buttonCloseControllerSettings.Click += buttonSettings_Click;
            // 
            // checkBoxFullFrame
            // 
            checkBoxFullFrame.AutoSize = true;
            checkBoxFullFrame.Location = new Point(11, 118);
            checkBoxFullFrame.Name = "checkBoxFullFrame";
            checkBoxFullFrame.RightToLeft = RightToLeft.Yes;
            checkBoxFullFrame.Size = new Size(159, 24);
            checkBoxFullFrame.TabIndex = 23;
            checkBoxFullFrame.Text = "دریافت فریم مستقل";
            checkBoxFullFrame.UseVisualStyleBackColor = true;
            // 
            // numericUpDownQuality
            // 
            numericUpDownQuality.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            numericUpDownQuality.Location = new Point(135, 52);
            numericUpDownQuality.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numericUpDownQuality.Name = "numericUpDownQuality";
            numericUpDownQuality.Size = new Size(84, 27);
            numericUpDownQuality.TabIndex = 21;
            numericUpDownQuality.TextAlign = HorizontalAlignment.Center;
            numericUpDownQuality.Value = new decimal(new int[] { 40, 0, 0, 0 });
            // 
            // numericUpDownFps
            // 
            numericUpDownFps.Location = new Point(135, 81);
            numericUpDownFps.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numericUpDownFps.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownFps.Name = "numericUpDownFps";
            numericUpDownFps.Size = new Size(84, 27);
            numericUpDownFps.TabIndex = 20;
            numericUpDownFps.TextAlign = HorizontalAlignment.Center;
            numericUpDownFps.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // labelControllerSettingsConst
            // 
            labelControllerSettingsConst.AutoSize = true;
            labelControllerSettingsConst.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelControllerSettingsConst.ForeColor = Color.Gray;
            labelControllerSettingsConst.Location = new Point(17, 13);
            labelControllerSettingsConst.Name = "labelControllerSettingsConst";
            labelControllerSettingsConst.Size = new Size(117, 23);
            labelControllerSettingsConst.TabIndex = 8;
            labelControllerSettingsConst.Text = "تنظیمات کنترلر";
            // 
            // labelFpsConst
            // 
            labelFpsConst.AutoSize = true;
            labelFpsConst.ForeColor = Color.FromArgb(64, 64, 64);
            labelFpsConst.Location = new Point(37, 85);
            labelFpsConst.Name = "labelFpsConst";
            labelFpsConst.Size = new Size(88, 20);
            labelFpsConst.TabIndex = 18;
            labelFpsConst.Text = "سرعت (FPS):";
            // 
            // labelQualityConst
            // 
            labelQualityConst.AutoSize = true;
            labelQualityConst.ForeColor = Color.FromArgb(64, 64, 64);
            labelQualityConst.Location = new Point(23, 55);
            labelQualityConst.Name = "labelQualityConst";
            labelQualityConst.Size = new Size(96, 20);
            labelQualityConst.TabIndex = 17;
            labelQualityConst.Text = "کیفیت تصویر:";
            // 
            // buttonApply
            // 
            buttonApply.BackColor = Color.FromArgb(255, 128, 0);
            buttonApply.FlatAppearance.BorderSize = 0;
            buttonApply.FlatStyle = FlatStyle.Flat;
            buttonApply.ForeColor = Color.White;
            buttonApply.Location = new Point(17, 157);
            buttonApply.Margin = new Padding(3, 4, 3, 4);
            buttonApply.Name = "buttonApply";
            buttonApply.Size = new Size(217, 47);
            buttonApply.TabIndex = 16;
            buttonApply.Text = "اعمال تنظیمات";
            buttonApply.UseVisualStyleBackColor = false;
            buttonApply.Click += buttonApply_Click;
            // 
            // panelAgentSettingsCard
            // 
            panelAgentSettingsCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelAgentSettingsCard.BackColor = Color.White;
            panelAgentSettingsCard.BorderStyle = BorderStyle.FixedSingle;
            panelAgentSettingsCard.Controls.Add(buttonOpenFolderOutcoming);
            panelAgentSettingsCard.Controls.Add(buttonOpenFolderIncoming);
            panelAgentSettingsCard.Controls.Add(checkBoxSaceOutcomingSession);
            panelAgentSettingsCard.Controls.Add(checkBoxSaceIncomingSession);
            panelAgentSettingsCard.Controls.Add(buttonCloseAgentSettings);
            panelAgentSettingsCard.Controls.Add(labelAgentSettingsConst);
            panelAgentSettingsCard.Controls.Add(buttonAgentPasswordChange);
            panelAgentSettingsCard.Location = new Point(766, 70);
            panelAgentSettingsCard.Margin = new Padding(3, 4, 3, 4);
            panelAgentSettingsCard.Name = "panelAgentSettingsCard";
            panelAgentSettingsCard.Size = new Size(251, 186);
            panelAgentSettingsCard.TabIndex = 24;
            panelAgentSettingsCard.Visible = false;
            // 
            // buttonOpenFolderOutcoming
            // 
            buttonOpenFolderOutcoming.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOpenFolderOutcoming.BackColor = Color.FromArgb(255, 128, 0);
            buttonOpenFolderOutcoming.FlatAppearance.BorderSize = 0;
            buttonOpenFolderOutcoming.FlatStyle = FlatStyle.Flat;
            buttonOpenFolderOutcoming.ForeColor = Color.White;
            buttonOpenFolderOutcoming.Location = new Point(17, 88);
            buttonOpenFolderOutcoming.Margin = new Padding(3, 4, 3, 4);
            buttonOpenFolderOutcoming.Name = "buttonOpenFolderOutcoming";
            buttonOpenFolderOutcoming.Size = new Size(32, 29);
            buttonOpenFolderOutcoming.TabIndex = 24;
            buttonOpenFolderOutcoming.Text = "📁";
            toolTipCopyMyComputerId.SetToolTip(buttonOpenFolderOutcoming, "باز کردن پوشه ویدو های ذخیره شده از تماس های خروجی");
            buttonOpenFolderOutcoming.UseVisualStyleBackColor = false;
            buttonOpenFolderOutcoming.Click += buttonOpenFolderOutcoming_Click;
            // 
            // buttonOpenFolderIncoming
            // 
            buttonOpenFolderIncoming.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOpenFolderIncoming.BackColor = Color.FromArgb(255, 128, 0);
            buttonOpenFolderIncoming.FlatAppearance.BorderSize = 0;
            buttonOpenFolderIncoming.FlatStyle = FlatStyle.Flat;
            buttonOpenFolderIncoming.ForeColor = Color.White;
            buttonOpenFolderIncoming.Location = new Point(17, 52);
            buttonOpenFolderIncoming.Margin = new Padding(3, 4, 3, 4);
            buttonOpenFolderIncoming.Name = "buttonOpenFolderIncoming";
            buttonOpenFolderIncoming.Size = new Size(32, 29);
            buttonOpenFolderIncoming.TabIndex = 23;
            buttonOpenFolderIncoming.Text = "📁";
            toolTipCopyMyComputerId.SetToolTip(buttonOpenFolderIncoming, "باز کردن پوشه ویدو های ذخیره شده از تماس های ورودی");
            buttonOpenFolderIncoming.UseVisualStyleBackColor = false;
            buttonOpenFolderIncoming.Click += buttonOpenFolderIncoming_Click;
            // 
            // checkBoxSaceOutcomingSession
            // 
            checkBoxSaceOutcomingSession.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkBoxSaceOutcomingSession.AutoSize = true;
            checkBoxSaceOutcomingSession.Checked = true;
            checkBoxSaceOutcomingSession.CheckState = CheckState.Checked;
            checkBoxSaceOutcomingSession.Font = new Font("Segoe UI", 9F);
            checkBoxSaceOutcomingSession.ForeColor = Color.FromArgb(64, 64, 64);
            checkBoxSaceOutcomingSession.Location = new Point(60, 88);
            checkBoxSaceOutcomingSession.Name = "checkBoxSaceOutcomingSession";
            checkBoxSaceOutcomingSession.RightToLeft = RightToLeft.Yes;
            checkBoxSaceOutcomingSession.Size = new Size(169, 24);
            checkBoxSaceOutcomingSession.TabIndex = 22;
            checkBoxSaceOutcomingSession.Text = "ذخیره انصالات خروجی";
            checkBoxSaceOutcomingSession.UseVisualStyleBackColor = true;
            checkBoxSaceOutcomingSession.CheckedChanged += checkBoxSaceOutcomingSession_CheckedChanged;
            // 
            // checkBoxSaceIncomingSession
            // 
            checkBoxSaceIncomingSession.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkBoxSaceIncomingSession.AutoSize = true;
            checkBoxSaceIncomingSession.Checked = true;
            checkBoxSaceIncomingSession.CheckState = CheckState.Checked;
            checkBoxSaceIncomingSession.Font = new Font("Segoe UI", 9F);
            checkBoxSaceIncomingSession.ForeColor = Color.FromArgb(64, 64, 64);
            checkBoxSaceIncomingSession.Location = new Point(63, 54);
            checkBoxSaceIncomingSession.Name = "checkBoxSaceIncomingSession";
            checkBoxSaceIncomingSession.RightToLeft = RightToLeft.Yes;
            checkBoxSaceIncomingSession.Size = new Size(165, 24);
            checkBoxSaceIncomingSession.TabIndex = 21;
            checkBoxSaceIncomingSession.Text = "ذخیره انصالات ورودی";
            checkBoxSaceIncomingSession.UseVisualStyleBackColor = true;
            checkBoxSaceIncomingSession.CheckedChanged += checkBoxSaceIncomingSession_CheckedChanged;
            // 
            // buttonCloseAgentSettings
            // 
            buttonCloseAgentSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonCloseAgentSettings.BackColor = Color.FromArgb(255, 128, 0);
            buttonCloseAgentSettings.FlatAppearance.BorderSize = 0;
            buttonCloseAgentSettings.FlatStyle = FlatStyle.Flat;
            buttonCloseAgentSettings.Font = new Font("Segoe UI", 9F);
            buttonCloseAgentSettings.ForeColor = Color.White;
            buttonCloseAgentSettings.Location = new Point(200, 13);
            buttonCloseAgentSettings.Margin = new Padding(3, 4, 3, 4);
            buttonCloseAgentSettings.Name = "buttonCloseAgentSettings";
            buttonCloseAgentSettings.Size = new Size(34, 32);
            buttonCloseAgentSettings.TabIndex = 8;
            buttonCloseAgentSettings.Text = "✕";
            buttonCloseAgentSettings.UseVisualStyleBackColor = true;
            buttonCloseAgentSettings.Click += buttonSettings_Click;
            // 
            // labelAgentSettingsConst
            // 
            labelAgentSettingsConst.AutoSize = true;
            labelAgentSettingsConst.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelAgentSettingsConst.ForeColor = Color.Gray;
            labelAgentSettingsConst.Location = new Point(17, 13);
            labelAgentSettingsConst.Name = "labelAgentSettingsConst";
            labelAgentSettingsConst.Size = new Size(110, 23);
            labelAgentSettingsConst.TabIndex = 8;
            labelAgentSettingsConst.Text = "تنظیمات کاربر";
            // 
            // buttonAgentPasswordChange
            // 
            buttonAgentPasswordChange.BackColor = Color.FromArgb(255, 128, 0);
            buttonAgentPasswordChange.FlatAppearance.BorderSize = 0;
            buttonAgentPasswordChange.FlatStyle = FlatStyle.Flat;
            buttonAgentPasswordChange.ForeColor = Color.White;
            buttonAgentPasswordChange.Location = new Point(17, 125);
            buttonAgentPasswordChange.Margin = new Padding(3, 4, 3, 4);
            buttonAgentPasswordChange.Name = "buttonAgentPasswordChange";
            buttonAgentPasswordChange.Size = new Size(217, 47);
            buttonAgentPasswordChange.TabIndex = 16;
            buttonAgentPasswordChange.Text = "تغییر کلمه عبور";
            buttonAgentPasswordChange.UseVisualStyleBackColor = false;
            buttonAgentPasswordChange.Click += buttonAgentPasswordChange_Click;
            // 
            // panelAudioVideoSettings
            // 
            panelAudioVideoSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelAudioVideoSettings.BackColor = Color.White;
            panelAudioVideoSettings.BorderStyle = BorderStyle.FixedSingle;
            panelAudioVideoSettings.Controls.Add(flowLayoutPanelMicAndWebcam);
            panelAudioVideoSettings.Controls.Add(flowLayoutPanelAudioVideoSettings);
            panelAudioVideoSettings.Controls.Add(buttonCloseAudioVideoSettings);
            panelAudioVideoSettings.Controls.Add(labelAudioVideoSettingsConst);
            panelAudioVideoSettings.Controls.Add(checkBoxClipboardSharing);
            panelAudioVideoSettings.Location = new Point(506, 170);
            panelAudioVideoSettings.Margin = new Padding(3, 4, 3, 4);
            panelAudioVideoSettings.Name = "panelAudioVideoSettings";
            panelAudioVideoSettings.Size = new Size(251, 224);
            panelAudioVideoSettings.TabIndex = 25;
            panelAudioVideoSettings.Visible = false;
            // 
            // flowLayoutPanelMicAndWebcam
            // 
            flowLayoutPanelMicAndWebcam.AutoSize = true;
            flowLayoutPanelMicAndWebcam.Location = new Point(9, 44);
            flowLayoutPanelMicAndWebcam.Name = "flowLayoutPanelMicAndWebcam";
            flowLayoutPanelMicAndWebcam.Size = new Size(234, 46);
            flowLayoutPanelMicAndWebcam.TabIndex = 19;
            flowLayoutPanelMicAndWebcam.WrapContents = false;
            // 
            // flowLayoutPanelAudioVideoSettings
            // 
            flowLayoutPanelAudioVideoSettings.AutoScroll = true;
            flowLayoutPanelAudioVideoSettings.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanelAudioVideoSettings.Location = new Point(9, 140);
            flowLayoutPanelAudioVideoSettings.Name = "flowLayoutPanelAudioVideoSettings";
            flowLayoutPanelAudioVideoSettings.Padding = new Padding(3);
            flowLayoutPanelAudioVideoSettings.Size = new Size(234, 74);
            flowLayoutPanelAudioVideoSettings.TabIndex = 18;
            // 
            // buttonCloseAudioVideoSettings
            // 
            buttonCloseAudioVideoSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonCloseAudioVideoSettings.BackColor = Color.FromArgb(255, 128, 0);
            buttonCloseAudioVideoSettings.FlatAppearance.BorderSize = 0;
            buttonCloseAudioVideoSettings.FlatStyle = FlatStyle.Flat;
            buttonCloseAudioVideoSettings.Font = new Font("Segoe UI", 9F);
            buttonCloseAudioVideoSettings.ForeColor = Color.White;
            buttonCloseAudioVideoSettings.Location = new Point(200, 10);
            buttonCloseAudioVideoSettings.Margin = new Padding(3, 4, 3, 4);
            buttonCloseAudioVideoSettings.Name = "buttonCloseAudioVideoSettings";
            buttonCloseAudioVideoSettings.Size = new Size(34, 32);
            buttonCloseAudioVideoSettings.TabIndex = 17;
            buttonCloseAudioVideoSettings.Text = "✕";
            buttonCloseAudioVideoSettings.UseVisualStyleBackColor = true;
            buttonCloseAudioVideoSettings.Click += buttonCloseAudioVideoSettings_Click;
            // 
            // labelAudioVideoSettingsConst
            // 
            labelAudioVideoSettingsConst.AutoSize = true;
            labelAudioVideoSettingsConst.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelAudioVideoSettingsConst.ForeColor = Color.Gray;
            labelAudioVideoSettingsConst.Location = new Point(17, 13);
            labelAudioVideoSettingsConst.Name = "labelAudioVideoSettingsConst";
            labelAudioVideoSettingsConst.Size = new Size(172, 23);
            labelAudioVideoSettingsConst.TabIndex = 8;
            labelAudioVideoSettingsConst.Text = "مانیتور و صدا و تصویر";
            // 
            // checkBoxClipboardSharing
            // 
            checkBoxClipboardSharing.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkBoxClipboardSharing.AutoSize = true;
            checkBoxClipboardSharing.Checked = true;
            checkBoxClipboardSharing.CheckState = CheckState.Checked;
            checkBoxClipboardSharing.Font = new Font("Segoe UI", 9F);
            checkBoxClipboardSharing.ForeColor = Color.FromArgb(64, 64, 64);
            checkBoxClipboardSharing.Location = new Point(66, 107);
            checkBoxClipboardSharing.Name = "checkBoxClipboardSharing";
            checkBoxClipboardSharing.RightToLeft = RightToLeft.Yes;
            checkBoxClipboardSharing.Size = new Size(174, 24);
            checkBoxClipboardSharing.TabIndex = 20;
            checkBoxClipboardSharing.Text = "اشتراک‌گذاری کلیپ‌بورد";
            checkBoxClipboardSharing.UseVisualStyleBackColor = true;
            checkBoxClipboardSharing.CheckedChanged += checkBoxClipboardSharing_CheckedChanged;
            // 
            // pictureBox
            // 
            pictureBox.BackColor = Color.FromArgb(20, 20, 20);
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Location = new Point(0, 168);
            pictureBox.Margin = new Padding(3, 4, 3, 4);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(725, 399);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.TabIndex = 4;
            pictureBox.TabStop = false;
            pictureBox.Visible = false;
            // 
            // panelChat
            // 
            panelChat.BackColor = Color.WhiteSmoke;
            panelChat.Controls.Add(flowlayoutpanelChatHistory);
            panelChat.Controls.Add(panelTransfer);
            panelChat.Controls.Add(panelChatInput);
            panelChat.Dock = DockStyle.Right;
            panelChat.Location = new Point(725, 168);
            panelChat.Name = "panelChat";
            panelChat.Size = new Size(300, 399);
            panelChat.TabIndex = 5;
            panelChat.Visible = false;
            // 
            // flowlayoutpanelChatHistory
            // 
            flowlayoutpanelChatHistory.AllowDrop = true;
            flowlayoutpanelChatHistory.AutoScroll = true;
            flowlayoutpanelChatHistory.BackColor = Color.White;
            flowlayoutpanelChatHistory.Controls.Add(buttonUploadFile);
            flowlayoutpanelChatHistory.Dock = DockStyle.Fill;
            flowlayoutpanelChatHistory.FlowDirection = FlowDirection.TopDown;
            flowlayoutpanelChatHistory.Location = new Point(0, 0);
            flowlayoutpanelChatHistory.Name = "flowlayoutpanelChatHistory";
            flowlayoutpanelChatHistory.Size = new Size(300, 153);
            flowlayoutpanelChatHistory.TabIndex = 0;
            flowlayoutpanelChatHistory.WrapContents = false;
            // 
            // buttonUploadFile
            // 
            buttonUploadFile.Location = new Point(3, 3);
            buttonUploadFile.Name = "buttonUploadFile";
            buttonUploadFile.Size = new Size(94, 37);
            buttonUploadFile.TabIndex = 3;
            buttonUploadFile.Text = "آپلود فایل";
            buttonUploadFile.UseVisualStyleBackColor = true;
            buttonUploadFile.Visible = false;
            buttonUploadFile.Click += buttonUploadFile_Click;
            // 
            // panelTransfer
            // 
            panelTransfer.BackColor = Color.FromArgb(245, 246, 250);
            panelTransfer.Controls.Add(buttonCancelTransfer);
            panelTransfer.Controls.Add(progressBarTransfer);
            panelTransfer.Controls.Add(labelTransferStatus);
            panelTransfer.Dock = DockStyle.Bottom;
            panelTransfer.Location = new Point(0, 153);
            panelTransfer.Name = "panelTransfer";
            panelTransfer.Padding = new Padding(8);
            panelTransfer.Size = new Size(300, 73);
            panelTransfer.TabIndex = 2;
            // 
            // progressBarTransfer
            // 
            progressBarTransfer.Dock = DockStyle.Top;
            progressBarTransfer.Location = new Point(8, 8);
            progressBarTransfer.Name = "progressBarTransfer";
            progressBarTransfer.Size = new Size(284, 18);
            progressBarTransfer.TabIndex = 2;
            progressBarTransfer.Visible = false;
            // 
            // panelChatInput
            // 
            panelChatInput.BackColor = Color.White;
            panelChatInput.Controls.Add(buttonSendChat);
            panelChatInput.Controls.Add(textBoxChatInput);
            panelChatInput.Controls.Add(buttonChatSend);
            panelChatInput.Dock = DockStyle.Bottom;
            panelChatInput.Location = new Point(0, 226);
            panelChatInput.Name = "panelChatInput";
            panelChatInput.Padding = new Padding(5);
            panelChatInput.Size = new Size(300, 173);
            panelChatInput.TabIndex = 1;
            // 
            // buttonSendChat
            // 
            buttonSendChat.BackColor = Color.FromArgb(255, 128, 0);
            buttonSendChat.FlatAppearance.BorderSize = 0;
            buttonSendChat.FlatStyle = FlatStyle.Flat;
            buttonSendChat.ForeColor = Color.White;
            buttonSendChat.Location = new Point(235, 10);
            buttonSendChat.Name = "buttonSendChat";
            buttonSendChat.Size = new Size(60, 51);
            buttonSendChat.TabIndex = 2;
            buttonSendChat.Text = "ارسال";
            buttonSendChat.UseVisualStyleBackColor = false;
            buttonSendChat.Click += buttonChatSend_Click;
            // 
            // textBoxChatInput
            // 
            textBoxChatInput.BorderStyle = BorderStyle.FixedSingle;
            textBoxChatInput.Location = new Point(8, 10);
            textBoxChatInput.Name = "textBoxChatInput";
            textBoxChatInput.RightToLeft = RightToLeft.Yes;
            textBoxChatInput.Size = new Size(225, 51);
            textBoxChatInput.TabIndex = 0;
            textBoxChatInput.Text = "";
            textBoxChatInput.KeyDown += textBoxChatInput_KeyDown;
            // 
            // buttonChatSend
            // 
            buttonChatSend.BackColor = Color.FromArgb(255, 128, 0);
            buttonChatSend.Dock = DockStyle.Right;
            buttonChatSend.FlatAppearance.BorderSize = 0;
            buttonChatSend.FlatStyle = FlatStyle.Flat;
            buttonChatSend.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonChatSend.ForeColor = Color.White;
            buttonChatSend.Location = new Point(295, 5);
            buttonChatSend.Name = "buttonChatSend";
            buttonChatSend.Size = new Size(0, 163);
            buttonChatSend.TabIndex = 1;
            buttonChatSend.Text = "ارسال";
            buttonChatSend.UseVisualStyleBackColor = false;
            buttonChatSend.Visible = false;
            buttonChatSend.Click += buttonChatSend_Click;
            // 
            // panelInfo
            // 
            panelInfo.BackColor = Color.FromArgb(255, 128, 0);
            panelInfo.Controls.Add(labelProcess);
            panelInfo.Controls.Add(buttonDisplayName);
            panelInfo.Controls.Add(labelSpeedInfo);
            panelInfo.Dock = DockStyle.Bottom;
            panelInfo.Location = new Point(0, 567);
            panelInfo.Margin = new Padding(3, 4, 3, 4);
            panelInfo.Name = "panelInfo";
            panelInfo.Size = new Size(1025, 33);
            panelInfo.TabIndex = 9;
            // 
            // labelProcess
            // 
            labelProcess.AutoSize = true;
            labelProcess.ForeColor = Color.White;
            labelProcess.Location = new Point(520, 7);
            labelProcess.Name = "labelProcess";
            labelProcess.Size = new Size(18, 20);
            labelProcess.TabIndex = 18;
            labelProcess.Text = "...";
            // 
            // buttonDisplayName
            // 
            buttonDisplayName.Anchor = AnchorStyles.Left;
            buttonDisplayName.AutoSize = true;
            buttonDisplayName.BackColor = Color.FromArgb(255, 128, 0);
            buttonDisplayName.FlatAppearance.BorderSize = 0;
            buttonDisplayName.FlatStyle = FlatStyle.Flat;
            buttonDisplayName.ForeColor = Color.White;
            buttonDisplayName.Location = new Point(9, 5);
            buttonDisplayName.Margin = new Padding(0);
            buttonDisplayName.Name = "buttonDisplayName";
            buttonDisplayName.RightToLeft = RightToLeft.Yes;
            buttonDisplayName.Size = new Size(33, 30);
            buttonDisplayName.TabIndex = 17;
            buttonDisplayName.Text = "...";
            buttonDisplayName.TextAlign = ContentAlignment.MiddleLeft;
            buttonDisplayName.UseVisualStyleBackColor = false;
            buttonDisplayName.Click += buttonDisplayNameChange_Click;
            // 
            // labelSpeedInfo
            // 
            labelSpeedInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            labelSpeedInfo.ForeColor = Color.White;
            labelSpeedInfo.Location = new Point(694, 8);
            labelSpeedInfo.Name = "labelSpeedInfo";
            labelSpeedInfo.RightToLeft = RightToLeft.Yes;
            labelSpeedInfo.Size = new Size(323, 20);
            labelSpeedInfo.TabIndex = 9;
            labelSpeedInfo.Text = "...";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoValidate = AutoValidate.EnableAllowFocusChange;
            BackColor = Color.FromArgb(245, 246, 250);
            ClientSize = new Size(1025, 600);
            Controls.Add(panelAgentSettingsCard);
            Controls.Add(panelControllerSettingsCard);
            Controls.Add(panelAudioVideoSettings);
            Controls.Add(panelDashboard);
            Controls.Add(pictureBox);
            Controls.Add(panelChat);
            Controls.Add(panelConnectionBar);
            Controls.Add(panelHeader);
            Controls.Add(panelInfo);
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "دسکتاپ پشتيبان";
            Resize += MainForm_Resize;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            panelConnectionBar.ResumeLayout(false);
            panelIdInput.ResumeLayout(false);
            panelIdInput.PerformLayout();
            panelDashboard.ResumeLayout(false);
            panelRecentConnections.ResumeLayout(false);
            panelRecentConnections.PerformLayout();
            panelAgentCard.ResumeLayout(false);
            panelAgentCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxInstaLogo).EndInit();
            panelControllerSettingsCard.ResumeLayout(false);
            panelControllerSettingsCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownQuality).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownFps).EndInit();
            panelAgentSettingsCard.ResumeLayout(false);
            panelAgentSettingsCard.PerformLayout();
            panelAudioVideoSettings.ResumeLayout(false);
            panelAudioVideoSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            panelChat.ResumeLayout(false);
            flowlayoutpanelChatHistory.ResumeLayout(false);
            panelTransfer.ResumeLayout(false);
            panelChatInput.ResumeLayout(false);
            panelInfo.ResumeLayout(false);
            panelInfo.PerformLayout();
            ResumeLayout(false);

        }

        private NumericUpDown numericUpDownQuality;
        private NumericUpDown numericUpDownFps;

        #endregion

        private Button buttonSendChat;
        private Controls.GuidFormattedTextBox guidFormattedTextBoxIdRemote;
        private PictureBox pictureBoxLogo;
        private PictureBox pictureBoxInstaLogo;
        private Button buttonUploadFile;
        private Button buttonChange;
        private Label labelWebsite;
        private Label labelInsta;
        private Label labelReportConst;
        private Label labelEmail;
        private CheckBox checkBoxFullFrame;
        private Panel panelAgentSettingsCard;
        private Label labelAgentSettingsConst;
        private Button buttonAgentPasswordChange;
        private Button buttonCloseAgentSettings;
        private Button buttonCloseControllerSettings;
        private Button buttonClearLog;
        private Button buttonAudioVideoSettings;
        private Panel panelAudioVideoSettings;
        private Label labelAudioVideoSettingsConst;
        private Button buttonCloseAudioVideoSettings;
        private FlowLayoutPanel flowLayoutPanelAudioVideoSettings;
        private RichTextBox richTextBoxNews;
        private FlowLayoutPanel flowLayoutPanelMicAndWebcam;
        private Panel panelInfo;
        private Label labelSpeedInfo;
        private Button buttonDisplayName;
        private Label labelProcess;
        private Button buttonInvite;
        private Button buttonMyDeviceId;
        private ToolTip toolTipCopyMyComputerId;
        private CheckBox checkBoxSaceOutcomingSession;
        private CheckBox checkBoxSaceIncomingSession;
        private Button buttonOpenFolderOutcoming;
        private Button buttonOpenFolderIncoming;
        private Label labelRemoteIdConst;
        private Controls.GuidFormattedTextBox guidFormattedTextBoxIdLocal;
    }
}