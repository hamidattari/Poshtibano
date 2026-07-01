namespace Poshtibano.Desk.Forms
{
    partial class AccessRequestForm
    {

        private System.ComponentModel.IContainer components = null;

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
            labelMessage = new Label();
            labelRequester = new Label();
            pictureBoxIcon = new PictureBox();
            buttonAllow = new Button();
            buttonDeny = new Button();
            panelInfo = new Panel();
            labelInfo = new Label();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxIcon).BeginInit();
            panelInfo.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(255, 128, 0);
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(buttonClose);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Margin = new Padding(0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(482, 50);
            panelHeader.TabIndex = 6;
            panelHeader.MouseDown += panelHeader_MouseDown;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(20, 12);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(200, 28);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🔐 درخواست دسترسی";
            labelTitle.MouseDown += panelHeader_MouseDown;
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClose.BackColor = Color.FromArgb(255, 128, 0);
            buttonClose.Cursor = Cursors.Hand;
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.FlatStyle = FlatStyle.Flat;
            buttonClose.Font = new Font("Segoe UI", 9F);
            buttonClose.ForeColor = Color.White;
            buttonClose.Location = new Point(432, 0);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(50, 50);
            buttonClose.TabIndex = 1;
            buttonClose.Text = "✕";
            buttonClose.UseVisualStyleBackColor = false;
            buttonClose.Click += buttonClose_Click;
            // 
            // labelMessage
            // 
            labelMessage.Font = new Font("Segoe UI", 11F);
            labelMessage.ForeColor = Color.FromArgb(50, 50, 50);
            labelMessage.Location = new Point(100, 70);
            labelMessage.Name = "labelMessage";
            labelMessage.RightToLeft = RightToLeft.Yes;
            labelMessage.Size = new Size(360, 60);
            labelMessage.TabIndex = 2;
            labelMessage.Text = "یک کاربر از راه دور درخواست دسترسی به دسکتاپ شما را فرستاده است.\n\nآیا اجازه می‌دهید؟";
            // 
            // labelRequester
            // 
            labelRequester.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            labelRequester.ForeColor = Color.FromArgb(255, 128, 0);
            labelRequester.Location = new Point(100, 130);
            labelRequester.Name = "labelRequester";
            labelRequester.Size = new Size(360, 30);
            labelRequester.TabIndex = 1;
            // 
            // pictureBoxIcon
            // 
            pictureBoxIcon.Location = new Point(20, 70);
            pictureBoxIcon.Name = "pictureBoxIcon";
            pictureBoxIcon.Size = new Size(64, 64);
            pictureBoxIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxIcon.TabIndex = 3;
            pictureBoxIcon.TabStop = false;
            // 
            // buttonAllow
            // 
            buttonAllow.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAllow.BackColor = Color.FromArgb(46, 204, 113);
            buttonAllow.Cursor = Cursors.Hand;
            buttonAllow.FlatAppearance.BorderSize = 0;
            buttonAllow.FlatStyle = FlatStyle.Flat;
            buttonAllow.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonAllow.ForeColor = Color.White;
            buttonAllow.Location = new Point(240, 289);
            buttonAllow.Name = "buttonAllow";
            buttonAllow.Size = new Size(110, 44);
            buttonAllow.TabIndex = 4;
            buttonAllow.Text = "✓ اجازه دادن";
            buttonAllow.UseVisualStyleBackColor = false;
            buttonAllow.Click += buttonAllow_Click;
            buttonAllow.MouseEnter += buttonAllow_MouseEnter;
            buttonAllow.MouseLeave += buttonAllow_MouseLeave;
            // 
            // buttonDeny
            // 
            buttonDeny.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonDeny.BackColor = Color.FromArgb(231, 76, 60);
            buttonDeny.Cursor = Cursors.Hand;
            buttonDeny.FlatAppearance.BorderSize = 0;
            buttonDeny.FlatStyle = FlatStyle.Flat;
            buttonDeny.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            buttonDeny.ForeColor = Color.White;
            buttonDeny.Location = new Point(110, 289);
            buttonDeny.Name = "buttonDeny";
            buttonDeny.Size = new Size(110, 44);
            buttonDeny.TabIndex = 5;
            buttonDeny.Text = "✕ رد کردن";
            buttonDeny.UseVisualStyleBackColor = false;
            buttonDeny.Click += buttonDeny_Click;
            buttonDeny.MouseEnter += buttonDeny_MouseEnter;
            buttonDeny.MouseLeave += buttonDeny_MouseLeave;
            // 
            // panelInfo
            // 
            panelInfo.BackColor = Color.FromArgb(240, 248, 255);
            panelInfo.BorderStyle = BorderStyle.FixedSingle;
            panelInfo.Controls.Add(labelInfo);
            panelInfo.Location = new Point(54, 163);
            panelInfo.Name = "panelInfo";
            panelInfo.Size = new Size(356, 103);
            panelInfo.TabIndex = 0;
            // 
            // labelInfo
            // 
            labelInfo.Font = new Font("Segoe UI", 9F);
            labelInfo.ForeColor = Color.FromArgb(100, 100, 100);
            labelInfo.Location = new Point(20, 5);
            labelInfo.Name = "labelInfo";
            labelInfo.RightToLeft = RightToLeft.Yes;
            labelInfo.Size = new Size(300, 90);
            labelInfo.TabIndex = 0;
            labelInfo.Text = "⚠️ اگر اجازه دهید، این کاربر می‌تواند:\n\n  • صفحه نمایش شما را ببیند\n  • موس و کیبورد شما را کنترل کند";
            // 
            // AccessRequestForm
            // 
            BackColor = Color.FromArgb(245, 246, 250);
            ClientSize = new Size(482, 345);
            Controls.Add(panelInfo);
            Controls.Add(labelRequester);
            Controls.Add(labelMessage);
            Controls.Add(pictureBoxIcon);
            Controls.Add(buttonAllow);
            Controls.Add(buttonDeny);
            Controls.Add(panelHeader);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "AccessRequestForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "درخواست دسترسی";
            TopMost = true;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxIcon).EndInit();
            panelInfo.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private Label labelTitle;
        private Button buttonClose;

        private Label labelMessage;
        private Label labelRequester;
        private PictureBox pictureBoxIcon;

        private Button buttonAllow;
        private Button buttonDeny;

        private Panel panelInfo;
        private Label labelInfo;
    }
}