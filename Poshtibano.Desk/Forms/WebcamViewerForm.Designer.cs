namespace Poshtibano.Desk.Forms
{
    partial class WebcamViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Panel panelHeader;
        private Label labelTitle;
        private Button buttonClose;
        protected PictureBox pictureBox;

        #region Windows Form Designer generated code
        /// 
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// 
        private void InitializeComponent()
        {
            panelHeader = new Panel();
            labelTitle = new Label();
            buttonClose = new Button();
            pictureBox = new PictureBox();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.AutoSize = true;
            panelHeader.BackColor = Color.FromArgb(255, 128, 0);
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(buttonClose);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Margin = new Padding(0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(640, 53);
            panelHeader.TabIndex = 0;
            panelHeader.MouseDown += panelHeader_MouseDown;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(20, 12);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(127, 28);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "📷 Webcam";
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
            buttonClose.Location = new Point(590, 0);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(50, 50);
            buttonClose.TabIndex = 1;
            buttonClose.Text = "✕";
            buttonClose.UseVisualStyleBackColor = false;
            buttonClose.Click += buttonClose_Click;
            // 
            // pictureBox
            // 
            pictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox.BackColor = Color.Black;
            pictureBox.Location = new Point(0, 50);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(640, 430);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.TabIndex = 1;
            pictureBox.TabStop = false;
            // 
            // WebcamViewerForm
            // 
            BackColor = Color.FromArgb(245, 246, 250);
            ClientSize = new Size(640, 480);
            Controls.Add(pictureBox);
            Controls.Add(panelHeader);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(320, 240);
            Name = "WebcamViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}