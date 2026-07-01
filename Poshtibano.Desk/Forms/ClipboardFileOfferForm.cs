using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Poshtibano.Desk.Forms
{
    public partial class ClipboardFileOfferForm : Form
    {
        public event Action<string> OnAccepted;
        public event Action<string> OnRejected;

        private readonly string _transferId;
        private readonly List<string> _fileNames;
        private readonly long _totalSize;

        protected override bool ShowWithoutActivation => true;

        public ClipboardFileOfferForm(string transferId, List<string> fileNames, long totalSize)
        {
            InitializeComponent();
            _transferId = transferId;
            _fileNames = fileNames;
            _totalSize = totalSize;
        }

        private void ClipboardNotificationForm_Load(object sender, EventArgs e)
        {
            var sizeStr = FormatSizeUI(_totalSize);
            var fileListText = _fileNames.Count <= 3
                ? string.Join("، ", _fileNames)
                : $"{string.Join("، ", _fileNames.Take(3))} و {_fileNames.Count - 3} فایل/پوشه دیگر";

            labelMessage.Text = $"📋 {_fileNames.Count} فایل/پوشه ({sizeStr})\n{fileListText}";

            timerAutoClose.Start();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            timerAutoClose.Stop();
            OnAccepted?.Invoke(_transferId);
            this.Close();
        }

        private void btnReject_Click(object sender, EventArgs e)
        {
            HandleReject();
        }

        private void timerAutoClose_Tick(object sender, EventArgs e)
        {
            HandleReject();
        }

        private void HandleReject()
        {
            timerAutoClose.Stop();
            OnRejected?.Invoke(_transferId);
            this.Close();
        }

        private void ClipboardNotificationForm_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.Black, ButtonBorderStyle.Solid);
        }

        private string FormatSizeUI(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
}