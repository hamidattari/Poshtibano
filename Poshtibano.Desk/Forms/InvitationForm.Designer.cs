namespace Poshtibano.Desk.Forms
{
    partial class InvitationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        private Panel panelHeader;
        private Label labelTitle;
        private Button buttonClose;
        private Button buttonMinimize;
        private Button buttonOK;
        private Button buttonCancel;

        private Panel panelTextBox;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panelHeader = new Panel();
            labelTitle = new Label();
            buttonClose = new Button();
            buttonMinimize = new Button();
            panelTextBox = new Panel();
            buttonOK = new Button();
            buttonCancel = new Button();
            panelIdInput = new Panel();
            buttonInvite = new Button();
            guidFormattedTextBoxId = new Poshtibano.Desk.Controls.GuidFormattedTextBox();
            labelIdConst = new Label();
            panelInfo = new Panel();
            label1 = new Label();
            panelHeader.SuspendLayout();
            panelIdInput.SuspendLayout();
            panelInfo.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(255, 128, 0);
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(buttonClose);
            panelHeader.Controls.Add(buttonMinimize);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Margin = new Padding(0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(469, 50);
            panelHeader.TabIndex = 4;
            panelHeader.MouseDown += Header_MouseDown;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(20, 12);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(96, 28);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "⚡ دعوت";
            labelTitle.MouseDown += Header_MouseDown;
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.None;
            buttonClose.BackColor = Color.FromArgb(255, 128, 0);
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.FlatStyle = FlatStyle.Flat;
            buttonClose.Font = new Font("Segoe UI", 9F);
            buttonClose.ForeColor = Color.White;
            buttonClose.Location = new Point(417, 1);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(50, 50);
            buttonClose.TabIndex = 1;
            buttonClose.Text = "✕";
            buttonClose.UseVisualStyleBackColor = false;
            buttonClose.Click += ButtonClose_Click;
            // 
            // buttonMinimize
            // 
            buttonMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonMinimize.BackColor = Color.FromArgb(255, 128, 0);
            buttonMinimize.FlatAppearance.BorderSize = 0;
            buttonMinimize.FlatStyle = FlatStyle.Flat;
            buttonMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonMinimize.ForeColor = Color.White;
            buttonMinimize.Location = new Point(366, 1);
            buttonMinimize.Name = "buttonMinimize";
            buttonMinimize.Size = new Size(50, 50);
            buttonMinimize.TabIndex = 2;
            buttonMinimize.Text = "—";
            buttonMinimize.UseVisualStyleBackColor = false;
            buttonMinimize.Visible = false;
            buttonMinimize.Click += ButtonMinimize_Click;
            // 
            // panelTextBox
            // 
            panelTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelTextBox.BackColor = Color.White;
            panelTextBox.BorderStyle = BorderStyle.FixedSingle;
            panelTextBox.Location = new Point(20, 95);
            panelTextBox.Name = "panelTextBox";
            panelTextBox.Size = new Size(300, 150);
            panelTextBox.TabIndex = 0;
            // 
            // buttonOK
            // 
            buttonOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonOK.BackColor = Color.FromArgb(255, 128, 0);
            buttonOK.Cursor = Cursors.Hand;
            buttonOK.DialogResult = DialogResult.OK;
            buttonOK.FlatAppearance.BorderSize = 0;
            buttonOK.FlatStyle = FlatStyle.Flat;
            buttonOK.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonOK.ForeColor = Color.White;
            buttonOK.Location = new Point(113, 263);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(120, 40);
            buttonOK.TabIndex = 2;
            buttonOK.Text = "✓ دعوت";
            buttonOK.UseVisualStyleBackColor = false;
            buttonOK.Click += ButtonOK_Click;
            buttonOK.MouseEnter += ButtonOK_MouseEnter;
            buttonOK.MouseLeave += ButtonOK_MouseLeave;
            // 
            // buttonCancel
            // 
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.BackColor = Color.FromArgb(200, 200, 200);
            buttonCancel.Cursor = Cursors.Hand;
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.FlatAppearance.BorderSize = 0;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonCancel.ForeColor = Color.FromArgb(50, 50, 50);
            buttonCancel.Location = new Point(239, 263);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(120, 40);
            buttonCancel.TabIndex = 3;
            buttonCancel.Text = "✕ انصراف";
            buttonCancel.UseVisualStyleBackColor = false;
            buttonCancel.Click += buttonCancel_Click;
            buttonCancel.MouseEnter += ButtonCancel_MouseEnter;
            buttonCancel.MouseLeave += ButtonCancel_MouseLeave;
            // 
            // panelIdInput
            // 
            panelIdInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelIdInput.BackColor = Color.FromArgb(245, 246, 250);
            panelIdInput.Controls.Add(buttonInvite);
            panelIdInput.Controls.Add(guidFormattedTextBoxId);
            panelIdInput.Controls.Add(labelIdConst);
            panelIdInput.Location = new Point(17, 189);
            panelIdInput.Margin = new Padding(3, 4, 3, 4);
            panelIdInput.Name = "panelIdInput";
            panelIdInput.Size = new Size(438, 67);
            panelIdInput.TabIndex = 5;
            // 
            // buttonInvite
            // 
            buttonInvite.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonInvite.BackColor = Color.FromArgb(255, 128, 0);
            buttonInvite.FlatAppearance.BorderSize = 0;
            buttonInvite.FlatStyle = FlatStyle.Flat;
            buttonInvite.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonInvite.ForeColor = Color.White;
            buttonInvite.Location = new Point(739, 10);
            buttonInvite.Margin = new Padding(3, 4, 3, 4);
            buttonInvite.Name = "buttonInvite";
            buttonInvite.Size = new Size(44, 46);
            buttonInvite.TabIndex = 5;
            buttonInvite.Text = "⚡";
            buttonInvite.UseVisualStyleBackColor = false;
            // 
            // guidFormattedTextBoxId
            // 
            guidFormattedTextBoxId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            guidFormattedTextBoxId.BackColor = Color.White;
            guidFormattedTextBoxId.BorderStyle = BorderStyle.None;
            guidFormattedTextBoxId.CopyOnly = false;
            guidFormattedTextBoxId.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            guidFormattedTextBoxId.ForeColor = Color.Black;
            guidFormattedTextBoxId.GuidValue = new Guid("c4653600-ffff-ffff-8899-aabbccddeeff");
            guidFormattedTextBoxId.Location = new Point(161, 12);
            guidFormattedTextBoxId.Margin = new Padding(3, 4, 3, 4);
            guidFormattedTextBoxId.MaxLength = 15;
            guidFormattedTextBoxId.Name = "guidFormattedTextBoxId";
            guidFormattedTextBoxId.Size = new Size(256, 40);
            guidFormattedTextBoxId.Spacer = '-';
            guidFormattedTextBoxId.TabIndex = 2;
            guidFormattedTextBoxId.Text = "0-000-000-000";
            guidFormattedTextBoxId.TextAlign = HorizontalAlignment.Center;
            // 
            // labelIdConst
            // 
            labelIdConst.AutoSize = true;
            labelIdConst.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            labelIdConst.ForeColor = Color.Gray;
            labelIdConst.Location = new Point(17, 17);
            labelIdConst.Name = "labelIdConst";
            labelIdConst.Size = new Size(127, 28);
            labelIdConst.TabIndex = 0;
            labelIdConst.Text = "شماره دستگاه";
            // 
            // panelInfo
            // 
            panelInfo.BackColor = Color.FromArgb(240, 248, 255);
            panelInfo.BorderStyle = BorderStyle.FixedSingle;
            panelInfo.Controls.Add(label1);
            panelInfo.Location = new Point(17, 62);
            panelInfo.Name = "panelInfo";
            panelInfo.Size = new Size(438, 121);
            panelInfo.TabIndex = 6;
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI", 9F);
            label1.ForeColor = Color.FromArgb(100, 100, 100);
            label1.Location = new Point(4, 5);
            label1.Name = "label1";
            label1.RightToLeft = RightToLeft.Yes;
            label1.Size = new Size(429, 112);
            label1.TabIndex = 0;
            label1.Text = "⚠️ به آدرس مورد نظر دعوتنامه برای دسترسی به موارد زیربفرستید\r\n\r\n  • صفحه نمایش شما را ببیند\r\n  • موس و کیبورد شما را کنترل کند";
            // 
            // InvitationForm
            // 
            AcceptButton = buttonOK;
            BackColor = Color.FromArgb(245, 246, 250);
            CancelButton = buttonCancel;
            ClientSize = new Size(469, 315);
            Controls.Add(panelInfo);
            Controls.Add(panelIdInput);
            Controls.Add(buttonOK);
            Controls.Add(buttonCancel);
            Controls.Add(panelHeader);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MinimizeBox = false;
            Name = "InvitationForm";
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ویرایش پیام";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelIdInput.ResumeLayout(false);
            panelIdInput.PerformLayout();
            panelInfo.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panelIdInput;
        private Button buttonInvite;
        private Controls.GuidFormattedTextBox guidFormattedTextBoxId;
        private Label labelIdConst;
        private Panel panelInfo;
        private Label label1;
    }
}