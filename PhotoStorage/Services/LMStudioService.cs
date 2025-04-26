using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;

namespace PhotoStorage.Services
{
    public enum MediaType
    {
        Image,
        Video,
        Audio
    }

    public class LMStudioService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private const string ConfigFileName = "lmstudio_config.json";

        public LMStudioService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public string GetLMStudioUrl()
        {
            var configFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                var jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (jsonObject != null && jsonObject.TryGetValue("LMStudioUrl", out var url))
                {
                    Console.WriteLine($"Using LM Studio URL: {url}"); // Log the URL being used
                    return url;
                }
            }
            return _configuration["LMStudioUrl"] ?? "http://default-lm-studio-link.com";
        }

        public void SetLMStudioUrl(string newUrl)
        {
            var configFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            var jsonObject = new Dictionary<string, string> { { "LMStudioUrl", newUrl } };
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true }));
        }

        public async Task<string[]> AnalyzePhotoAsync(string photoPath)
        {
            var requestContent = new StringContent(JsonSerializer.Serialize(new
            {
                prompt = "Generate tags for the uploaded photo.",
                model = "llava-v1.5-7b"
            }), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{GetLMStudioUrl()}/completions", requestContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("LM Studio Response: " + jsonResponse); // Log the response

                var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                if (result.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var text = choices[0].GetProperty("text").GetString();
                    return text?.Split(',').Select(tag => tag.Trim()).Take(5).ToArray() ?? Array.Empty<string>();
                }
                else
                {
                    Console.WriteLine("'choices' array not found or empty in the response.");
                    return Array.Empty<string>();
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error calling LM Studio API: " + ex.Message);
                return Array.Empty<string>();
            }
        }

        public async Task<string> GenerateDescriptionAsync(string userDescription, MediaType mediaType)
        {
            string mediaTypeDescription = mediaType switch
            {
                MediaType.Image => "photo",
                MediaType.Video => "video",
                MediaType.Audio => "audio",
                _ => "media"
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(new
            {
                messages = new[]
                {
                    new { role = "user", content = $"Improve this description for a {mediaTypeDescription}: {userDescription}" }
                },
                model = "llava-v1.5-7b"
            }), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{GetLMStudioUrl()}/v1/chat/completions", requestContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                if (result.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? "AI description could not be generated.";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error calling LM Studio API: " + ex.Message);
            }

            return "AI description could not be generated.";
        }
    }
}