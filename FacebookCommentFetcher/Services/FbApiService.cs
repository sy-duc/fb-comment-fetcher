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
            string accessToken,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var allComments = new List<CommentInfo>();
            string? nextUrl = $"https://graph.facebook.com/v21.0/{postId}/comments?fields=id,message,from,created_time&limit=100&access_token={accessToken}";

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
