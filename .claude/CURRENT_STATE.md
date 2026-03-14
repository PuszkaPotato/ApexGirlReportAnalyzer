# ApexGirl Report Analyzer - Current State

**Last Updated:** March 14, 2026
**Phase:** 3 (Discord Bot) - IN PROGRESS
**Status:** Screenshot handler done, tier slash command done — tier modal is next on `feature/discord-bot` branch

---

## Quick Stats

**Development:**
- Started: January 20, 2026
- Time Invested: ~42-47 hours (estimate)
- Lines of Code: ~4,500 (estimate)
- Database Tables: 10 entities
- API Endpoints: 9 operational
- Test Coverage: 0% (deferred)

**Technical Performance:**
- Model: gpt-4.1-mini
- Extraction Accuracy: 100% (15/15 test screenshots)
- Field Accuracy: 97.06% (battleDate variance acceptable)
- Response Time: 5.53s average
- Cost per Upload: $0.0016 (~EUR1.50 per 1000 uploads)
- Invalid Image Detection: Working

---

## What Works Right Now

### Working Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/upload` | POST | Upload single screenshot for analysis |
| `/api/upload/batch` | POST | Upload multiple screenshots (max 20) |
| `/api/battlereport/battlereport` | GET | Query reports with filters + pagination |
| `/api/battlereport/battlereport/{id}` | GET | Get single report by ID |
| `/api/user/get-or-create` | POST | Get or create user by Discord ID |
| `/api/user/quota/{userId}` | GET | Check remaining uploads |
| `/api/discordserver/config` | POST | Set or update Discord server config |
| `/api/discordserver/{discordServerId}` | GET | Get Discord server config |
| `/api/status/health` | GET | Health check |

### BattleReport Query Features
- Filter by: uploadId, battleDate, battleType, userId, participant, inGameId, groupTag
- Pagination: limit (default 10) and offset
- Returns: TotalCount, Count, FiltersApplied, BattleReports array
- Ordering: Most recent first (CreatedAt descending)

### Architecture
- Clean 4-layer separation (API/Core/Infrastructure/Models)
- Dependency injection throughout
- Service layer with business logic
- DTOs for all API communication
- Interfaces for all services
- Shared mappers and helpers extracted

---

## Recent Session Accomplishments (Mar 13)

### Bot Foundation — Completed
- Created `Configuration/DiscordBotOptions.cs` and `Configuration/ApiOptions.cs` (Options pattern, wired with `Configure<T>`)
- Created `Http/ApiClient.cs` — typed HttpClient covering all API endpoints the bot needs:
  - `GetOrCreateUserAsync` — `POST /api/user/get-or-create`
  - `GetUserQuotaAsync` — `GET /api/user/quota/{userId}`
  - `UploadScreenshotAsync` — `POST /api/upload` (multipart/form-data)
  - `GetServerConfigAsync` — `GET /api/discordserver/{id}` (returns null on 404)
  - `SetServerConfigAsync` — `POST /api/discordserver/config`
  - `GetBattleReportsAsync` — `GET /api/battlereport` (with filters + pagination)
- Updated `Program.cs` to register options and `ApiClient` typed HttpClient with base URL + API key header
- Updated `appsettings.json` with `Bot:Token` and `Api:ApiKey` placeholder entries pointing to user secrets
- Clean build, zero warnings

### Permissions
- Added `Edit` and `Write` to `.claude/settings.local.json` — no longer prompts for file operations

---

## Previous Session Accomplishments (Mar 1)

### Tier Management — Completed
- Built full `ITierService`, `TierService`, `TierMapper`
- Created `CreateTierRequest`, `UpdateTierRequest`, `TierLimitRequest` DTOs
- Added `DeleteTierResult` enum for 404/409 differentiation
- Added `MigrateTierAssigneesAsync` for moving users/servers between tiers
- Refactored `TiersController` from direct `AppDbContext` to service layer
- Registered `ITierService` in `Program.cs`

### XML Documentation
- Enabled `GenerateDocumentationFile` in API `.csproj`
- Added `IncludeXmlComments` to Swagger
- Added `/// <summary>` and `/// <inheritdoc />` to all controllers and helpers
- Fixed CS1587 (doc comments placed after attributes instead of before)
- Fixed CS1573 (missing `<param>` tags for `limit`, `offset`, `cancellationToken`)
- Zero CS1591/CS1573/CS1587 warnings remaining

### UploadController cleanup
- Removed `#if DEBUG` compile-time error helpers
- Replaced with consistent `StatusCode(500, ...)` + generic message pattern
- Logger already captures full exception detail — no need to leak it to clients

### Bot project prep
- Created `feature/discord-bot` branch
- Discord.Net 3.18.0, `Microsoft.Extensions.Http`, Models reference already in place
- User secrets ID already configured in `.csproj`
- Clarified config split: `BaseUrl` → `appsettings.json`, `ApiKey`/`BotToken` → user secrets

---

