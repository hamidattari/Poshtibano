// File: MainForm.FileTransfer.cs
using Poshtibano.Common;
using Poshtibano.Desk.Shared.Services;
using System.Text;

namespace Poshtibano.Desk
{
    partial class MainForm
    {
        private ToolTip toolTipFile = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 500,
            ReshowDelay = 200,
            ShowAlways = true
        };

        private bool _inCancelingFileTransfer = false;

        private async void buttonUploadFile_Click(object sender, EventArgs e)
        {
            if (_session == null || !_session.IsConnected)
            {
                MessageBox.Show("لطفاً ابتدا به دستگاه متصل شوید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_fileTrnsferInProgress)
            {
                MessageBox.Show("در حال حاضر یک انتقال فایل در جریان است. لطفاً صبر کنید.", "اطلاع", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "انتخاب فایل برای ارسال",
                Filter = "همه فایل‌ها|*.*",
                Multiselect = false
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _ = Task.Run(async () =>
                    {
                        if (_fileTrnsferInProgress) return;
                        _fileTrnsferInProgress = true;
                        await _session.SendFilesAsync(new[] { ofd.FileName });
                        _fileTrnsferInProgress = false;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ارسال فایل ناموفق: {ex.Message}", "خطای انتقال", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Session_OnFileProgress(FileTransferProgress progress)
        {
            Invoke(() =>
            {
                if (progress.Percent < 100 && progress.State != FileTransferProgressState.Complete)
                {
                    _fileTrnsferInProgress = true;
                }

                if (progress.State == FileTransferProgressState.Complete) return;

                _activeTransferId = progress.TransferId;

                labelTransferStatus.Visible = true;
                progressBarTransfer.Visible = true;
                buttonCancelTransfer.Visible = true;

                buttonUploadFile.Visible = false;

                var cancelButton = this.Controls["buttonCancelTransfer"] as Button;
                if (cancelButton != null)
                    cancelButton.Visible = true;

                if (progress.IsReceiving)
                    labelTransferStatus.Text = $"در حال دریافت {progress.FileName}: {progress.Percent:F1}%";
                else
                    labelTransferStatus.Text = $"در حال انتقال {progress.FileName}: {progress.Percent:F1}%";

                try
                {
                    progressBarTransfer.Value = (int)progress.Percent;
                }
                catch
                {
                    progressBarTransfer.Value = 0;
                }

                if (progress.Percent == 100 && _fileTrnsferInProgress)
                {
                    //_ = Task.Delay(2500).ContinueWith(_ =>
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            Invoke(() =>
                            {
                                _fileTrnsferInProgress = false;

                                if (!progress.IsReceiving)
                                {
                                    var panel = new Panel
                                    {
                                        Width = flowlayoutpanelChatHistory.ClientSize.Width - 20,
                                        Height = 60,
                                        BackColor = Color.WhiteSmoke
                                    };

                                    var fileName = System.IO.Path.GetFileName(progress.FileName);
                                    var label = new Label
                                    {
                                        RightToLeft = RightToLeft.Yes,
                                        Text = $"فایل {fileName} با موفقیت ارسال شد",
                                        AutoEllipsis = true,
                                        AutoSize = false,
                                        Width = panel.Width - 10,
                                        Height = 30,
                                        Location = new Point(10, 10)
                                    };

                                    panel.Controls.Add(label);
                                    toolTipFile.SetToolTip(panel, $"فایل {fileName} با موفقیت ارسال شد");

                                    flowlayoutpanelChatHistory.AllowDrop = true;
                                    flowlayoutpanelChatHistory.Controls.Add(panel);
                                    flowlayoutpanelChatHistory.ScrollControlIntoView(panel);
                                }

                                buttonCancelTransfer.Visible = false;
                                progressBarTransfer.Visible = false;

                                progressBarTransfer.Value = 0;

                                labelTransferStatus.Visible = false;
                                labelTransferStatus.ForeColor = Color.Gray;

                                buttonUploadFile.Visible = true;

                            });
                        }
                        catch { }
                    });
                }
            });
        }

        private void Session_OnFileReceived(string transferId, string savedPath)
        {
            Invoke(() =>
            {
                if (!panelChat.Visible) buttonChat_Click(null, null);

                _fileTrnsferInProgress = false;

                progressBarTransfer.Visible = false;
                labelTransferStatus.Visible = false;
                buttonCancelTransfer.Visible = false;

                var panel = new Panel
                {
                    Width = flowlayoutpanelChatHistory.ClientSize.Width - 20,
                    Height = 60,
                    BackColor = Color.WhiteSmoke
                };

                var label = new Label
                {
                    Text = System.IO.Path.GetFileName(savedPath),
                    AutoEllipsis = true,
                    AutoSize = false,
                    Width = panel.Width - 110,
                    Height = 30,

                    Location = new Point(10, 10)
                };

                var buttonSave = new Button
                {
                    Text = "ذخیره",
                    Location = new Point(panel.Width - 90, 10),
                    Size = new Size(80, 30)
                };

                buttonSave.Click += (s, e) =>
                {
                    using var sfd = new SaveFileDialog
                    {
                        FileName = System.IO.Path.GetFileName(savedPath),
                        Filter = "همه فایل‌ها|*.*"
                    };

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            System.IO.File.Copy(savedPath, sfd.FileName, true);
                            MessageBox.Show("فایل با موفقیت ذخیره شد.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"ذخیره ناموفق: {ex.Message}");
                        }
                    }
                };

                buttonUploadFile.Visible = true;

                panel.Controls.Add(label);
                panel.Controls.Add(buttonSave);

                toolTipFile.SetToolTip(panel, $"فایل {System.IO.Path.GetFileName(savedPath)} با موفقیت دریافت شد");

                flowlayoutpanelChatHistory.Controls.Add(panel);
                flowlayoutpanelChatHistory.ScrollControlIntoView(panel);
            });
        }

        private void Session_OnTransferCancelled(string transferId, string fileName)
        {
            Console.WriteLine($"[{DateTime.Now}] 🛑 Session_OnTransferCancelled CALLED:  {transferId}");

            if (InvokeRequired)
            {
                Invoke(() => Session_OnTransferCancelled(transferId, fileName));
                return;
            }

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🛑 Updating UI for cancel");

                buttonCancelTransfer.Visible = false;
                progressBarTransfer.Visible = false;

                progressBarTransfer.Value = 0;

                labelTransferStatus.Visible = true;
                labelTransferStatus.Text = $"❌ انتقال فایل \"{fileName}\" لغو شد";
                labelTransferStatus.ForeColor = Color.Red;

                buttonUploadFile.Visible = true;

                Console.WriteLine($"[{DateTime.Now}] ✅ UI updated");

                _ = Task.Delay(2500).ContinueWith(_ =>
                {
                    if (_inCancelingFileTransfer) return;
                    _inCancelingFileTransfer = true;

                    if (!this.IsDisposed)
                    {
                        try
                        {
                            Invoke(() =>
                            {
                                labelTransferStatus.Visible = false;
                                labelTransferStatus.ForeColor = Color.Gray;
                                _activeTransferId = null;

                                Console.WriteLine($"[{DateTime.Now}] ✅ Cancel message hidden");
                            });
                        }
                        catch { }

                        var msg = new ChatMessage
                        {
                            Id = Guid.NewGuid(),
                            Mode = ChatMessageMode.Warning,
                            Timestamp = DateTimeOffset.UtcNow,
                            Text = $"❌ انتقال فایل \"{fileName}\" لغو شد",
                            SenderRole = Common.ClientRole.None
                        };

                        buttonUploadFile.Visible = true;
                        AddChatBubble(msg);

                        _fileTrnsferInProgress = false;
                        _inCancelingFileTransfer = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error in Session_OnTransferCancelled:  {ex.Message}");
            }
        }

        private void ButtonCancelTransfer_Click(object sender, EventArgs e)
        {
            if (_session != null && !string.IsNullOrEmpty(_activeTransferId))
            {
                Task.Run(() =>
                {
                    var fileName = _session.CancelFileTransfer(_activeTransferId);

                    Invoke(() =>
                    {
                        _fileTrnsferInProgress = false;
                        buttonUploadFile.Visible = true;
                        labelTransferStatus.Text = $"❌ انتقال فایل \"{fileName}\" لغو شد";
                        labelTransferStatus.ForeColor = Color.Red;

                        progressBarTransfer.Visible = false;
                        labelTransferStatus.Visible = false;
                        buttonCancelTransfer.Visible = false;
                        labelTransferStatus.ForeColor = Color.Gray;

                        _activeTransferId = null;

                        var msg = new ChatMessage
                        {
                            Id = Guid.NewGuid(),
                            Mode = ChatMessageMode.Warning,
                            Timestamp = DateTimeOffset.UtcNow,
                            Text = $"❌ انتقال فایل \"{Path.GetFileName(fileName)}\" لغو شد",
                            SenderRole = Common.ClientRole.None
                        };

                        AddChatBubble(msg);
                    });
                });
            }
        }

        private void HandleFileDrop(IntPtr hDrop)
        {
            uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
            string[] filePaths = new string[fileCount];

            for (uint i = 0; i < fileCount; i++)
            {
                StringBuilder sb = new StringBuilder(1024);
                DragQueryFile(hDrop, i, sb, (uint)sb.Capacity);
                filePaths[i] = sb.ToString();
            }

            DragFinish(hDrop);

            if (filePaths.Length > 0)
            {
                ProcessDroppedFiles(filePaths);
            }
        }

        private void ProcessDroppedFiles(string[] files)
        {
            if (_session != null && !_fileTrnsferInProgress)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _fileTrnsferInProgress = true;
                        await _session.SendFilesAsync(files);
                        if (!panelChat.Visible) buttonChat_Click(null, null);
                        _fileTrnsferInProgress = false;
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => MessageBox.Show($"خطا در ارسال: {ex.Message}"));
                    }
                });
            }
        }

        private void FlowlayoutpanelChatHistory_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;

                if (_fileTrnsferInProgress) e.Effect = DragDropEffects.None;
            }
        }

        private void FlowlayoutpanelChatHistory_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_fileTrnsferInProgress) return;

                        _fileTrnsferInProgress = true;
                        await _session.SendFilesAsync(files);
                        _fileTrnsferInProgress = false;
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => MessageBox.Show($"ارسال فایل‌ها ناموفق: {ex.Message}", "خطای انتقال", MessageBoxButtons.OK, MessageBoxIcon.Error));
                    }
                });
            }
        }
    }
}