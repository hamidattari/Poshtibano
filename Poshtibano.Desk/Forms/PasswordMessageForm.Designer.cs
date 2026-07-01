namespace Poshtibano.Desk.Forms
{
    partial class PasswordMessageForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Panel panelHeader;
        private Label labelTitle;
        private Button buttonClose;
        private Button buttonMinimize;

        private TextBox textBoxPassword;
        private Button buttonOK;
        private Label labelInfo;

        private Panel panelTextBox;
        private Button buttonTogglePassword;

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
            panelHeader = new Panel();
            labelTitle = new Label();
            buttonClose = new Button();
            buttonMinimize = new Button();
            textBoxPassword = new TextBox();
            panelTextBox = new Panel();
            buttonTogglePassword = new Button();
            buttonOK = new Button();
            labelInfo = new Label();
            panelHeader.SuspendLayout();
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
            panelHeader.Size = new Size(363, 50);
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
            labelTitle.Size = new Size(107, 28);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🔒 رمزعبور";
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
            buttonClose.Location = new Point(314, 0);
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
            buttonMinimize.Location = new Point(262, 0);
            buttonMinimize.Name = "buttonMinimize";
            buttonMinimize.Size = new Size(50, 50);
            buttonMinimize.TabIndex = 2;
            buttonMinimize.Text = "—";
            buttonMinimize.UseVisualStyleBackColor = false;
            buttonMinimize.Visible = false;
            buttonMinimize.Click += ButtonMinimize_Click;
            // 
            // textBoxPassword
            // 
            textBoxPassword.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxPassword.BackColor = Color.White;
            textBoxPassword.BorderStyle = BorderStyle.None;
            textBoxPassword.Font = new Font("Segoe UI", 12F);
            textBoxPassword.ForeColor = Color.FromArgb(50, 50, 50);
            textBoxPassword.Location = new Point(41, 98);
            textBoxPassword.Name = "textBoxPassword";
            textBoxPassword.PasswordChar = '*';
            textBoxPassword.ScrollBars = ScrollBars.Vertical;
            textBoxPassword.Size = new Size(235, 27);
            textBoxPassword.TabIndex = 0;
            textBoxPassword.TextAlign = HorizontalAlignment.Center;
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
            // buttonTogglePassword
            // 
            buttonTogglePassword.BackColor = Color.Transparent;
            buttonTogglePassword.FlatAppearance.BorderSize = 0;
            buttonTogglePassword.FlatStyle = FlatStyle.Flat;
            buttonTogglePassword.Font = new Font("Segoe UI", 12F);
            buttonTogglePassword.ForeColor = Color.FromArgb(100, 100, 100);
            buttonTogglePassword.Location = new Point(276, 92);
            buttonTogglePassword.Name = "buttonTogglePassword";
            buttonTogglePassword.Size = new Size(36, 36);
            buttonTogglePassword.TabIndex = 3;
            buttonTogglePassword.Text = "👁";
            buttonTogglePassword.UseVisualStyleBackColor = false;
            buttonTogglePassword.Click += ButtonTogglePassword_Click;
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
            buttonOK.Location = new Point(115, 140);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(120, 40);
            buttonOK.TabIndex = 2;
            buttonOK.Text = "✓ انتخاب";
            buttonOK.UseVisualStyleBackColor = false;
            buttonOK.Click += ButtonOK_Click;
            buttonOK.MouseEnter += ButtonOK_MouseEnter;
            buttonOK.MouseLeave += ButtonOK_MouseLeave;
            // 
            // labelInfo
            // 
            labelInfo.AutoSize = true;
            labelInfo.Font = new Font("Segoe UI", 9F);
            labelInfo.ForeColor = Color.FromArgb(100, 100, 100);
            labelInfo.Location = new Point(41, 65);
            labelInfo.Margin = new Padding(20, 15, 20, 10);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(260, 20);
            labelInfo.TabIndex = 1;
            labelInfo.Text = "رمز عبور را وارد کنید (میتواند خالی باشد)";
            // 
            // PasswordMessageForm
            // 
            AcceptButton = buttonOK;
            BackColor = Color.FromArgb(245, 246, 250);
            ClientSize = new Size(363, 204);
            Controls.Add(buttonTogglePassword);
            Controls.Add(textBoxPassword);
            Controls.Add(labelInfo);
            Controls.Add(buttonOK);
            Controls.Add(panelHeader);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MinimizeBox = false;
            Name = "PasswordMessageForm";
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ویرایش پیام";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}