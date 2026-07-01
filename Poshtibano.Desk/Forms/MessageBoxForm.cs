using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Poshtibano.Desk.Forms
{
    public partial class MessageBoxForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int BorderThickness = 3; // Border thickness
        private readonly Color BorderColor = Color.FromArgb(255, 128, 0); // Color.Orange

        public bool AllowAccess { get; private set; } = false;

        public MessageBoxForm(string messsage)
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            labelMessage.Text = messsage;
            pictureBoxIcon.Image = GetWarningIcon();
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
            btnDeny.BackColor = Color.FromArgb(231, 76, 60);
        }

        private void buttonDeny_MouseEnter(object sender, EventArgs e)
        {
            btnDeny.BackColor = Color.FromArgb(192, 57, 43);
        }

        private void buttonDeny_Click(object sender, EventArgs e)
        {
            AllowAccess = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void buttonAllow_Click(object sender, EventArgs e)
        {
            AllowAccess = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            AllowAccess = false;
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
                btnDeny.Location = new Point(this.Width - 130, this.Height - 70);
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
