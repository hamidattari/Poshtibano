using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Poshtibano.Desk.Forms
{

    public partial class EditMessageForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int BorderThickness = 3; // Border thickness
        private readonly Color BorderColor = Color.FromArgb(255, 128, 0); // Color.Orange

        public string EditedText { get; set; }

        public EditMessageForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        public EditMessageForm(string originalMessage)
        {
            InitializeComponent();
            textBoxMessage.Text = originalMessage;
            EditedText = originalMessage;
            ApplyModernStyles();
        }

        private void ButtonCancel_MouseLeave(object sender, EventArgs e)
        {
            buttonCancel.BackColor = Color.FromArgb(200, 200, 200);
        }

        private void ButtonCancel_MouseEnter(object sender, EventArgs e)
        {
            buttonCancel.BackColor = Color.FromArgb(180, 180, 180);
        }
        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void ButtonMinimize_Click(object? sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void ButtonOK_MouseLeave(object? sender, EventArgs e)
        {
            buttonOK.BackColor = Color.FromArgb(255, 128, 0);
        }

        private void ButtonOK_MouseEnter(object? sender, EventArgs e)
        {
            buttonOK.BackColor = Color.FromArgb(230, 110, 0);
        }

        private void ButtonOK_Click(object? sender, EventArgs e)
        {
            EditedText = textBoxMessage.Text;
        }

        private void ApplyModernStyles()
        {
            this.DoubleBuffered = true;
        }

        // Header Dragging
        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // Custom Border Drawing
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

        // Minimize Support
        protected override void OnResize(EventArgs e)
        {
            return;

            base.OnResize(e);

            if (buttonClose != null)
            {
                buttonClose.Location = new Point(this.Width - 50, 0);
                buttonMinimize.Location = new Point(this.Width - 100, 0);

                textBoxMessage.Width = this.Width - 40;
                buttonOK.Location = new Point(this.Width - 280, this.Height - 70);
                buttonCancel.Location = new Point(this.Width - 150, this.Height - 70);
            }

            this.Invalidate();
        }
    }
}
