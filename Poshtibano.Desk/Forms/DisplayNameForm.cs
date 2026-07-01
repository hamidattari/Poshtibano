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

    public partial class DisplayNameForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int BorderThickness = 3; // Border thickness
        private readonly Color BorderColor = Color.FromArgb(255, 128, 0); // Color.Orange

        public string DisplayName { get; set; }

        public DisplayNameForm(string name = null, string title = "👨 انتخاب اسم")
        {
            InitializeComponent();
            ApplyModernStyles();

            ResizeRedraw = true;
            Padding = new Padding(BorderThickness);

            textBoxDisplayName.Text = name ?? string.Empty;
            labelTitle.Text = title;
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
            DisplayName = textBoxDisplayName.Text;
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
        }
    }
}