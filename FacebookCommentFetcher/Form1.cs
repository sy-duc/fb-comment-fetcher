using FacebookCommentFetcher.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FacebookCommentFetcher
{
    public partial class Form1 : Form
    {
        private readonly FbApiService _fbApiService = new FbApiService();

        // Khai báo control để còn dùng trong các event sau này
        private TextBox txtToken = null!;
        private TextBox txtPostId = null!;
        private TextBox txtPageId = null!;
        private Button btnTest = null!;
        private Button btnStart = null!;
        private Button btnExport = null!;
        private RadioButton radioAll = null!;
        private RadioButton radioKeyword = null!;
        private TextBox txtKeyword = null!;
        private ProgressBar progressBar = null!;
        private DataGridView grid = null!;

        public Form1()
        {
            InitializeComponent();
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
                RowCount = 7,
                ColumnCount = 1,
                AutoSize = true,
                Padding = new Padding(10),
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Token
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // PostId
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // PageId
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
            tokenPanel.Controls.Add(txtToken);
            layout.Controls.Add(tokenPanel);

            // ===== POST ID =====
            var postIdPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            postIdPanel.Controls.Add(new Label { Text = "Post ID:", AutoSize = true, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtPostId = new TextBox { Width = 400 };
            postIdPanel.Controls.Add(txtPostId);
            layout.Controls.Add(postIdPanel);

            // ===== PAGE ID =====
            var pageIdPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            pageIdPanel.Controls.Add(new Label { Text = "Page ID:", AutoSize = true, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtPageId = new TextBox { Width = 400 };
            pageIdPanel.Controls.Add(txtPageId);
            layout.Controls.Add(pageIdPanel);

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

            // Thêm event handler cho việc click vào cell
            grid.CellContentClick += Grid_CellContentClick;
            grid.DataBindingComplete += Grid_DataBindingComplete;

            layout.Controls.Add(grid);
        }

        // ===== HANDLER TẠM THỜI =====
        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            try
            {
                string token = txtToken.Text.Trim();
                string postId = txtPostId.Text.Trim();
                string pageId = txtPageId.Text.Trim();

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(postId) || string.IsNullOrEmpty(pageId))
                {
                    MessageBox.Show("Vui lòng nhập Access Token, Post ID và Page ID.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var meta = await _fbApiService.GetPostMetadataAsync(postId, token);

                if (meta != null)
                {
                    // Thông báo kiểm tra kết nối thành công
                    string msg = $"Kết nối thành công!\n\nPost ID: {postId}\nPage ID: {pageId}\nCreated: {meta.Value.GetProperty("created_time").GetString()}";
                    MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Enable button cho phép bắt đầu fetch comments
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

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            btnStart.Enabled = false;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Value = 0;

            // Kiểm tra đã test chưa
            if (string.IsNullOrEmpty(txtPostId.Text.Trim()) || string.IsNullOrEmpty(txtToken.Text.Trim()))
            {
                MessageBox.Show("Vui lòng test kết nối trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var progress = new Progress<int>(value =>
                {
                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = Math.Min(value, progressBar.Maximum);
                });

                var comments = await _fbApiService.FetchCommentsAsync(txtPostId.Text.Trim(), txtToken.Text.Trim(), progress);
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


        private void BtnExport_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("📁 Xuất file Excel (chưa triển khai logic).", "Thông báo");
        }

        private void Grid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Tự động điều chỉnh cột để hiển thị tốt hơn
            if (grid.Columns.Contains("Id") && grid.Columns["Id"] != null)
            {
                grid.Columns["Id"]!.HeaderText = "Comment ID";
                grid.Columns["Id"]!.Width = 120;
                // Đặt style cho cột CommentId như một link
                grid.Columns["Id"]!.DefaultCellStyle.ForeColor = Color.Blue;
                grid.Columns["Id"]!.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Underline);
                grid.Columns["Id"]!.ToolTipText = "Click để xem người comment trên Facebook";
            }

            if (grid.Columns.Contains("Message") && grid.Columns["Message"] != null)
            {
                grid.Columns["Message"]!.HeaderText = "Nội dung";
                grid.Columns["Message"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            if (grid.Columns.Contains("CreatedTime") && grid.Columns["CreatedTime"] != null)
            {
                grid.Columns["CreatedTime"]!.HeaderText = "Thời gian";
                grid.Columns["CreatedTime"]!.Width = 150;
            }

            // Thêm tooltip cho toàn bộ grid
            grid.ShowCellToolTips = true;
        }

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Kiểm tra nếu click vào cột CommentId
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var columnName = grid.Columns[e.ColumnIndex].Name;

                if (columnName == "Id")
                {
                    var commentUrl = grid.Rows[e.RowIndex].Cells["CommentUrl"].Value?.ToString();

                    if (!string.IsNullOrEmpty(commentUrl))
                    {
                        try
                        {
                            // Mở browser với URL của comment
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = commentUrl,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Không thể mở browser: {ex.Message}",
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
        }
    }
}
