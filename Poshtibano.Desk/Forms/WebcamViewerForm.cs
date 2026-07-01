using AForge.Video.DirectShow;
using Poshtibano.Desk.Services.Media;
using System;
using System.Drawing;
using System.Drawing.Drawing2D; 
using System.IO;
using System.Windows.Forms;

namespace Poshtibano.Desk.Forms
{
    public partial class WebcamViewerForm : Form
    {
        private const int WM_NCHITTEST = 0x84;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2;

        private const int BorderThickness = 3; // Border thickness
        private readonly Color BorderColor = Color.FromArgb(255, 128, 0); // Color.Orange

        private Bitmap _currentFrame;
        private readonly object _frameLock = new object();
        private volatile bool _isClosing = false;
        private string _senderName;
        private int _frameCount = 0;

        public event Action OnViewerClosed;
        public event Action<VideoCapabilities> VideoCapabilitieUpdated;

        public WebcamViewerForm(string senderName = "Remote User")
        {
            _senderName = senderName;
            InitializeComponent();
            labelTitle.Text = $"📷 Webcam - {_senderName}";

            this.ResizeRedraw = true;

            // Add a 3‑pixel margin around the form
            this.Padding = new Padding(BorderThickness);

            // We’ll set the form’s background color to orange so that any empty space shows up as orange.
            this.BackColor = BorderColor;

            ApplyModernStyles();
        }

        private void ApplyModernStyles()
        {
            this.DoubleBuffered = true;
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

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                Point pos = new Point(m.LParam.ToInt32());
                pos = this.PointToClient(pos);

                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;
                int tolerance = BorderThickness + 2;

                if (pos.X <= tolerance && pos.Y <= tolerance) { m.Result = (IntPtr)HTTOPLEFT; return; }
                if (pos.X >= w - tolerance && pos.Y <= tolerance) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                if (pos.X <= tolerance && pos.Y >= h - tolerance) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                if (pos.X >= w - tolerance && pos.Y >= h - tolerance) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }

                if (pos.X <= tolerance) { m.Result = (IntPtr)HTLEFT; return; }
                if (pos.X >= w - tolerance) { m.Result = (IntPtr)HTRIGHT; return; }
                if (pos.Y <= tolerance) { m.Result = (IntPtr)HTTOP; return; }
                if (pos.Y >= h - tolerance) { m.Result = (IntPtr)HTBOTTOM; return; }

                if (pos.Y < 50 && pos.Y > tolerance)
                {
                    if (pos.X < w - 50)
                    {
                        m.Result = (IntPtr)HTCAPTION;
                        return;
                    }
                }
            }
            base.WndProc(ref m);
        }

        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void UpdateFrame(byte[] frameData, int width, int height)
        {
            if (_isClosing || frameData == null || frameData.Length == 0) return;
            _frameCount++;
            try
            {
                Bitmap newFrame;
                using (var ms = new MemoryStream(frameData))
                {
                    newFrame = (Bitmap)Image.FromStream(ms);
                }

                Bitmap oldFrame;
                lock (_frameLock)
                {
                    if (_isClosing) { newFrame?.Dispose(); return; }
                    oldFrame = _currentFrame;
                    _currentFrame = newFrame;
                }
                oldFrame?.Dispose();

                if (!_isClosing && !this.IsDisposed && this.IsHandleCreated)
                {
                    BeginInvoke(new Action(DoUpdatePictureBox));
                }
            }
            catch { }
        }

        private void DoUpdatePictureBox()
        {
            if (_isClosing || pictureBox == null || pictureBox.IsDisposed) return;
            try
            {
                Bitmap frameToShow = null;
                lock (_frameLock)
                {
                    if (_currentFrame != null && !_isClosing)
                        frameToShow = new Bitmap(_currentFrame);
                }
                if (frameToShow != null && !_isClosing)
                {
                    var oldImage = pictureBox.Image;
                    pictureBox.Image = frameToShow;
                    oldImage?.Dispose();
                }
                else frameToShow?.Dispose();
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isClosing = true;
            System.Threading.Thread.Sleep(50);
            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = null;
            }
            if (pictureBox != null && !pictureBox.IsDisposed)
            {
                var img = pictureBox.Image;
                pictureBox.Image = null;
                img?.Dispose();
            }
            OnViewerClosed?.Invoke();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isClosing = true;
                lock (_frameLock)
                {
                    _currentFrame?.Dispose();
                    _currentFrame = null;
                }
                OnViewerClosed = null;
            }
            base.Dispose(disposing);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}