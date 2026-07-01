using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Poshtibano.Desk.Forms
{
    partial class ClipboardFileOfferForm
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

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            labelMessage = new Label();
            buttonAccept = new Button();
            buttonReject = new Button();
            timerAutoClose = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // labelMessage
            // 
            labelMessage.Font = new System.Drawing.Font("Segoe UI", 9F);
            labelMessage.ForeColor = Color.FromArgb(64, 64, 64);
            labelMessage.Location = new Point(9, 7);
            labelMessage.Name = "labelMessage";
            labelMessage.RightToLeft = RightToLeft.Yes;
            labelMessage.Size = new Size(434, 60);
            labelMessage.TabIndex = 0;
            labelMessage.Text = "Loading...";
            labelMessage.TextAlign = ContentAlignment.MiddleRight;
            // 
            // buttonAccept
            // 
            buttonAccept.BackColor = Color.FromArgb(76, 175, 80);
            buttonAccept.Cursor = Cursors.Hand;
            buttonAccept.FlatAppearance.BorderSize = 0;
            buttonAccept.FlatStyle = FlatStyle.Flat;
            buttonAccept.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
            buttonAccept.ForeColor = Color.White;
            buttonAccept.Location = new Point(326, 73);
            buttonAccept.Margin = new Padding(3, 4, 3, 4);
            buttonAccept.Name = "buttonAccept";
            buttonAccept.Size = new Size(114, 47);
            buttonAccept.TabIndex = 1;
            buttonAccept.Text = "✅ دریافت";
            buttonAccept.UseVisualStyleBackColor = false;
            buttonAccept.Click += btnAccept_Click;
            // 
            // buttonReject
            // 
            buttonReject.BackColor = Color.FromArgb(244, 67, 54);
            buttonReject.Cursor = Cursors.Hand;
            buttonReject.FlatAppearance.BorderSize = 0;
            buttonReject.FlatStyle = FlatStyle.Flat;
            buttonReject.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
            buttonReject.ForeColor = Color.White;
            buttonReject.Location = new Point(207, 73);
            buttonReject.Margin = new Padding(3, 4, 3, 4);
            buttonReject.Name = "buttonReject";
            buttonReject.Size = new Size(114, 47);
            buttonReject.TabIndex = 2;
            buttonReject.Text = "❌ رد";
            buttonReject.UseVisualStyleBackColor = false;
            buttonReject.Click += btnReject_Click;
            // 
            // timerAutoClose
            // 
            timerAutoClose.Interval = 30000;
            timerAutoClose.Tick += timerAutoClose_Tick;
            // 
            // ClipboardFileOfferForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 248, 225);
            ClientSize = new Size(457, 133);
            Controls.Add(buttonReject);
            Controls.Add(buttonAccept);
            Controls.Add(labelMessage);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "ClipboardFileOfferForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "File Offer";
            Load += ClipboardNotificationForm_Load;
            Paint += ClipboardNotificationForm_Paint;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.Button buttonAccept;
        private System.Windows.Forms.Button buttonReject;
        private System.Windows.Forms.Timer timerAutoClose;
    }
}