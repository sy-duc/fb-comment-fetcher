using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FacebookCommentFetcher.Services
{
    internal class FbApiService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<(string pageId, string postId)> ResolvePostInfoAsync(string postLink)
        {
            if (string.IsNullOrWhiteSpace(postLink))
                throw new ArgumentException("Link bài viết không được để trống.");

            // 💡 Chuẩn hóa link (xóa query string)
            postLink = postLink.Split('?')[0];

            // ===== Các pattern phổ biến =====
            // 1. Dạng cũ: https://www.facebook.com/<page>/posts/<post_id>
            var match = Regex.Match(postLink, @"facebook\.com/.+?/posts/(\d+)");
            if (match.Success)
                return ("", match.Groups[1].Value);

            // 2. Dạng mới: https://www.facebook.com/<page>/videos/<post_id>
            match = Regex.Match(postLink, @"facebook\.com/.+?/videos/(\d+)");
            if (match.Success)
                return ("", match.Groups[1].Value);

            // 3. Dạng chia sẻ: https://www.facebook.com/share/p/<short_id>/
            match = Regex.Match(postLink, @"facebook\.com/share/p/(\w+)");
            if (match.Success)
            {
                // Facebook hiện tại dùng shortlink → không thể extract trực tiếp
                throw new InvalidOperationException("Link chia sẻ (share/p/...) không thể lấy post_id trực tiếp. Hãy mở link và copy lại link đầy đủ của bài viết.");
            }

            // 4. Dạng photo: https://www.facebook.com/photo/?fbid=123456789&set=a.987654321
            match = Regex.Match(postLink, @"fbid=(\d+)");
            if (match.Success)
                return ("", match.Groups[1].Value);

            throw new InvalidOperationException("Không thể phân tích được link bài viết. Hãy đảm bảo bạn dán đúng link đầy đủ.");
        }

        public async Task<JsonElement?> GetPostMetadataAsync(string postId, string accessToken)
        {
            string url = $"https://graph.facebook.com/v23.0/{postId}?fields=id,created_time,from,message,permalink_url&access_token={accessToken}";
            var response = await _httpClient.GetAsync(url);

            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Facebook API lỗi: {content}");

            var json = JsonDocument.Parse(content);
            return json.RootElement;
        }

        public async Task<List<CommentInfo>> FetchCommentsAsync(
            string postId,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var allComments = new List<CommentInfo>();
            string? nextUrl = $"https://graph.facebook.com/v21.0/{postId}/comments?fields=id,message,from,created_time&limit=100&access_token={_accessToken}";

            int pageCount = 0;
            while (!string.IsNullOrEmpty(nextUrl))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var response = await _httpClient.GetAsync(nextUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var commentId = item.GetProperty("id").GetString() ?? "";
                        var comment = new CommentInfo
                        {
                            Id = commentId,
                            Message = item.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "",
                            CreatedTime = item.TryGetProperty("created_time", out var created)
                                ? DateTime.Parse(created.GetString() ?? "")
                                : DateTime.MinValue,
                            CommentUrl = $"https://www.facebook.com/{postId}/?comment_id={commentId}"
                        };
                        allComments.Add(comment);
                    }
                }

                pageCount++;
                progress?.Report(pageCount);

                if (doc.RootElement.TryGetProperty("paging", out var paging) &&
                    paging.TryGetProperty("next", out var next))
                {
                    nextUrl = next.GetString();
                }
                else
                {
                    nextUrl = null;
                }

                await Task.Delay(300, cancellationToken);
            }

            return allComments;
        }
    }
}
