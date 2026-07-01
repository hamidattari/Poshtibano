// Updated MediaPermissionRequestForm.cs
using Poshtibano.Common;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace Poshtibano.Desk.Forms
{
    public partial class MediaPermissionRequestForm : Form
    {
        public bool PermissionGranted { get; private set; }

        private MediaType _mediaType;
        private string _requesterName;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int BorderThickness = 3; // Border thickness
        private readonly Color BorderColor = Color.FromArgb(255, 128, 0); // Color.Orange

        public MediaPermissionRequestForm(MediaType mediaType, string requesterName, ClientRole requesterRole, bool sendTheirsMedia = false)
        {
            _mediaType = mediaType;
            _requesterName = requesterName;

            DoubleBuffered = true;

            InitializeComponent();
            SetupUI(sendTheirsMedia);
        }

        private void SetupUI(bool sendTheirsMedia = false)
        {
            string emoji = _mediaType == MediaType.Audio ? "🎤" : "📷";
            string mediaName = _mediaType == MediaType.Audio ? "صدا" : "وبکم و صدا";
            labelTitle.Text = $"{emoji} درخواست {mediaName}";

            labelRequester.Text = $"{emoji} {_requesterName}";

            this.DoubleBuffered = true;
            pictureBoxIcon.Image = GetWarningIcon();

            if (sendTheirsMedia)
                labelInfo.Text = $"⚠️ اگر اجازه دهید، شما می‌توانید:\n\n  • به {mediaName}  از این کاربر دسترسی پیدا کنید";
            else
                labelInfo.Text = $"⚠️ اگر اجازه دهید، این کاربر می‌تواند:\n\n  • به {mediaName} از شما دسترسی پیدا کند";

            buttonDeny.Click += (s, e) =>
            {
                PermissionGranted = false;
                this.DialogResult = DialogResult.No;
                this.Close();
            };

            buttonAllow.Click += (s, e) =>
            {
                PermissionGranted = true;
                this.DialogResult = DialogResult.Yes;
                this.Close();
            };

            this.AcceptButton = buttonAllow;
            this.CancelButton = buttonDeny;
        }

        /// <summary>
        /// Sets the message displayed in the dialog.
        /// </summary>
        public void SetMessage(string message)
        {
            if (labelMessage != null)
            {
                labelMessage.Text = message;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.None)
            {
                PermissionGranted = false;
            }
            base.OnFormClosing(e);
        }

        // Header Dragging
        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (Pen borderPen = new Pen(BorderColor, BorderThickness))
            {
                borderPen.Alignment = PenAlignment.Inset;

                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                e.Graphics.DrawRectangle(borderPen, rect);
            }
        }

        private void buttonDeny_MouseLeave(object sender, EventArgs e)
        {
            buttonDeny.BackColor = Color.FromArgb(231, 76, 60);
        }

        private void buttonDeny_MouseEnter(object sender, EventArgs e)
        {
            buttonDeny.BackColor = Color.FromArgb(192, 57, 43);
        }

        private void buttonDeny_Click(object sender, EventArgs e)
        {
            PermissionGranted = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void buttonAllow_MouseLeave(object sender, EventArgs e)
        {
            buttonAllow.BackColor = Color.FromArgb(46, 204, 113);
        }

        private void buttonAllow_MouseEnter(object sender, EventArgs e)
        {
            buttonAllow.BackColor = Color.FromArgb(39, 174, 96);
        }

        private void buttonAllow_Click(object sender, EventArgs e)
        {
            PermissionGranted = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            PermissionGranted = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnResize(EventArgs e)
        {
            return;

            base.OnResize(e);

            if (buttonClose != null)
            {
                buttonClose.Location = new Point(this.Width - 50, 0);
                buttonAllow.Location = new Point(this.Width - 250, this.Height - 70);
                buttonDeny.Location = new Point(this.Width - 130, this.Height - 70);
            }

            this.Invalidate();
        }

        private Image GetWarningIcon()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 128, 0)), 0, 0, 64, 64);
                g.DrawString("!", new Font("Segoe UI", 36, FontStyle.Bold), Brushes.White, new Point(18, 8));
            }
            return bitmap;
        }
    }
}