## Previous Session Accomplishments (Feb 23 — evening)

### SRP Refactoring (based on senior dev feedback)
- **Removed `AnalyzeWithOpenAIAsync` wrapper** — `UploadService` now calls `_openAIService.AnalyzeScreenshotAsync` directly
- **Moved `UserExistsAsync`** from `UploadService` → `UserService` (added to `IUserService` interface)
- **Removed `ParseExtractionVersion`** — `ExtractionVersion` field removed from `BattleReport` entity entirely
- **Cleaned up inline section comments** — removed "// Step X:" style comments from `ProcessUploadAsync`
- **Fixed null safety** — upload null check before `MarkUploadAsFailedAsync` in catch block

### DiscordServerId type fix
- Changed `Upload.DiscordServerId` from `Guid?` to `string?`
- Removed `ParseDiscordServerId` method — was pointlessly converting string to Guid
- Removed `DiscordServer` navigation property from `Upload` (FK relationship was incompatible)
- Removed `Uploads` collection from `DiscordServer`
- Updated `UserService.ValidateServerQuotaAsync` to query by `DiscordServerId` string

### Prompt file restructure
- Replaced `BattleAnalysisPrompt.txt` with two files: `BattleAnalysis.json` (version + promptFile ref) + `BattleAnalysis.txt` (prompt text)
- `OpenAIService.GetPrompt()` now returns `PromptConfig` record with both `Prompt` and `Version`
- `PromptVersion` now flows through `BattleReportResponse` and saved to `Upload` in `SaveBattleReportAsync`
- Removed `OpenAI:PromptVersion` and `OpenAI:PromptPath` from config — replaced with `OpenAI:PromptName`
- Added `<Content CopyToOutputDirectory>` for Prompts folder in `.csproj`

### Migrations reset (twice)
- Wiped all migrations and recreated `InitialCreate` fresh with correct schema

### Previous Sessions
- CLI API Key Generation (`--generate-apikey` command)
- Code standards established
- Extracted shared code: `BattleReportMapper`, `HashHelper`
- Built `BattleReportService` and `BattleReportController`
- Upload passthrough: `discordChannelId`/`discordMessageId` through full pipeline
- DTOs, Services, EF Migration for Discord bot API-side prep

---

## What Doesn't Work Yet

### Not Implemented
- **Bot project** — scaffold exists, configuration classes next
- **Testing** - No unit or integration tests
- **Analytics Endpoints** - Stats and aggregations
- **Admin Features** - No admin panel
- **Error Reporting** - Users can't report bad extractions
- **Deployment** - Not hosted anywhere (local only)

### Known Limitations
- No rate limiting beyond quota system
- No caching

---

## Code Quality Notes

### Standards Established
See `.claude/docs/code-standards.md`:
- File-scoped namespaces
- Traditional constructors everywhere
- XML documentation on public APIs
- Structured logging with templates
- Early return pattern for validation
- No inline section comments — extract to methods or rely on self-documenting code

### Shared Code
- `Infrastructure/Mappers/BattleReportMapper.cs` - Entity ↔ DTO
- `Infrastructure/Mappers/UserMapper.cs` - User entity ↔ UserResponse
- `Infrastructure/Mappers/DiscordServerMapper.cs` - DiscordServer entity ↔ DiscordServerConfigResponse
- `Infrastructure/Helpers/HashHelper.cs` - SHA-256 hashing
- `Infrastructure/Mappers/TierMapper.cs` - Tier entity ↔ TierResponse

---

## Next Steps (Priority Order)

### Phase 3: Bot Project (branch: `feature/discord-bot`)
1. ~~**Config classes** — `DiscordBotOptions`, `ApiOptions`; wire up `appsettings.json` + user secrets~~ ✓ Done
2. ~~**`ApiClient`** — Typed HttpClient for all API calls~~ ✓ Done
3. ~~**`DiscordBotService`** — BackgroundService managing Discord client lifecycle~~ ✓ Done
4. ~~**`/setup` slash commands** — `SetupModule.cs` (`init`, `view`, `update`)~~ ✓ Done
5. ~~**`/reports` slash command** — `ReportsModule.cs`~~ ✓ Done
6. ~~**Screenshot handler** — `ScreenshotHandler.cs` (core feature)~~ ✓ Done
7. ~~**`/assign-tier` slash command** — `TierModule.cs`~~ ✓ Done
8. **Tier assignment modal** — `TierModalModule.cs` ← **NEXT**
9. **Polish** — Graceful shutdown, logging

### Before Pre-Alpha Testing
- **CSV Export** — export battle reports to CSV

### Future Phases
- **Phase 4:** Analytics & Polish
- **Phase 5:** Deployment & Launch

---

## Project Info

**Project:** ApexGirl Report Analyzer
**Developer:** Veronica
**Purpose:** Portfolio + Real Use (gaming group)
**Timeline:** January 2026 - March 2026 (estimated)
**Current Phase:** 3 In Progress
**Next Milestone:** Bot project implementation
