using Poshtibano.Desk.Controls;
using Poshtibano.Desk.Shared;
using Poshtibano.Desk.Shared.Services;

namespace Poshtibano.Desk
{
    partial class MainForm
    {
        private void buttonChatSend_Click(object sender, EventArgs e)
        {
            var text = textBoxChatInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (_session != null && _session.IsConnected)
            {
                _ = _session.SendChatMessageAsync(text);
                textBoxChatInput.Clear();
                textBoxChatInput.Focus();
            }
        }

        private void textBoxChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                buttonChatSend_Click(sender, e);
            }
        }

        private void Session_OnChatMessage(ChatMessage msg)
        {
            msg.Mode = _currentRole == msg.SenderRole ? ChatMessageMode.Local : ChatMessageMode.Remote;
            Invoke(() => 
            {
                if (!panelChat.Visible) buttonChat_Click(null, null);
                AddChatBubble(msg);
            });
        }

        private void Session_OnChatEvent(ChatEvent evt)
        {
            Invoke(() =>
            {
                foreach (Control control in flowlayoutpanelChatHistory.Controls)
                {
                    if (control is ChatBubbleControl bubble && bubble.ChatMessage?.Id == evt.MessageId)
                    {
                        if (evt.EventType == ChatEventType.MessageLiked)
                        {
                            bubble.IsLiked = evt.IsLiked;
                        }
                        else if (evt.EventType == ChatEventType.MessageEdited)
                        {
                            bubble.Message = evt.NewText;
                            bubble.IsEdited = true;
                        }
                        else if (evt.EventType == ChatEventType.MessageDeleted)
                        {
                            bubble.IsDeleted = true;
                        }
                        break;
                    }
                }
            });
        }

        #region Chat UI

        private void AddChatBubble(ChatMessage msg)
        {
            Console.WriteLine("Message added to chat history: " + msg.Text);

            if (InvokeRequired)
            {
                Invoke(() => AddChatBubble(msg));
                return;
            }

            if (this.IsDisposed || this.Disposing)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Form disposed, ignoring chat message");
                return;
            }

            try
            {
                bool textBoxHadFocus = textBoxChatInput.Focused;

                if (flowlayoutpanelChatHistory.IsDisposed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Chat panel disposed, ignoring message");
                    return;
                }

                int availableWidth = flowlayoutpanelChatHistory.ClientSize.Width;
                if (availableWidth <= 0)
                    availableWidth = 280;

                var bubble = new ChatBubbleControl
                {
                    Width = availableWidth - 10,
                    Message = msg.Text,
                    Time = msg.Timestamp.ToLocalTime().ToString("HH:mm"),
                    Mode = msg.Mode,
                    ChatMessage = msg,
                    IsLiked = msg.IsLiked,
                    IsEdited = msg.IsEdited,
                    MaxBubbleWidth = availableWidth - 40,
                    AutoSize = false
                };

                bubble.OnChatEventRequested += (messageId, eventType, data) =>
                {
                    _session?.SendChatEvent(messageId, eventType, data);
                };

                flowlayoutpanelChatHistory.Controls.Add(bubble);
                flowlayoutpanelChatHistory.ScrollControlIntoView(bubble);

                if (textBoxHadFocus)
                {
                    textBoxChatInput.Focus();
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ ObjectDisposedException in AddChatBubble: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error in AddChatBubble: {ex.Message}");
            }
        }

        #endregion
    }
}