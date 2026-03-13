# Technical Decisions Log

This document tracks key technical decisions made during development, including rationale, alternatives considered, and trade-offs.

---

## Model Selection & Optimization

### Decision 18: OpenAI Model Selection (gpt-4.1-mini)

**Chosen:** gpt-4.1-mini as primary model
**Alternatives Considered:** gpt-4.1, gpt-4.1-nano, gpt-4o, gpt-4o-mini
**Date:** January 2026

**Reasoning:**
- **Empirical testing** - Tested 15 unique screenshots repeatedly (100+ total requests)
- **Identical accuracy** - gpt-4.1-mini achieved 100% extraction success (15/15), same as gpt-4.1
- **Performance** - 2x faster than gpt-4.1 (5.53s vs 11.18s average)
- **Cost efficiency** - 4.8x cheaper than gpt-4.1 ($0.0016 vs $0.0080 per request)
- **Production value** - EUR10 gets 6,393 requests vs 1,318 with gpt-4.1

**Test Results:**
```
GPT-4.1-MINI:
- Extraction success: 100% (15/15 images)
- Field accuracy: 97.06% (battleDate excluded - formatting variance)
- Field errors (excluding battleDate): 0 - Perfect
- Average response time: 5.53s
- Cost per request: $0.001642

GPT-4.1 (for comparison):
- Extraction success: 100% (15/15 images)
- Field accuracy: 97.06% (identical)
- Average response time: 11.18s
- Cost per request: $0.007961
```

**Why not gpt-4.1-nano:**
- Initial tests showed struggles with image analysis
- Required too many retry attempts
- Not cost-effective despite lower base price

**Fallback Strategy:**
- Use gpt-4.1-mini as primary
- If validation fails or extraction errors detected, can retry with gpt-4.1
- Currently not implemented - may add if accuracy issues appear in production

---

## Validation & Security Decisions

### Decision 19: Image Validation Strategy (OpenAI Detection)

**Chosen:** OpenAI validates images, flag invalid, log patterns
**Alternatives Considered:** OCR pre-check (Tesseract), filename validation, dimension checks only
**Date:** January 2026

**Reasoning:**
- **Simplicity** - No additional dependencies (Tesseract, OpenCV)
- **Accuracy** - OpenAI understands context (mail screen vs battle report vs performance screen)
- **Cross-platform** - Works regardless of filename (Android: `Screenshot_...` vs iPhone: `Image123.png`)
- **Cost acceptable** - Invalid image costs $0.0006, blocking after 5 failures prevents abuse
- **User base trust** - Gaming group members want tool to work, unlikely to abuse
- **Graceful degradation** - Logs suspicious patterns, can block manually if needed

**Implementation:**
Updated OpenAI prompt to detect invalid images:
```
IMPORTANT: Verify this is an Apex Girl battle report showing "Battle Overview" screen.
If NOT a battle report, respond with:
{
  "invalid": true,
  "reason": "Not a battle report - appears to be [description]"
}
```

Service layer checks `IsInvalid` flag before processing.

**Why not OCR (Tesseract):**
- Additional native dependency complexity
- OCR accuracy issues with game fonts (gradients, stylized text)
- Different phone resolutions cause detection variance
- Maintenance burden for keyword lists
- Premature optimization - abuse isn't a real problem yet

**Why not filename validation:**
- iPhones save as `Image1234.png` instead of `Screenshot_..._ApexGirl.jpg`
- Trivial to bypass by renaming files
- False positives on legitimate uploads

**Abuse Prevention Strategy:**
- Log all failed uploads with "Invalid image" reason
- Future: Track failed count per user in 24h window
- Future: Soft block after 5 consecutive failures
- Future: Hard block after 10 failures in 24h
- **Current status:** Monitoring only - will implement if abuse detected

---

## Architecture Refinements

### Decision 20: Controller Organization (Separated by Concern)

**Chosen:** Separate controllers - UploadController, UserController, StatusController
**Alternatives Considered:** Monolithic UploadController with all endpoints
**Date:** January 2026

**Reasoning:**
- **Single Responsibility** - Each controller handles one domain
- **Testability** - Easier to test controllers in isolation
- **Scalability** - Clear where to add new endpoints
- **RESTful design** - `/api/upload`, `/api/user`, `/api/status` are clear resource boundaries

