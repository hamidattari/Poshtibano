using Poshtibano.Desk.Shared.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Poshtibano.Desk.Controls
{
    public class GuidFormattedTextBox : TextBox
    {
        private ContextMenuStrip _contextMenu;

        private bool _isFormatting = false;
        private Guid _currentGuid = Guid.Empty;
        private bool _isReadOnly = false;

        public char Spacer { get; set; } = '-';

        public GuidFormattedTextBox()
        {
            // Event handlers
            this.TextChanged += GuidFormattedTextBox_TextChanged;
            this.KeyPress += GuidFormattedTextBox_KeyPress;
            this.MaxLength = 15; // 10 digits + 4 spaces + 1 extra for safety

            _contextMenu = new ContextMenuStrip();
            _contextMenu.RightToLeft = RightToLeft.Yes;

            var renameItem = new ToolStripMenuItem("کپی");
            renameItem.Click += (s, e) =>
            {
                SelectAll();
                HandleCopy();
            };

            var deleteItem = new ToolStripMenuItem(" چسباندن");
            deleteItem.Click += (s, e) =>
            {
                SelectAll();
                HandlePaste();
            };

            _contextMenu.Items.Add(renameItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(deleteItem);
            ContextMenuStrip = _contextMenu;

            //MouseUp += (sender, e) => 
            //{ 
            //    _contextMenu.Show(this, e.Location); 
            //};
        }

        /// <summary>
        /// Gets or sets the GUID value
        /// </summary>
        public Guid GuidValue
        {
            get
            {
                _currentGuid = NumericValue.ConvertNumberToGuid();
                return _currentGuid;
            }
            set
            {
                _currentGuid = value;
                UpdateTextFromGuid();
            }
        }

        /// <summary>
        /// Gets the 10-digit number as string (without formatting)
        /// </summary>
        public string UnformattedNumber
        {
            get => GuidExtention.GetDigitsOnly(this.Text);
        }

        /// <summary>
        /// Gets the 10-digit number as long
        /// </summary>
        public long NumericValue
        {
            get
            {
                string digits = UnformattedNumber;
                if (long.TryParse(digits, out long result))
                    return result;
                return 0;
            }
        }

        /// <summary>
        /// Update text display from GUID
        /// </summary>
        private void UpdateTextFromGuid()
        {
            if (_currentGuid == Guid.Empty)
            {
                this.Text = string.Empty;
                return;
            }

            long number = _currentGuid.ConvertGuidToNumber();
            string formattedText = GuidExtention.FormatNumber(number.ToString());

            _isFormatting = true;
            this.Text = formattedText;
            this.SelectionStart = this.Text.Length;
            _isFormatting = false;
        }

        /// <summary>
        /// Handle text changes and apply formatting
        /// </summary>
        private void GuidFormattedTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isFormatting)
                return;

            int cursorPosition = this.SelectionStart;
            string currentText = this.Text;

            // Get only digits
            string digits = GuidExtention.GetDigitsOnly(currentText);

            // Limit to 10 digits
            if (digits.Length > 10)
            {
                digits = digits.Substring(0, 10);
            }

            // Format the number
            string formattedText = GuidExtention.FormatNumber(digits);

            // Calculate new cursor position
            int newCursorPosition = CalculateNewCursorPosition(
                currentText,
                formattedText,
                cursorPosition
            );

            // Update text
            _isFormatting = true;
            this.Text = formattedText;
            this.SelectionStart = Math.Min(newCursorPosition, this.Text.Length);
            _isFormatting = false;
        }

        /// <summary>
        /// Calculate cursor position after formatting
        /// </summary>
        private int CalculateNewCursorPosition(string oldText, string newText, int oldPosition)
        {
            // Count digits before cursor in old text
            int digitsBefore = 0;
            for (int i = 0; i < Math.Min(oldPosition, oldText.Length); i++)
            {
                if (char.IsDigit(oldText[i]))
                    digitsBefore++;
            }

            // Find position in new text that has same number of digits before it
            int digitsCount = 0;
            for (int i = 0; i < newText.Length; i++)
            {
                if (char.IsDigit(newText[i]))
                {
                    digitsCount++;
                    if (digitsCount == digitsBefore)
                        return i + 1;
                }
            }

            return newText.Length;
        }

        /// <summary>
        /// Handle key press - allow only digits
        /// </summary>
        private void GuidFormattedTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_isReadOnly)
            {
                if (char.IsControl(e.KeyChar))
                {
                    e.Handled = false;
                    return;
                }

                e.Handled = true;
                return;
            }

            // Allow control keys (backspace, delete, etc.)
            if (char.IsControl(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            // Allow only digits
            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            // Check if we already have 10 digits
            string digits = GuidExtention.GetDigitsOnly(this.Text);
            if (digits.Length >= 10)
            {
                // If text is selected, allow replacement
                if (this.SelectionLength == 0)
                {
                    SelectionLength = 1;
                    //e.Handled = true;
                }
            }
        }

        public bool CopyOnly
        {
            get => _isReadOnly;
            set
            {
                _isReadOnly = value;

                if (_isReadOnly)
                {
                    this.ReadOnly = false; this.Enabled = true; this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240); this.ForeColor = System.Drawing.Color.DarkGray;
                }
                else
                {
                    // ✅ Normal mode
                    this.BackColor = System.Drawing.Color.White;
                    this.ForeColor = System.Drawing.Color.Black;
                }
            }
        }

        private static readonly Keys[] AllowedKeys = new Keys[]
        {
            Keys.C,           // Ctrl+C
            Keys.A,           // Ctrl+A
            Keys.Tab,         // Tab
            Keys.Enter,       // Enter
            Keys. Left,                    Keys.Right,
            Keys.Up,
            Keys.Down,
            Keys.Home,
            Keys.End,
            Keys.PageUp,
            Keys.PageDown,
            Keys. Escape,
            Keys. ControlKey,
            Keys.ShiftKey,
        };

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Override WndProc to handle Copy and Paste operations
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (_isReadOnly)
            {
                if (m.Msg == 0x0302) // WM_PASTE
                {
                    m.Result = IntPtr.Zero;
                    return;
                }

                if (m.Msg == 0x0300) // WM_CUT
                {
                    m.Result = IntPtr.Zero;
                    return;
                }

                if (m.Msg == 0x000C) // WM_SETTEXT
                {
                    return;
                }

                if (m.Msg == 0x0301) // WM_COPY
                {
                    HandleCopy();
                    return;
                }
            }
            else
            {
                // WM_COPY = 0x0301
                if (m.Msg == 0x0301)
                {
                    HandleCopy();
                    return; // Don't call base.WndProc for copy
                }

                // WM_PASTE = 0x0302
                if (m.Msg == 0x0302)
                {
                    HandlePaste();
                    return; // Don't call base.WndProc for paste
                }

                // WM_CUT = 0x0300
                if (m.Msg == 0x0300)
                {
                    HandleCut();
                    return; // Don't call base.WndProc for cut
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Handle Copy - Copy only digits without spaces
        /// </summary>
        private void HandleCopy()
        {
            try
            {
                string textToCopy;

                if (this.SelectionLength > 0)
                {
                    string selectedText = this.SelectedText;
                    //textToCopy = GuidExtention.GetDigitsOnly(selectedText);
                    textToCopy = selectedText;
                }
                else
                {
                    textToCopy = Text;
                }

                if (!string.IsNullOrEmpty(textToCopy))
                {
                    Clipboard.SetText(textToCopy);
                }
            }
            catch
            {
                var msg = new Message { Msg = 0x0301 };
                base.WndProc(ref msg);
            }
        }

        /// <summary>
        /// Handle Cut - Copy digits and clear selection
        /// </summary>
        private void HandleCut()
        {
            try
            {
                if (this.SelectionLength > 0)
                {
                    string selectedText = this.SelectedText;
                    string digitsToCopy = GuidExtention.GetDigitsOnly(selectedText);

                    if (!string.IsNullOrEmpty(digitsToCopy))
                    {
                        Clipboard.SetText(digitsToCopy);
                    }

                    int selStart = this.SelectionStart;
                    string beforeSelection = this.Text.Substring(0, selStart);
                    string afterSelection = this.Text.Substring(selStart + this.SelectionLength);

                    string beforeDigits = GuidExtention.GetDigitsOnly(beforeSelection);
                    string afterDigits = GuidExtention.GetDigitsOnly(afterSelection);

                    string newDigits = beforeDigits + afterDigits;
                    this.Text = GuidExtention.FormatNumber(newDigits);

                    this.SelectionStart = GuidExtention.FormatNumber(beforeDigits).Length;
                }
            }
            catch
            {
                var msg = new Message { Msg = 0x0300 };
                base.WndProc(ref msg);
            }
        }

        /// <summary>
        /// Handle Paste - Paste only digits
        /// </summary>
        private void HandlePaste()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();
                    string digitsOnly = GuidExtention.GetDigitsOnly(clipboardText);

                    if (!string.IsNullOrEmpty(digitsOnly))
                    {
                        // Limit to available space
                        string currentDigits = GuidExtention.GetDigitsOnly(this.Text);
                        int availableSpace = 10 - currentDigits.Length + this.SelectionLength;

                        if (availableSpace > 0)
                        {
                            digitsOnly = digitsOnly.Substring(0, Math.Min(digitsOnly.Length, availableSpace));

                            // Insert the digits
                            int selStart = this.SelectionStart;
                            string beforeSelection = this.Text.Substring(0, selStart);
                            string afterSelection = this.Text.Substring(selStart + this.SelectionLength);

                            string beforeDigits = GuidExtention.GetDigitsOnly(beforeSelection);
                            string afterDigits = GuidExtention.GetDigitsOnly(afterSelection);

                            string newDigits = beforeDigits + digitsOnly + afterDigits;
                            if (newDigits.Length > 10)
                                newDigits = newDigits.Substring(0, 10);

                            this.Text = GuidExtention.FormatNumber(newDigits);

                            // Position cursor after pasted content
                            int newPosition = GuidExtention.FormatNumber(beforeDigits + digitsOnly).Length;
                            this.SelectionStart = Math.Min(newPosition, this.Text.Length);
                        }
                    }
                }
            }
            catch
            {
                var msg = new Message { Msg = 0x0302 };
                base.WndProc(ref msg);
            }
        }

        /// <summary>
        /// Override OnKeyDown to handle Ctrl+C, Ctrl+V, Ctrl+X
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_isReadOnly)
            {
                if (e.Control && (e.KeyCode == Keys.C))
                {
                    HandleCopy();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                else if (e.KeyCode == Keys.A && e.Control)
                {
                    this.SelectAll();
                    e.Handled = true;
                    return;
                }
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                         e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                         e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
                {
                    e.Handled = false;
                    return;
                }
                else
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            // Ctrl+C (Copy)
            if (e.Control && e.KeyCode == Keys.C)
            {
                HandleCopy();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // Ctrl+X (Cut)
            if (e.Control && e.KeyCode == Keys.X)
            {
                HandleCut();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            // Ctrl+V (Paste)
            if (e.Control && e.KeyCode == Keys.V)
            {
                HandlePaste();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Set value from 10-digit string
        /// </summary>
        public void SetFromString(string value)
        {
            string digits = GuidExtention.GetDigitsOnly(value);
            if (digits.Length > 10)
                digits = digits.Substring(0, 10);

            this.Text = GuidExtention.FormatNumber(digits);
        }

        /// <summary>
        /// Validate if current text represents a valid 10-digit number
        /// </summary>
        public bool IsValid()
        {
            string digits = UnformattedNumber;
            return digits.Length == 10;
        }
    }
}