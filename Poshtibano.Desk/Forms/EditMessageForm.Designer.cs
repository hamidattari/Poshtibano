namespace Poshtibano.Desk.Forms
{
    partial class EditMessageForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        private Panel panelHeader;
        private Label labelTitle;
        private Button buttonClose;
        private Button buttonMinimize;

        private TextBox textBoxMessage;
        private Button buttonOK;
        private Button buttonCancel;
        private Label labelInfo;

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
            panelHeader = new Panel();
            labelTitle = new Label();
            buttonClose = new Button();
            buttonMinimize = new Button();
            textBoxMessage = new TextBox();
            panelTextBox = new Panel();
            buttonOK = new Button();
            buttonCancel = new Button();
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
            panelHeader.Size = new Size(532, 50);
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
            labelTitle.Size = new Size(143, 28);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "✏️ ویرایش پیام";
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
            buttonClose.Location = new Point(482, 0);
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
            buttonMinimize.Location = new Point(431, 0);
            buttonMinimize.Name = "buttonMinimize";
            buttonMinimize.Size = new Size(50, 50);
            buttonMinimize.TabIndex = 2;
            buttonMinimize.Text = "—";
            buttonMinimize.UseVisualStyleBackColor = false;
            buttonMinimize.Visible = false;
            buttonMinimize.Click += ButtonMinimize_Click;
            // 
            // textBoxMessage
            // 
            textBoxMessage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxMessage.BackColor = Color.White;
            textBoxMessage.BorderStyle = BorderStyle.None;
            textBoxMessage.Font = new Font("Segoe UI", 10F);
            textBoxMessage.ForeColor = Color.FromArgb(50, 50, 50);
            textBoxMessage.Location = new Point(0, 98);
            textBoxMessage.Multiline = true;
            textBoxMessage.Name = "textBoxMessage";
            textBoxMessage.ScrollBars = ScrollBars.Vertical;
            textBoxMessage.Size = new Size(532, 130);
            textBoxMessage.TabIndex = 0;
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
            buttonOK.Location = new Point(141, 234);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(120, 40);
            buttonOK.TabIndex = 2;
            buttonOK.Text = "✓ ثبت";
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
            buttonCancel.Location = new Point(267, 234);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(120, 40);
            buttonCancel.TabIndex = 3;
            buttonCancel.Text = "✕ انصراف";
            buttonCancel.UseVisualStyleBackColor = false;
            buttonCancel.MouseEnter += ButtonCancel_MouseEnter;
            buttonCancel.MouseLeave += ButtonCancel_MouseLeave;
            // 
            // labelInfo
            // 
            labelInfo.AutoSize = true;
            labelInfo.Font = new Font("Segoe UI", 9F);
            labelInfo.ForeColor = Color.FromArgb(100, 100, 100);
            labelInfo.Location = new Point(357, 65);
            labelInfo.Margin = new Padding(20, 15, 20, 10);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(157, 20);
            labelInfo.TabIndex = 1;
            labelInfo.Text = "پیام خود را ویرایش کنید:";
            // 
            // EditMessageForm
            // 
            AcceptButton = buttonOK;
            BackColor = Color.FromArgb(245, 246, 250);
            CancelButton = buttonCancel;
            ClientSize = new Size(532, 286);
            Controls.Add(textBoxMessage);
            Controls.Add(labelInfo);
            Controls.Add(buttonOK);
            Controls.Add(buttonCancel);
            Controls.Add(panelHeader);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MinimizeBox = false;
            Name = "EditMessageForm";
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