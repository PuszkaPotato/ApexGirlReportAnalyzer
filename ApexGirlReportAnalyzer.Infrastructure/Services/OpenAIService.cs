using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Configure HttpClient
        var apiKey = _configuration["OpenAI:ApiKey"];
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<BattleReportResponse> AnalyzeScreenshotAsync(string base64Image)
    {
        try
        {
            _logger.LogInformation("Starting OpenAI analysis...");

            // Build the request
            var requestBody = BuildRequest(base64Image);

            // Call OpenAI API
            var apiUrl = _configuration["OpenAI:ApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
            var response = await _httpClient.PostAsync(
                apiUrl,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            // Check for errors
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"OpenAI API returned {response.StatusCode}: {errorContent}");
            }

            // Parse response
            var responseContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var openAiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, options);

            // ADD THIS LINE - Log the raw response
            _logger.LogInformation("Raw OpenAI response: {Response}", responseContent);

            if (openAiResponse?.Choices == null || openAiResponse.Choices.Length == 0)
            {
                _logger.LogError("OpenAI response was empty or invalid. Full response: {Response}", responseContent);
                throw new InvalidOperationException("OpenAI returned empty response");
            }

            // Extract the JSON from AI's response
            var aiMessage = openAiResponse.Choices[0].Message.Content;
            _logger.LogInformation("Received AI response, parsing battle data...");

            // Parse the battle data
            var battleData = ParseBattleData(aiMessage);

            // Calculate cost
            if (openAiResponse.Usage != null)
            {
                battleData.TokensUsed = openAiResponse.Usage.TotalTokens;
                battleData.EstimatedCost = CalculateCost(openAiResponse.Usage);
            }

            _logger.LogInformation("Analysis complete. Tokens used: {Tokens}, Cost: ${Cost:F4}",
                battleData.TokensUsed, battleData.EstimatedCost);

            return battleData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing screenshot");
            throw;
        }
    }

    private object BuildRequest(string base64Image)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var maxTokens = int.TryParse(_configuration["OpenAI:MaxTokens"], out var tokens) ? tokens : 1500;

        return new
        {
            model = model,
            max_tokens = maxTokens,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = GetPrompt()
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:image/png;base64,{base64Image}"
                            }
                        }
                    }
                }
            }
        };
    }

    private string GetPrompt()
    {
        // We'll refine this prompt in the next session!
        return @"
        You are analyzing a battle report screenshot from the mobile game Apex Girl.

        Extract the following information and return it as valid JSON (and ONLY JSON, no other text):

        {
            ""battleType"": ""string (e.g., Parking War, Great Win, etc.)"",
            ""battleDate"": ""ISO 8601 datetime from screenshot"",
            ""player"": {
            ""username"": ""string (username ONLY, without group tag)"",
            ""groupTag"": ""string or null (e.g., CTS, ALTR - without brackets)"",
            ""level"": ""integer or null"",
            ""fanCount"": ""integer"",
            ""lossCount"": ""integer"",
            ""injuredCount"": ""integer"",
            ""remainingCount"": ""integer"",
            ""reinforceCount"": ""integer or null"",
            ""sing"": ""integer"",
            ""dance"": ""integer"",
            ""activeSkill"": ""integer"",
            ""basicAttackBonus"": ""integer (percentage as whole number, e.g., 172)"",
            ""reduceBasicAttackDamage"": ""integer"",
            ""skillBonus"": ""integer"",
            ""skillReduction"": ""integer"",
            ""extraDamage"": ""integer""
            },
            ""enemy"": {
            // Same structure as player
            }
        }

        IMPORTANT:
        - Return ONLY valid JSON, no markdown, no explanations
        - All numbers should be positive integers (no commas, no negative signs - e.g., 881400 not 881,400 or -20)
        - Red text does NOT mean negative values - treat all stats as positive/absolute values
        - Use null for missing values
        - Be precise with numbers from the screenshot
        - The player is on the LEFT side of the Battle Overview, the enemy is on the RIGHT side
        - Username and groupTag are SEPARATE fields - do not concatenate them (e.g., username: ""QueenVee"", groupTag: ""CTS"", NOT username: ""CTSQueenVee"")
        - groupTag should NOT include brackets - just the tag itself (e.g., ""CTS"" not ""[CTS]"")
        - Carefully match each stat to its correct position - sing/dance are different from fan counts and losses

        ";
    }

    private BattleReportResponse ParseBattleData(string aiResponse)
    {
        // Remove markdown code blocks if present
        var jsonString = aiResponse.Trim();
        if (jsonString.StartsWith("```json"))
        {
            jsonString = jsonString.Substring(7); // Remove ```json
        }
        if (jsonString.StartsWith("```"))
        {
            jsonString = jsonString.Substring(3); // Remove ```
        }
        if (jsonString.EndsWith("```"))
        {
            jsonString = jsonString.Substring(0, jsonString.Length - 3); // Remove ```
        }
        jsonString = jsonString.Trim();

        // Parse JSON
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var battleData = JsonSerializer.Deserialize<BattleReportResponse>(jsonString, options);

        if (battleData == null)
        {
            throw new InvalidOperationException("Failed to parse battle data from AI response");
        }

        return battleData;
    }

    private decimal CalculateCost(Usage usage)
    {
        // GPT-4.1-nano pricing (as of Jan 2026)
        const decimal inputCostPer1M = 0.2m;   // $0.20 per 1M input tokens
        const decimal outputCostPer1M = 0.8m; // $0.80 per 1M output tokens

        var inputCost = (usage.PromptTokens / 1_000_000m) * inputCostPer1M;
        var outputCost = (usage.CompletionTokens / 1_000_000m) * outputCostPer1M;

        return inputCost + outputCost;
    }

    // OpenAI API response models
    private class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}