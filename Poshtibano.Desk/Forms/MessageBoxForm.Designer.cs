namespace Poshtibano.Desk.Forms
{
    partial class MessageBoxForm
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
            pictureBoxIcon = new PictureBox();
            btnDeny = new Button();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxIcon).BeginInit();
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
            labelMessage.Location = new Point(90, 88);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(380, 35);
            labelMessage.TabIndex = 2;
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
            // btnDeny
            // 
            btnDeny.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDeny.BackColor = Color.FromArgb(231, 76, 60);
            btnDeny.Cursor = Cursors.Hand;
            btnDeny.FlatAppearance.BorderSize = 0;
            btnDeny.FlatStyle = FlatStyle.Flat;
            btnDeny.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnDeny.ForeColor = Color.White;
            btnDeny.Location = new Point(187, 152);
            btnDeny.Name = "btnDeny";
            btnDeny.Size = new Size(110, 45);
            btnDeny.TabIndex = 5;
            btnDeny.Text = "✕ ببند";
            btnDeny.UseVisualStyleBackColor = false;
            btnDeny.Click += buttonDeny_Click;
            btnDeny.MouseEnter += buttonDeny_MouseEnter;
            btnDeny.MouseLeave += buttonDeny_MouseLeave;
            // 
            // MessageBoxForm
            // 
            BackColor = Color.FromArgb(245, 246, 250);
            ClientSize = new Size(482, 209);
            Controls.Add(labelMessage);
            Controls.Add(pictureBoxIcon);
            Controls.Add(btnDeny);
            Controls.Add(panelHeader);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "MessageBoxForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "درخواست دسترسی";
            TopMost = true;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxIcon).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private Label labelTitle;
        private Button buttonClose;

        private Label labelMessage;
        private PictureBox pictureBoxIcon;
        private Button btnDeny;
    }
}