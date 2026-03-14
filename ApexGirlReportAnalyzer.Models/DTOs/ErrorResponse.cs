namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Represents a standardised error response returned by the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Human-readable description of the error.
    /// </summary>
    public string Message { get; set; } = "An error occurred while processing your request.";

    /// <summary>
    /// Error category (e.g. ValidationError, NotFound, InternalServerError, Conflict).
    /// </summary>
    public string Type { get; set; } = "Error";
}
