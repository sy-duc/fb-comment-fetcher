using System.Text.Json;

namespace FacebookCommentFetcher.Services
{
    public class AppConfig
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    internal class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch
            {
                // có thể log lỗi, nhưng không crash app
            }

            return new AppConfig();
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // ignore lỗi ghi file
            }
        }
    }
}
