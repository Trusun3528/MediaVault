using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace PhotoStorage.Services
{
    public class LMStudioService
    {
        private readonly HttpClient _httpClient;
        private const string LMStudioBaseUrl = "http://localhost:1234/v1";

        public LMStudioService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                var response = await _httpClient.PostAsync($"{LMStudioBaseUrl}/completions", requestContent);
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

        public async Task<string> GenerateDescriptionAsync(string userDescription)
        {
            var requestContent = new StringContent(JsonSerializer.Serialize(new
            {
                messages = new[]
                {
                    new { role = "user", content = $"Improve this description: {userDescription}" }
                },
                model = "llava-v1.5-7b"
            }), System.Text.Encoding.UTF8, "application/json");

            Console.WriteLine("Request Payload:");
            Console.WriteLine(await requestContent.ReadAsStringAsync());

            try
            {
                var response = await _httpClient.PostAsync($"{LMStudioBaseUrl}/chat/completions", requestContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("LM Studio Response: " + jsonResponse); // Log the response

                var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                if (result.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    {
                        var improvedDescription = content.GetString();

                        // Ensure the description is meaningful and not just the file path
                        if (!string.IsNullOrEmpty(improvedDescription) && !improvedDescription.Contains("uploads"))
                        {
                            return improvedDescription;
                        }
                        else
                        {
                            Console.WriteLine("Generated description is invalid or contains the file path.");
                            return "AI description could not be generated.";
                        }
                    }
                    else
                    {
                        Console.WriteLine("'message.content' property not found in the first choice.");
                        return "AI description could not be generated.";
                    }
                }
                else
                {
                    Console.WriteLine("'choices' array not found or empty in the response.");
                    return "AI description could not be generated.";
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error calling LM Studio API: " + ex.Message);
                throw;
            }
        }
    }
}