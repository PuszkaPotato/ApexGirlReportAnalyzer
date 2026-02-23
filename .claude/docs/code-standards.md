# Code Standards

These standards are derived from existing patterns in the codebase. Follow these to maintain consistency.

---

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes, Records | PascalCase | `UploadService`, `BattleReport` |
| Interfaces | `I` prefix + PascalCase | `IUploadService`, `IUserService` |
| Methods | PascalCase | `ProcessUploadAsync`, `ValidateQuota` |
| Properties | PascalCase | `UserId`, `BattleData` |
| Private fields | `_camelCase` | `_context`, `_logger` |
| Parameters | camelCase | `userId`, `base64Image` |
| Async methods | `Async` suffix | `GetRemainingQuotaAsync` |
| DTOs | `*Request`, `*Response`, `*Dto` | `UploadRequest`, `BattleReportResponse` |

---

## File Structure

**Namespaces:** Use file-scoped namespaces (single line, no braces)
```csharp
namespace ApexGirlReportAnalyzer.API.Controllers;
```

**Usings:** At top of file, no global usings file
```csharp
using ApexGirlReportAnalyzer.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
```

**One class per file** (except small related classes like `QuotaInfo` inside `UploadResponse.cs`)

---

## Controllers

**Use traditional constructors** (same as services, for consistency):
```csharp
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IUploadService uploadService, ILogger<UploadController> logger)
    {
        _uploadService = uploadService;
        _logger = logger;
    }
}
```

**Attributes:**
- `[ApiController]` on all controllers
- `[Route("api/[controller]")]` for base route
- `[HttpGet]`, `[HttpPost]`, etc. with route template if needed
- `[ProducesResponseType]` for documenting response types
- `[FromQuery]`, `[FromForm]`, `[FromBody]` explicit parameter binding

**XML documentation** on all public endpoints:
```csharp
/// <summary>
/// Upload a single battle report screenshot for analysis
/// </summary>
/// <param name="image">Screenshot image file (PNG or JPEG)</param>
/// <returns>Analysis results or error</returns>
[HttpPost]
public async Task<IActionResult> Upload(...)
```

---

## Services

**Use traditional constructors** (same pattern as controllers):
```csharp
public class UploadService : IUploadService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UploadService> _logger;

    public UploadService(AppDbContext context, ILogger<UploadService> logger)
    {
        _context = context;
        _logger = logger;
    }
}
```

**Organize with regions** for private helpers:
```csharp
#region Private Helper Methods

// private helper methods here

#endregion
```

**Single Responsibility — keep services focused:**
- A service should only contain logic that belongs to its domain
- `UserService` handles user operations (existence checks, quota, get-or-create)
- `OpenAIService` handles all OpenAI interaction (calling API, parsing response, invalid image detection)
- `UploadService` orchestrates the upload flow — delegates to other services
- Shared pure functions (no DI needed) → static helper classes (e.g., `HashHelper`, `UploadResponseHelper`)

---

## Entities

**Inherit from BaseEntity:**
```csharp
public class Upload : BaseEntity
{
    // BaseEntity provides Id and CreatedAt
}
```

**Property patterns:**
```csharp
// Required non-nullable (must be set at creation)
public required string ImageHash { get; set; }

// Navigation properties (EF will populate)
public User User { get; set; } = null!;

// Optional navigation
public BattleReport? BattleReport { get; set; }

// Soft delete
public DateTime? DeletedAt { get; set; }

// Collections (initialize to prevent null)
public ICollection<AnalyticsEvent> AnalyticsEvents { get; set; } = new List<AnalyticsEvent>();

// Default values
public PrivacyScope PrivacyScope { get; set; } = PrivacyScope.Public;
```

---

## DTOs

**XML documentation on all properties:**
```csharp
/// <summary>
/// Response model for screenshot upload
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// Indicates if the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
```

**String defaults:**
```csharp
public string TierName { get; set; } = string.Empty;
```

---

## Interfaces

