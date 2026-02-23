# ApexGirl Report Analyzer - Current State

**Last Updated:** February 24, 2026
**Phase:** 3 (Discord Bot) - IN PROGRESS
**Status:** SRP refactoring in progress — API-side controllers still needed

---

## Quick Stats

**Development:**
- Started: January 20, 2026
- Time Invested: ~42-47 hours (estimate)
- Lines of Code: ~4,500 (estimate)
- Database Tables: 10 entities
- API Endpoints: 7 operational (9 when controllers done)
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
| `/api/user/quota/{userId}` | GET | Check remaining uploads |
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

## Recent Session Accomplishments (Feb 23 — evening)

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

### SRP Refactoring — Remaining
- **`GetBattleReportResponseAsync`** in `UploadService` — may be duplicated in `BattleReportService`, investigate
- **`CreateErrorResponse`** in `UploadService` — should move to `UploadResponseHelper` static class

### Not Implemented
- **Controllers** — `UserController` (get-or-create) + `DiscordServerController` (config endpoints) still needed
- **DI registration** — `IDiscordServerService` not yet registered in `Program.cs`
- **Bot project** — scaffold exists but implementation not started
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

---

## Next Steps (Priority Order)

### SRP Refactoring — Finish
1. Investigate `GetBattleReportResponseAsync` — is it duplicated in `BattleReportService`?
2. Create `UploadResponseHelper` and move `CreateErrorResponse` into it

### Phase 3: Discord Bot — Remaining API-side
3. **`UserController`** — add `POST /api/user/get-or-create` endpoint
4. **`DiscordServerController`** — `POST /api/discord-server/config` + `GET /api/discord-server/{id}`
5. **`Program.cs`** — register `IDiscordServerService` in DI

### Phase 3: Bot Project
6. **Bot scaffold** — Configuration classes (`DiscordBotOptions`, `ApiOptions`), `appsettings.json`, user secrets
7. **`ApiClient`** — Typed HttpClient for all API calls
8. **`DiscordBotService`** — BackgroundService managing Discord client lifecycle
9. **`/setup` slash command** — `SetupModule.cs`
10. **Screenshot listener** — `ScreenshotHandler.cs` (core feature)
11. **`/reports` slash command** — `ReportsModule.cs`
12. **Error reporting** — `ErrorReportingService.cs`
13. **Polish** — Caching, rate limiting, graceful shutdown, logging

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
**Next Milestone:** Finish SRP refactoring, then UserController + DiscordServerController + DI registration, then start Bot project
