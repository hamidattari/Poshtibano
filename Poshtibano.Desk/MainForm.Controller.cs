namespace Poshtibano.Desk
{
    partial class MainForm
    {
        #region Controller-Specific Setup

        private void SetupControllerUI()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Invoke(async () =>
                    {
                        pictureBox.MouseMove += PictureBox_MouseMove;
                        pictureBox.MouseDown += PictureBox_MouseDown;
                        pictureBox.MouseUp += PictureBox_MouseUp;
                        pictureBox.MouseWheel += PictureBox_MouseWheel;

                        textBoxChatInput.GotFocus += TextBoxChatInput_GotFocus;
                        textBoxChatInput.LostFocus += TextBoxChatInput_LostFocus;

                        flowlayoutpanelChatHistory.AllowDrop = true;
                        flowlayoutpanelChatHistory.DragEnter += FlowlayoutpanelChatHistory_DragEnter;
                        flowlayoutpanelChatHistory.DragDrop += FlowlayoutpanelChatHistory_DragDrop;

                        buttonUploadFile.Visible = !_fileTrnsferInProgress;

                        Console.WriteLine($"[{DateTime.Now}] ✅ Controller UI setup complete");
                    });

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error in session ready: {ex.Message}");
                }
            });

            Console.WriteLine($"[{DateTime.Now}] ⚙️ Setting up Controller UI");
        }

        private void DetachControllerUIHandlers()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🧹 Detaching Controller UI handlers");

                pictureBox.MouseMove -= PictureBox_MouseMove;
                pictureBox.MouseDown -= PictureBox_MouseDown;
                pictureBox.MouseUp -= PictureBox_MouseUp;
                pictureBox.MouseWheel -= PictureBox_MouseWheel;

                textBoxChatInput.GotFocus -= TextBoxChatInput_GotFocus;
                textBoxChatInput.LostFocus -= TextBoxChatInput_LostFocus;

                flowlayoutpanelChatHistory.DragEnter -= FlowlayoutpanelChatHistory_DragEnter;
                flowlayoutpanelChatHistory.DragDrop -= FlowlayoutpanelChatHistory_DragDrop;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error detaching UI handlers: {ex.Message}");
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            _session?.HandleMouseMove(e);
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            this.ActiveControl = pictureBox;
            _session?.HandleMouseDown(e);
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            _session?.HandleMouseUp(e);
        }

        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            var mousePos = pictureBox.PointToClient(MousePosition);
            _session?.HandleMouseWheel(e, mousePos);
        }

        private void TextBoxChatInput_GotFocus(object sender, EventArgs e)
        {
            try
            {
                if (_session != null && _session.IsConnected)
                {
                    _session.SetKeyboardSuppression(false);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session disposed in GotFocus: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error in GotFocus: {ex.Message}");
            }
        }

        private void TextBoxChatInput_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (_session != null && _session.IsConnected)
                {
                    _session.SetKeyboardSuppression(true);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session disposed in LostFocus: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error in LostFocus: {ex.Message}");
            }
        }

   

        #endregion
    }
}