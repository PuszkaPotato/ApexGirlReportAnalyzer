namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Response for batch upload operations
/// Contains summary statistics and individual results
/// </summary>
public class BatchUploadResponse
{
    /// <summary>
    /// Whether the batch operation was successful (at least one upload succeeded)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the entire batch failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of images submitted
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// Number of successfully processed uploads
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed uploads
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of duplicate images detected
    /// </summary>
    public int DuplicateCount { get; set; }

    /// <summary>
    /// Individual results for each image in the batch
    /// </summary>
    public List<UploadResponse> Results { get; set; } = new();
}