**Evolution:**
- Started with UploadController containing quota and health endpoints
- Refactored quota checking to `/api/user/quota/{id}`
- Moved health check to `/api/status/health`
- Upload remains focused on upload processing only

---

### Decision 21: Service Layer Extraction (UserService)

**Chosen:** Extract UserService from UploadService
**Alternatives Considered:** Keep quota logic in UploadService, create generic QuotaService
**Date:** January 2026

**Reasoning:**
- **Reusability** - UserController and UploadService both need quota information
- **Single Responsibility** - User operations belong together
- **Testability** - Can test quota logic independently from upload flow
- **Future Discord bot** - Bot will need user operations without upload context

**Implementation:**
Created `IUserService` with:
- `GetRemainingQuotaAsync(userId)` - Returns quota information
- `ValidateQuotaAsync(userId)` - Returns validation result with error message
- `HasQuotaAsync(userId)` - Simple boolean check

---

## Database & Infrastructure Decisions

### Decision 22: PostgreSQL DateTime Handling (UTC Enforcement)

**Chosen:** Explicitly specify DateTimeKind.Utc for all database queries
**Alternatives Considered:** Use DateTimeOffset, configure Npgsql globally, use TIMESTAMP instead of TIMESTAMPTZ
**Date:** January 2026

**Problem Encountered:**
```
System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type
'timestamp with time zone', only UTC is supported.
```

**Root Cause:**
- PostgreSQL's `timestamptz` requires explicit timezone information
- `DateTime.UtcNow.Date` strips time BUT also strips Kind (becomes Unspecified)
- Npgsql driver rejects Unspecified DateTimes for timestamptz columns

**Solution:**
```csharp
// Before (fails):
var today = DateTime.UtcNow.Date;

// After (works):
var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

// For constructed dates:
var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
```

**Where Applied:**
- All date range calculations in UserService quota queries
- Any DateTime used in EF Core LINQ queries
- Upload creation timestamps

---

### Decision 23: External Prompt Storage (Updated Feb 2026)

**Chosen:** Store prompt as two files — `BattleAnalysis.json` (metadata) + `BattleAnalysis.txt` (prompt text)
**Previous approach:** Single `BattleAnalysisPrompt.txt` file with `PromptPath` config
**Date:** January 2026, Updated February 2026

**Reasoning:**
- **Iteration speed** - Can modify prompt without rebuilding application
- **Version control** - Prompt changes tracked in Git
- **Readability** - Easier to edit multi-line prompt in dedicated `.txt` file
- **Version tracking** - JSON metadata file co-locates version with prompt (no config drift)
- **Deployment** - Can update prompt in production without code deployment (if needed)

**Structure:**
```
Prompts/
  BattleAnalysis.json   ← { "version": "1.0", "promptFile": "BattleAnalysis.txt" }
  BattleAnalysis.txt    ← prompt text (human-readable, easily editable)
```

**Configuration:**
```json
{
  "OpenAI": {
    "PromptName": "BattleAnalysis"
  }
}
```

**Why not JSON string for prompt:**
- JSON requires escaping all quotes and newlines — makes prompt unreadable and uneditable
- YAML is readable but adds a dependency and Veronica hates it
- Two files (JSON metadata + TXT content) gives best of both worlds

**Why version moved out of config:**
- Previously `OpenAI:PromptVersion` in appsettings — easy to forget to update when changing prompt
- Now version lives next to the prompt itself — single source of truth
- `PromptVersion` flows through `BattleReportResponse` → stored on `Upload` entity

---

## Lessons Learned from Phase 1

**Timeline:**
- Started: Monday, January 20, 2026
- Completed: Saturday, January 25, 2026
- Actual time: ~5 days (1 day minimal work Thursday)
- Estimated time: 3 weeks (20-30 hours)
- **Result: Completed ahead of schedule**

**What Went Well:**
- **Empirical testing paid off** - 100+ test requests gave confidence in model selection
- **Clean architecture from start** - Separation of concerns made refactoring easy
- **PostgreSQL worked great** - JSON columns for flexible data, timestamptz for proper timezone handling
- **OpenAI Vision API exceeded expectations** - 100% accuracy after prompt refinement
- **Refactoring as we go** - UserService extraction improved code quality immediately

