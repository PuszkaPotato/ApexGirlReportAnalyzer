using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Core.Interfaces;

/// <summary>
/// Service for interacting with OpenAI Vision API
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// Analyzes a battle report screenshot and extracts structured data
    /// </summary>
    /// <param name="base64Image">Base64-encoded screenshot</param>
    /// <returns>Extracted battle report data</returns>
    Task<BattleReportResponse> AnalyzeScreenshotAsync(string base64Image);
}