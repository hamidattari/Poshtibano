using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Services;
using Poshtibano.Desk.Shared;
using Poshtibano.Desk.Shared.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Poshtibano.Desk.Controls
{
    public partial class ChatBubbleControl : UserControl
    {
        public string Message { get; set; } = "";
        public string Time { get; set; } = "";
        public ChatMessageMode Mode { get; set; }
        public int MaxBubbleWidth { get; set; } = 200;
        public ChatMessage ChatMessage { get; internal set; }

        public bool IsLiked
        {
            get => _isLiked;
            set
            {
                _isLiked = value;
                Invalidate();
            }
        }
        private bool _isLiked = false;

        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                _isEdited = value;
                Invalidate();
            }
        }
        private bool _isEdited = false;

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                Message = "پیام پاک شده است";
                _isDeleted = value;
                Invalidate();
            }
        }
        private bool _isDeleted = false;

        public event Action<Guid, ChatEventType, object> OnChatEventRequested;

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem likeMenuItem;
        private ToolStripMenuItem copyMenuItem;
        private ToolStripMenuItem editMenuItem;
        private ToolStripMenuItem deleteMenuItem;

        private Brush messageBrush;
        private Brush timeBrush;
        private Color bubbleColor;

        public ChatBubbleControl()
        {
            DoubleBuffered = true;
            Margin = new Padding(5, 4, 5, 4);
            Padding = new Padding(10);
            BackColor = Color.White;

            InitializeContextMenu();
            MouseUp += ChatBubbleControl_MouseUp;
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            likeMenuItem = new ToolStripMenuItem("❤️ لایک کردن");
            likeMenuItem.Click += LikeMenuItem_Click;

            copyMenuItem = new ToolStripMenuItem("📋 کپی متن");
            copyMenuItem.Click += CopyMenuItem_Click;

            editMenuItem = new ToolStripMenuItem("✏️ ویرایش پیام");
            editMenuItem.Click += EditMenuItem_Click;

            deleteMenuItem = new ToolStripMenuItem("🗑 حذف پیام");
            deleteMenuItem.Click += DeleteMenuItem_Click; ;

            contextMenu.Items.Add(likeMenuItem);
            contextMenu.Items.Add(copyMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(deleteMenuItem);
            contextMenu.Items.Add(editMenuItem);
        }

        private void ChatBubbleControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (Mode == ChatMessageMode.Warning || IsDeleted) return;

            if (e.Button == MouseButtons.Right)
            {
                editMenuItem.Visible = Mode == ChatMessageMode.Local;
                editMenuItem.Enabled = Mode == ChatMessageMode.Local;

                deleteMenuItem.Visible = Mode == ChatMessageMode.Local;
                deleteMenuItem.Enabled = Mode == ChatMessageMode.Local;

                likeMenuItem.Text = IsLiked ? "❤️ حذف لایک" : "❤️ لایک کردن";

                contextMenu.Show(this, e.Location);
            }
        }
        private void DeleteMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show("پیام پاک بشود ❌", "پاک کردن", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                Message = "پیام پاک شده است";
                IsDeleted = true;

                if (result == DialogResult.No) return;

                OnChatEventRequested?.Invoke(
                    ChatMessage.Id,
                    ChatEventType.MessageDeleted,
                    Message
            );

                Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در پاک کردن: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LikeMenuItem_Click(object sender, EventArgs e)
        {
            IsLiked = !IsLiked;

            if (ChatMessage != null)
            {
                OnChatEventRequested?.Invoke(
                    ChatMessage.Id,
                    ChatEventType.MessageLiked,
                    IsLiked
                );
            }
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(Message);
                MessageBox.Show("متن کپی شد ✓", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در کپی: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditMenuItem_Click(object sender, EventArgs e)
        {
            if (Mode != ChatMessageMode.Local) return;

            var editForm = new EditMessageForm(Message);
            if (editForm.ShowDialog() == DialogResult.OK && editForm.EditedText != Message)
            {
                Message = editForm.EditedText;
                IsEdited = true;

                if (ChatMessage != null)
                {
                    OnChatEventRequested?.Invoke(
                        ChatMessage.Id,
                        ChatEventType.MessageEdited,
                        editForm.EditedText
                    );
                }

                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (string.IsNullOrEmpty(Message))
                return;

            e.Graphics.Clear(Color.White);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            bubbleColor = 
                Mode == ChatMessageMode.Local ? Color.CornflowerBlue : 
                Mode == ChatMessageMode.Remote ? Color.GreenYellow : 
                Mode == ChatMessageMode.Warning ? Color.Orange : Color.Black;
            messageBrush = new SolidBrush(
                Mode == ChatMessageMode.Local ? Color.Black : 
                Mode == ChatMessageMode.Remote ? Color.Black : 
                Mode == ChatMessageMode.Warning ? Color.Black : Color.White);
            timeBrush = new SolidBrush(
                Mode == ChatMessageMode.Local ? Color.White : 
                Mode == ChatMessageMode.Remote ? Color.White : 
                Mode == ChatMessageMode.Warning ? Color.White : Color.LightSteelBlue);

            Font messageFont = new Font("Segoe UI", 9.5F, IsDeleted ? FontStyle.Italic : FontStyle.Regular);
            Font timeFont = new Font("Segoe UI", 7.5F, FontStyle.Regular);

            const int horizontalPadding = 12;
            const int verticalPadding = 10;
            const int radiusSize = 15;
            const int spacing = 4;

            try
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                };

                int textMaxWidth = Math.Max(150, MaxBubbleWidth - horizontalPadding * 2);

                SizeF textSize = e.Graphics.MeasureString(
                    Message + "___" + Environment.NewLine,
                    messageFont,
                    textMaxWidth,
                    sf
                );

                SizeF timeSize = e.Graphics.MeasureString(Time, timeFont);
                SizeF editedSize = IsEdited
                    ? e.Graphics.MeasureString("(ویرایش شده)", new Font("Segoe UI", 7F, FontStyle.Italic))
                    : SizeF.Empty;

                int minWidthForTime = (int)timeSize.Width + horizontalPadding * 2 + 5;

                int bubbleWidth = 0;
                if (Mode == ChatMessageMode.Local || Mode == ChatMessageMode.Remote)
                {
                    bubbleWidth = Math.Max(
                        minWidthForTime,
                        Math.Min(MaxBubbleWidth, (int)textSize.Width + horizontalPadding * 2)
                    );
                }
                else
                { 
                    bubbleWidth = MaxBubbleWidth - horizontalPadding * 2;
                }

                int extraHeight = IsEdited ? (int)editedSize.Height + 2 : 0;
                int bubbleHeight = (int)(textSize.Height + timeSize.Height + verticalPadding * 2 + spacing + 2 + extraHeight);

                Height = bubbleHeight + 10;

                int bubbleX = Mode == ChatMessageMode.Local ? Width - bubbleWidth - 10 : 10;
                int bubbleY = 5;

                using (var path = RoundedRectangle(bubbleX, bubbleY, bubbleWidth, bubbleHeight, radiusSize))
                {
                    e.Graphics.FillPath(new SolidBrush(bubbleColor), path);

                    if (IsLiked)
                    {
                        Font heartFont = new Font("Segoe UI Emoji", 14);
                        e.Graphics.DrawString("❤︎", heartFont, Brushes.Red,
                            new PointF(bubbleX + bubbleWidth - 25, bubbleY + 5));
                    }
                }

                var textRect = new RectangleF(
                    bubbleX + horizontalPadding,
                    bubbleY + verticalPadding,
                    bubbleWidth - horizontalPadding * 2,
                    textSize.Height
                );

                e.Graphics.DrawString(
                    Message,
                    messageFont,
                    messageBrush,
                    textRect,
                    sf
                );

                float timeY = bubbleY + verticalPadding + textSize.Height + spacing;
                var timeRect = new RectangleF(
                    bubbleX + horizontalPadding,
                    timeY,
                    bubbleWidth - horizontalPadding * 2,
                    timeSize.Height
                );

                e.Graphics.DrawString(
                    Time,
                    timeFont,
                    timeBrush,
                    timeRect,
                    sf
                );

                if (IsEdited)
                {
                    Font editedFont = new Font("Segoe UI", 7F, FontStyle.Italic);
                    float editedY = timeY + timeSize.Height + 2;
                    var editedRect = new RectangleF(
                        bubbleX + horizontalPadding,
                        editedY,
                        bubbleWidth - horizontalPadding * 2,
                        editedSize.Height
                    );
                    e.Graphics.DrawString("(ویرایش شده)", editedFont,
                        new SolidBrush(Color.FromArgb(100, 100, 100)), editedRect, sf);
                }

                sf.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ChatBubbleControl. OnPaint: {ex.Message}");
            }
            finally
            {
                messageBrush?.Dispose();
                timeBrush?.Dispose();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        private System.Drawing.Drawing2D.GraphicsPath RoundedRectangle(int x, int y, int width, int height, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int adjustedRadius = Math.Min(radius, Math.Min(width, height) / 2);

            path.AddArc(x, y, adjustedRadius * 2, adjustedRadius * 2, 180, 90);
            path.AddArc(x + width - adjustedRadius * 2, y, adjustedRadius * 2, adjustedRadius * 2, 270, 90);
            path.AddArc(x + width - adjustedRadius * 2, y + height - adjustedRadius * 2, adjustedRadius * 2, adjustedRadius * 2, 0, 90);
            path.AddArc(x, y + height - adjustedRadius * 2, adjustedRadius * 2, adjustedRadius * 2, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}