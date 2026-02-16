# ApexGirl Report Analyzer - Current State

**Last Updated:** February 16, 2026
**Phase:** 2.5 (API Expansion) - IN PROGRESS
**Status:** Implementing report retrieval endpoints

---

## Quick Stats

**Development:**
- Started: January 20, 2026
- Time Invested: ~30-35 hours (estimate)
- Lines of Code: ~3,500 (estimate)
- Database Tables: 10 entities
- API Endpoints: 5 operational + 1 in progress
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
- `POST /api/upload` - Upload single screenshot for analysis
- `POST /api/upload/batch` - Upload multiple screenshots (max 20)
- `GET /api/user/quota/{userId}` - Check remaining uploads
- `GET /api/status/health` - Health check
- `GET /api/battlereport/battlereport` - Placeholder (returns mock data)

### Core Upload Flow
1. User uploads screenshot via Swagger
2. System validates image (size, type, dimensions)
3. Checks user quota (daily/monthly limits + server quota for Discord)
4. Calculates image hash for deduplication
5. Calls OpenAI Vision API (gpt-4.1-mini)
6. OpenAI validates image is battle report
7. Extracts all battle data (player stats, enemy stats, battle type)
8. Saves to database (Upload, BattleReport, 2 BattleSides)
9. Returns extracted data + remaining quota

### Architecture
- Clean 4-layer separation (API/Core/Infrastructure/Models)
- Dependency injection throughout
- Service layer with business logic
- DTOs for all API communication
- Interfaces for all services
- Validation helper for reusable validation logic

---

## What's In Progress

### BattleReportController (Phase 2.5)
Building the report retrieval endpoint with filtering:

**Files created/modified:**
- `BattleReportController.cs` - Controller with placeholder endpoint
- `IBattleReportService.cs` - Empty interface (needs methods)
- `BattleReportService.cs` - Empty service (needs implementation)
- `BattleReportResponse.cs` - Added `ReportId` and `UploadedAt` fields

**Endpoint design:**
```
GET /api/battlereport/battlereport
  ?participant=string    - Filter by participant name
  ?group=string          - Filter by group tag
  ?inGameId=string       - Filter by in-game ID
  ?battleDate=DateTime   - Filter by battle date
```

**TODO to complete:**
1. Define methods in `IBattleReportService`
2. Implement `BattleReportService` with database queries
3. Wire up DI in `Program.cs`
4. Connect controller to service
5. Add pagination (limit/offset)
6. Add user authorization (whose reports can they see?)

---

## Recent Changes (Since Jan 25)

### Added
- **Batch upload endpoint** - `POST /api/upload/batch` (max 20 images)
- **UploadValidationHelper** - Extracted validation logic from controller
- **BatchUploadResponse DTO** - Response type for batch operations
- **Code standards documentation** - `.claude/docs/code-standards.md`
- **BattleReportController scaffold** - Placeholder for report retrieval
- **BattleReportResponse fields** - Added `ReportId`, `UploadedAt`

### Modified
- **UploadController** - Refactored to use validation helper
- **BattleReportResponse** - Now usable for both OpenAI response and API response

---

## What Doesn't Work Yet

### Not Implemented
- **Report retrieval** - BattleReportService is empty
- **Authentication** - No API keys yet (all endpoints open)
- **Testing** - No unit or integration tests
- **Discord Bot** - No bot yet (Phase 3)
- **Analytics Endpoints** - Stats and aggregations
- **Admin Features** - No admin panel or management
- **Error Reporting** - Users can't report bad extractions
- **Deployment** - Not hosted anywhere (local only)

### Known Limitations
- **Manual user creation** - Must seed users in database
- **No user authorization** - Anyone can query any reports
- **No pagination** - Report queries could return too many results

---

## Code Quality Notes

### Standards Established
See `.claude/docs/code-standards.md` for full details:
- File-scoped namespaces
- Traditional constructors everywhere (controllers and services)
- XML documentation on all public APIs
- Structured logging with templates
- Early return pattern for validation

### Recently Fixed
- All controllers converted to traditional constructors
- BattleReportService converted to traditional constructor + file-scoped namespace
- BattleReportResponse now has full XML docs and proper initializers

---

## Next Steps (Priority Order)

### Immediate - Complete BattleReportService
1. Fix code style issues (namespace, constructor)
2. Define interface methods in `IBattleReportService`
3. Implement query logic with filters
4. Add pagination support
5. Register service in `Program.cs`
6. Wire up controller

### Then - User Authorization
1. Decide: Can users see all reports or only their own?
2. Add userId parameter to queries
3. Implement access control

### Future Phases
- **Phase 3:** Discord Bot
- **Phase 4:** Analytics & Polish
- **Phase 5:** Deployment & Launch

---

## Configuration Required

### appsettings.json (or User Secrets)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=apexgirl_dev;Username=postgres;Password=***"
  },
  "OpenAI": {
    "ApiKey": "sk-***",
    "ApiUrl": "https://api.openai.com/v1/chat/completions",
    "Model": "gpt-4.1-mini",
    "MaxTokens": 1500,
    "PromptVersion": "1.0",
    "PromptPath": "Prompts/BattleAnalysisPrompt.txt"
  }
}
```

### Database
- PostgreSQL 16 running on localhost:5432
- Database: `apexgirl_dev`
- Migrations applied: InitialCreate, AddUploadQuotaIndex
- Seeded data: 3 tiers (Free, Pro, Enterprise), test user

---

## How to Run Locally

1. **Prerequisites:**
   - .NET 10 SDK installed
   - PostgreSQL 16 running
   - OpenAI API key

2. **Database Setup:**
   ```bash
   dotnet ef database update --project ApexGirlReportAnalyzer.Infrastructure --startup-project ApexGirlReportAnalyzer.API
   ```

3. **User Secrets (for API key):**
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-key-here" --project ApexGirlReportAnalyzer.API
   ```

4. **Run:**
   ```bash
   dotnet run --project ApexGirlReportAnalyzer.API
   ```

5. **Test:**
   - Open browser to `https://localhost:7209/swagger`
   - Use seeded test user ID for uploads

---

## Project Info

**Project:** ApexGirl Report Analyzer
**Developer:** Veronica
**Purpose:** Portfolio + Real Use (gaming group)
**Timeline:** January 2026 - March 2026 (estimated)
**Current Phase:** 2.5 - API Expansion
**Next Milestone:** Working report retrieval endpoint
