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
        var model = _configuration["OpenAI:Model"] ?? "gpt-4.1";
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
        var promptPath = _configuration["OpenAI:PromptPath"] 
            ?? Path.Combine(AppContext.BaseDirectory, "Prompts", "BattleAnalysisPrompt.txt");

        if (!File.Exists(promptPath))
        {
            _logger.LogError("Prompt file not found at: {PromptPath}", promptPath);
            throw new FileNotFoundException($"Prompt file not found at: {promptPath}");
        }

        return File.ReadAllText(promptPath);
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

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Check if AI flagged this as invalid image FIRST
            if (root.TryGetProperty("invalid", out var invalidEl) &&
                invalidEl.ValueKind == JsonValueKind.True)
            {
                var reason = root.TryGetProperty("reason", out var reasonEl) &&
                             reasonEl.ValueKind != JsonValueKind.Null
                    ? reasonEl.GetString() ?? "Not a battle report screenshot"
                    : "Not a battle report screenshot";

                _logger.LogWarning("OpenAI flagged image as invalid: {Reason}", reason);

                return new BattleReportResponse
                {
                    IsInvalid = true,
                    InvalidReason = reason
                };
            }

            // battleType
            var battleType = root.TryGetProperty("battleType", out var btEl) && btEl.ValueKind != JsonValueKind.Null
                ? btEl.GetString() ?? string.Empty
                : string.Empty;

            // battleDate: parse as flexible string to avoid strict DateTime deserialize errors
            DateTime battleDateUtc = DateTime.UtcNow;
            if (root.TryGetProperty("battleDate", out var bdEl) && bdEl.ValueKind != JsonValueKind.Null)
            {
                var bdStr = bdEl.GetString();
                if (!string.IsNullOrWhiteSpace(bdStr))
                {
                    // Try general parse with invariant culture and assume/adjust to UTC
                    if (!DateTime.TryParse(bdStr, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var parsed))
                    {
                        // Try round-trip formats
                        var formats = new[]
                        {
                            "o", // round-trip
                            "yyyy-MM-ddTHH:mm:ssK",
                            "yyyy-MM-ddTHH:mm:ssZ",
                            "yyyy-MM-dd",
                            "MM/dd/yyyy",
                            "dd MMM yyyy",
                            "dd-MM-yyyy"
                        };

                        if (!DateTime.TryParseExact(bdStr, formats, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                            out parsed))
                        {
                            _logger.LogWarning("Unable to parse battleDate from AI response: {BattleDateString}. Using UtcNow.", bdStr);
                            parsed = DateTime.UtcNow;
                        }
                    }
                    // Ensure UTC kind
                    battleDateUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }
            }

            // player and enemy: deserialize their subtrees
            var player = root.TryGetProperty("player", out var pEl) && pEl.ValueKind != JsonValueKind.Null
                ? JsonSerializer.Deserialize<BattleSideDto>(pEl.GetRawText(), options) ?? new BattleSideDto()
                : new BattleSideDto();

            var enemy = root.TryGetProperty("enemy", out var eEl) && eEl.ValueKind != JsonValueKind.Null
                ? JsonSerializer.Deserialize<BattleSideDto>(eEl.GetRawText(), options) ?? new BattleSideDto()
                : new BattleSideDto();


            return new BattleReportResponse
            {
                BattleType = battleType,
                BattleDate = battleDateUtc,
                Player = player,
                Enemy = enemy
            };
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Failed to parse AI JSON response. Raw response: {Response}", jsonString);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing AI response. Raw response: {Response}", jsonString);
            throw;
        }
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