using FacebookCommentFetcher.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FacebookCommentFetcher
{
    public partial class Form1 : Form
    {
        // Khai báo control để còn dùng trong các event sau này
        private TextBox txtToken;
        private CheckBox chkSaveToken;
        private TextBox txtLink;
        private Button btnTest;
        private Button btnStart;
        private Button btnExport;
        private RadioButton radioAll;
        private RadioButton radioKeyword;
        private TextBox txtKeyword;
        private ProgressBar progressBar;
        private DataGridView grid;

        private AppConfig _config;

        public Form1()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            _config = ConfigService.LoadConfig();
            txtToken.Text = _config.AccessToken;
        }

        private void InitializeComponent()
        {
            // ===== FORM CƠ BẢN =====
            this.Text = "Facebook Comment Fetcher";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ===== TABLE LAYOUT =====
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                AutoSize = true,
                Padding = new Padding(10),
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Token
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Link
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Filters
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Progress
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid
            this.Controls.Add(layout);

            // ===== ACCESS TOKEN =====
            var tokenPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            tokenPanel.Controls.Add(new Label { Text = "Access Token:", AutoSize = true, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtToken = new TextBox { Width = 600 };
            chkSaveToken = new CheckBox { Text = "Lưu Token", AutoSize = true, Margin = new Padding(10, 3, 0, 0) };
            tokenPanel.Controls.Add(txtToken);
            tokenPanel.Controls.Add(chkSaveToken);
            layout.Controls.Add(tokenPanel);
            chkSaveToken.CheckedChanged += ChkSaveToken_CheckedChanged;

            // ===== LINK BÀI VIẾT =====
            var linkPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            linkPanel.Controls.Add(new Label { Text = "Post Link:", AutoSize = true, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtLink = new TextBox { Width = 800 };
            linkPanel.Controls.Add(txtLink);
            layout.Controls.Add(linkPanel);

            // ===== CÁC NÚT CHÍNH =====
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            btnTest = new Button { Text = "Test Connection", Width = 150 };
            btnStart = new Button { Text = "Start Fetching", Width = 150, Enabled = false };
            btnExport = new Button { Text = "Export Excel", Width = 150, Enabled = false };

            btnTest.Click += BtnTest_Click;
            btnStart.Click += BtnStart_Click;
            btnExport.Click += BtnExport_Click;

            btnPanel.Controls.AddRange(new Control[] { btnTest, btnStart, btnExport });
            layout.Controls.Add(btnPanel);

            // ===== FILTER OPTIONS =====
            var filterPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            radioAll = new RadioButton { Text = "Lấy toàn bộ bình luận", Checked = true, AutoSize = true };
            radioKeyword = new RadioButton { Text = "Lấy bình luận chứa từ khóa:", AutoSize = true };
            txtKeyword = new TextBox { Width = 200, Enabled = false };
            filterPanel.Controls.AddRange(new Control[] { radioAll, radioKeyword, txtKeyword });
            layout.Controls.Add(filterPanel);

            radioAll.CheckedChanged += (s, e) =>
            {
                txtKeyword.Enabled = !radioAll.Checked;
            };

            // ===== PROGRESS BAR =====
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 25
            };
            layout.Controls.Add(progressBar);

            // ===== DATA GRID =====
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            layout.Controls.Add(grid);
        }

        // ===== HANDLER TẠM THỜI =====
        private async void BtnTest_Click(object sender, EventArgs e)
        {
            try
            {
                string token = txtToken.Text.Trim();
                string link = txtLink.Text.Trim();

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(link))
                {
                    MessageBox.Show("Vui lòng nhập Access Token và Link bài viết.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var fb = new FbApiService();

                var (pageId, postId) = await fb.ResolvePostInfoAsync(link);
                var meta = await fb.GetPostMetadataAsync(postId, token);

                if (meta != null)
                {
                    string msg = $"Kết nối thành công!\n\nPost ID: {postId}\nPage ID: {pageId}\nCreated: {meta.Value.GetProperty("created_time").GetString()}";
                    MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnStart.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Không tìm thấy thông tin bài viết.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnStart.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStart.Enabled = false;
            }
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Value = 0;

            try
            {
                var progress = new Progress<int>(value =>
                {
                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = Math.Min(value, progressBar.Maximum);
                });

                var comments = await _facebookApiService.FetchCommentsAsync(txtPostId.Text, progress);
                grid.DataSource = comments;

                MessageBox.Show($"Đã tải {comments.Count} bình luận!");
                btnStart.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy comment: {ex.Message}");
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                btnStart.Enabled = true;
            }
        }


        private void BtnExport_Click(object sender, EventArgs e)
        {
            MessageBox.Show("📁 Xuất file Excel (chưa triển khai logic).", "Thông báo");
        }

        private void ChkSaveToken_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkSaveToken.Checked)
            {
                _config.AccessToken = txtToken.Text.Trim();
                ConfigService.SaveConfig(_config);

                MessageBox.Show("Đã lưu Access Token vào cấu hình!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