**Challenges Overcome:**
1. **PostgreSQL DateTime timezone issue** - Learned Npgsql requirements, fixed with explicit UTC
2. **Model selection** - Systematic testing revealed gpt-4.1-mini was perfect fit
3. **Invalid image detection** - OpenAI validation proved simpler than OCR
4. **Prompt engineering** - 500+ iterations refined prompt to 100% accuracy
5. **iPhone compatibility** - Avoided filename validation, works across all devices

**Key Metrics Achieved:**
- **Model Accuracy:** 100% extraction success rate (15/15 unique screenshots)
- **Field Accuracy:** 97.06% (battleDate variance acceptable)
- **Response Time:** 5.53s average (acceptable for user experience)
- **Cost per Upload:** $0.0016 (EUR10 = 6,393 uploads)
- **Invalid Image Detection:** Working (successfully rejects mail screens, performance screens)

---

---

## Phase 3: Discord Bot Decisions

### Decision 24: Typed HttpClient for ApiClient

**Chosen:** `AddHttpClient<ApiClient>` (typed HttpClient)
**Alternatives Considered:** `IHttpClientFactory` with named client, static `HttpClient`, manual `new HttpClient()`
**Date:** March 2026

**Reasoning:**
- **DI-friendly** — `ApiClient` is registered as a service, injected wherever needed
- **Encapsulation** — All API communication lives in one class; callers don't deal with URLs or serialization
- **Lifetime managed** — `IHttpClientFactory` handles socket exhaustion internally (avoids DNS stale issue with long-lived clients)
- **Testable** — Can mock `ApiClient` in tests; or inject a custom `HttpMessageHandler` for integration tests

**Configuration pattern:**
```csharp
builder.Services.AddHttpClient<ApiClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Api:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-API-Key", config["Api:ApiKey"]!);
});
```

**Why not IHttpClientFactory with named client:**
- Named clients still require manual `_factory.CreateClient("name")` at every call site
- Typed client gives the same lifecycle benefits with better encapsulation

---

### Decision 25: Options Pattern for Bot Configuration

**Chosen:** `Configure<T>(config.GetSection(...))` with `IOptions<T>` injection
**Alternatives Considered:** Inject `IConfiguration` directly, read config values inline
**Date:** March 2026

**Reasoning:**
- **Strongly typed** — `DiscordBotOptions.Token` vs `config["Bot:Token"]` — no magic strings, compiler-checked
- **Consistent with .NET idioms** — Standard pattern across ASP.NET Core ecosystem
- **Encapsulates config shape** — Options class documents what the section contains
- **Validated at startup** — Can add `ValidateDataAnnotations()` or `ValidateOnStart()` later

**Config split strategy:**
- `appsettings.json` — non-secret values (`BaseUrl`, `Name`) with `SET_IN_USER_SECRETS` placeholders for secret fields
- User secrets — `Bot:Token`, `Api:ApiKey` (never committed)

---

### Decision 26: ApiClient Returns Null on Failure

**Chosen:** Return `null` (or `T?`) when an API call fails; log the failure
**Alternatives Considered:** Throw exceptions on non-2xx, return result wrapper (`Result<T, Error>`)
**Date:** March 2026

**Reasoning:**
- **Bot context** — Discord command handlers need to reply to users gracefully; null is easy to check and handle
- **Simplicity** — Result wrappers add overhead for a project at this stage
- **Special case: 404** — `GetServerConfigAsync` returns `null` on 404 specifically (expected case: server not yet set up)
- **Logging** — Non-success status codes are logged as warnings so issues are traceable

**Trade-off acknowledged:**
- Caller can't distinguish "API returned 400" from "network error" — both return null
- Acceptable now; can add error details to the return type later if needed

---

## Revision History

| Date | Decision | Change | Reason |
|------|----------|--------|--------|
| Jan 20, 2026 | Initial decisions | First draft | Project start |
| Jan 25, 2026 | Decisions 18-23 | Phase 1 completion | Model selection, validation strategy, architecture refinements |
| Jan 25, 2026 | Lessons Learned | Phase 1 documented | Capture learnings while fresh |
| Mar 13, 2026 | Decisions 24-26 | Phase 3 bot decisions | Typed HttpClient, Options pattern, null-on-failure strategy |
