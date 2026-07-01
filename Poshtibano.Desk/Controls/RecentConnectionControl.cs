using Poshtibano.Desk.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Poshtibano.Desk.Controls
{
    public class RecentConnectionControl : Panel
    {
        private RecentConnection _connection;
        private Label _labelName;
        private Label _labelDetails;
        private Button _buttonConnect;
        private Button _buttonMenu;
        private ContextMenuStrip _contextMenu;

        public event Action<RecentConnection> OnConnectClicked;
        public event Action<RecentConnection> OnRenameClicked;
        public event Action<RecentConnection> OnDeleteClicked;

        public RecentConnection Connection
        {
            get => _connection;
            set
            {
                _connection = value;
                _connection.OnUpdateUi += () => UpdateUI();
                UpdateUI();
            }
        }

        public RecentConnectionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(460, 70);
            this.BackColor = Color.White;
            this.Margin = new Padding(5);
            this.Padding = new Padding(10);

            _labelName = new Label
            {
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = false,
                Size = new Size(250, 25),
                Location = new Point(170, 10),
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            };

            _labelDetails = new Label
            {
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(250, 20),
                Location = new Point(170, 35),
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            };

            _buttonConnect = new Button
            {
                Text = "اتصال",
                Size = new Size(80, 50),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(255, 128, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _buttonConnect.FlatAppearance.BorderSize = 0;
            _buttonConnect.Click += (s, e) => OnConnectClicked?.Invoke(_connection);

            _buttonMenu = new Button
            {
                Text = "⋮",
                Size = new Size(40, 50),
                Location = new Point(95, 10),
                BackColor = Color.FromArgb(236, 240, 241),
                ForeColor = Color.FromArgb(52, 73, 94),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _buttonMenu.FlatAppearance.BorderSize = 0;
            _buttonMenu.Click += ButtonMenu_Click;

            _contextMenu = new ContextMenuStrip();
            _contextMenu.RightToLeft = RightToLeft.Yes;

            var renameItem = new ToolStripMenuItem("تغییر نام");
            renameItem.Click += (s, e) => OnRenameClicked?.Invoke(_connection);

            var deleteItem = new ToolStripMenuItem("حذف");
            deleteItem.Click += (s, e) => OnDeleteClicked?.Invoke(_connection);

            _contextMenu.Items.Add(renameItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(deleteItem);

            this.Controls.Add(_labelName);
            this.Controls.Add(_labelDetails);
            this.Controls.Add(_buttonConnect);
            this.Controls.Add(_buttonMenu);

            // Hover effect
            this.MouseEnter += (s, e) => this.BackColor = Color.FromArgb(245, 246, 250);
            this.MouseLeave += (s, e) => this.BackColor = Color.White;
        }

        private void ButtonMenu_Click(object sender, EventArgs e)
        {
            _contextMenu.Show(_buttonMenu, new Point(0, _buttonMenu.Height));
        }

        private void UpdateUI()
        {
            if (_connection == null) return;

            _labelName.Text = string.IsNullOrWhiteSpace(_connection.DisplayName)
                ? _connection.Id.ToString("N").Substring(0, 10)
                : _connection.DisplayName;

            _labelDetails.Text = $"{_connection.GetPersianDate()} • {_connection.ConnectionCount} بار";
        }
    }
}