**XML documentation on interface and methods:**
```csharp
/// <summary>
/// Service for handling screenshot uploads and processing
/// </summary>
public interface IUploadService
{
    /// <summary>
    /// Process a screenshot upload
    /// </summary>
    /// <param name="base64Image">Base64-encoded image</param>
    /// <returns>Upload response with battle data or error</returns>
    Task<UploadResponse> ProcessUploadAsync(string base64Image, Guid userId, ...);
}
```

---

## Error Handling

**Early return pattern for validation:**
```csharp
var userIdValidation = UploadValidationHelper.ValidateUserId(userId);
if (userIdValidation != null)
    return userIdValidation;
```

**Try-catch at service boundaries:**
```csharp
try
{
    // Main logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing upload for user {UserId}", userId);
    return CreateErrorResponse("An unexpected error occurred");
}
```

**Debug vs Release error messages:**
```csharp
#if DEBUG
    return $"{ex.Message} | {ex.InnerException?.Message}";
#else
    return "An unexpected error occurred";
#endif
```

---

## Logging

**Use structured logging with templates:**
```csharp
_logger.LogInformation("Processing upload for user {UserId}. File: {FileName}", userId, image.FileName);
_logger.LogWarning("User {UserId} quota validation failed: {ErrorMessage}", userId, errorMessage);
_logger.LogError(ex, "Unexpected error during upload processing for user {UserId}", userId);
```

---

## Async/Await

- All I/O operations are async
- Always use `Async` suffix
- Pass `CancellationToken` through the call chain where available
- Use `await` not `.Result` or `.Wait()`

---

## Database Queries

**DateTime handling (PostgreSQL):**
```csharp
// Always specify UTC kind for date queries
var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
```

**Include for eager loading:**
```csharp
await _context.Uploads
    .Include(u => u.BattleReport)
    .FirstOrDefaultAsync(u => u.ImageHash == imageHash);
```

**Soft delete filter:**
```csharp
.Where(u => u.DeletedAt == null)
```

---

## Project References

```
API → Core, Infrastructure, Models
Infrastructure → Core, Models
Core → Models
Models → (no dependencies)
```

---

## Mappers

Entity → DTO mapping is extracted into static mapper classes in `Infrastructure/Mappers/`:

```csharp
public static class UserMapper
{
    public static UserResponse ToDto(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        return new UserResponse
        {
            Id = user.Id,
            TierName = user.Tier?.Name,
            // ...
        };
    }
}
```

**Rules:**
- One mapper class per entity
- Static class with static methods
- Null guard with `ArgumentNullException` (not silent empty return)
- Named `ToDto` for entity → DTO, `ToEntity` for the reverse if needed
- Apply the **boy scout rule**: extract inline mapping to a mapper when you touch a file

---

## Comments

**Don't use comments as section headers:**
```csharp
// BAD - just names what the code does
// Create BattleReport
var battleReport = new BattleReport { ... };

// GOOD - code is self-documenting, no comment needed
var battleReport = new BattleReport { ... };
```

**If you feel the need to comment a block, extract it to a method:**
```csharp
// BAD
// Create and configure identity
var identity = new ClaimsIdentity();
identity.AddClaim(...);
// ...

// GOOD
var identity = CreateIdentity(...);
```

**Comments that explain WHY are fine:**
```csharp
// Quota not consumed for duplicates — user already paid for this upload
RemainingQuota = quotaInfo
```

---

## Known Inconsistencies to Resolve

1. **Entity namespaces:** `Upload.cs` uses braces `namespace X { }`, newer files use file-scoped `namespace X;`
   - **Standard:** Use file-scoped namespaces going forward

2. **Constructor style:** Some controllers still use primary constructors
   - **Standard:** Use traditional constructors everywhere for consistency and debuggability

3. **Inline entity→DTO mapping:** Older services may map entities to DTOs inline instead of using a mapper class
   - **Standard:** Use mapper classes in `Infrastructure/Mappers/`
   - **Action:** Refactor inline mapping when touching those files (boy scout rule), not as a big-bang pass
