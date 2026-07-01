using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Poshtibano.Desk
{
    /// <summary>
    /// MainForm clipboard sharing event handlers.
    /// Follows architecture: MainForm ↔ SessionCoordinator
    /// </summary>
    partial class MainForm
    {
        private void checkBoxClipboardSharing_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = checkBoxClipboardSharing.Checked;
            ToggleClipboardSharing(enabled);

            // Save preference
            SettingsManager.Instance.SetClipboardSharingEnabled(enabled);

            Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard sharing checkbox: {(enabled ? "ON ✅" : "OFF ❌")}");
        }

        /// <summary>
        /// Remote clipboard text received
        /// </summary>
        private void Session_OnClipboardRemoteTextReceived(string text)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnClipboardRemoteTextReceived(text));
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📋 Remote clipboard text received: {text.Substring(0, Math.Min(50, text.Length))}...");
            //ShowClipboardNotification($"📋 متن از کلیپ‌بورد ریموت دریافت شد ({text.Length} حرف)");
        }

        /// <summary>
        /// Remote clipboard files received (after transfer complete)
        /// </summary>
        private void Session_OnClipboardRemoteFilesReceived(List<string> files)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnClipboardRemoteFilesReceived(files));
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📋 Remote clipboard files received: {files.Count} file(s)");
            //ShowClipboardNotification($"📋 {files.Count} فایل دریافت شد — آماده Paste ✅");
        }

        /// <summary>
        /// ✅ NEW: Remote file offer received — show Accept/Reject notification
        /// </summary>
        private void Session_OnClipboardFileOfferReceived(string transferId, List<string> fileNames, long totalSize)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnClipboardFileOfferReceived(transferId, fileNames, totalSize));
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📋 File offer: {fileNames.Count} file(s), {FormatSizeUI(totalSize)}");

            ShowClipboardFileOfferNotification(transferId, fileNames, totalSize);
        }

        /// <summary>
        /// ✅ NEW: Show a notification bar for file offer with Accept/Reject buttons
        /// </summary>
        private void ShowClipboardFileOfferNotification(string transferId, List<string> fileNames, long totalSize)
        {
            var notifForm = new ClipboardFileOfferForm(transferId, fileNames, totalSize);
            notifForm.Location = new Point(this.Location.X + this.Width - 420, this.Location.Y + 75);

            notifForm.OnAccepted += (id) =>
            {
                _session?.AcceptClipboardFileOffer(id);
                //ShowClipboardNotification("📋 درخواست دریافت فایل ارسال شد...");
            };

            notifForm.OnRejected += (id) =>
            {
                _session?.RejectClipboardFileOffer(id);
            };

            notifForm.Show(this);
        }

        /// <summary>
        /// Clipboard status changed
        /// </summary>
        private void Session_OnClipboardStatusChanged(string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnClipboardStatusChanged(status));
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] {status}");
        }

        /// <summary>
        /// Clipboard error
        /// </summary>
        private void Session_OnClipboardError(string error)
        {
            if (InvokeRequired)
            {
                Invoke(() => Session_OnClipboardError(error));
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] ❌ Clipboard error: {error}");
        }

        /// <summary>
        /// Toggle clipboard sharing on/off
        /// </summary>
        public void ToggleClipboardSharing(bool enabled)
        {
            if (_session != null)
            {
                _session.IsClipboardSharingEnabled = enabled;
                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard sharing toggled: {enabled}");
            }
        }

        /// <summary>
        /// Shows a brief notification about clipboard activity
        /// </summary>
        private void ShowClipboardNotification(string message)
        {
            try
            {
                var notification = new ToolTip
                {
                    IsBalloon = true,
                    ToolTipIcon = ToolTipIcon.Info,
                    ToolTipTitle = "Clipboard Sharing"
                };

                notification.Show(message, this, Width / 2, 50, 3000);

                var timer = new System.Windows.Forms.Timer { Interval = 4000 };
                timer.Tick += (s, e) =>
                {
                    notification.Dispose();
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Notification error: {ex.Message}");
            }
        }

        private static string FormatSizeUI(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}