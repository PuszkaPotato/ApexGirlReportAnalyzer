# Discord Bot Implementation Plan (Phase 3)

**Created:** February 17, 2026
**Status:** Phase 1 complete (API-side prep), Phase 2+ remaining

---

## Architecture Overview

New project `ApexGirlReportAnalyzer.Bot` in the same solution. The bot is an **API client** — it talks to the API over HTTP using a generated API key. No direct database access. References the Models project to share DTOs.

```
Discord User drops screenshot
    → Bot receives MessageReceived event
    → Bot downloads image, calls API (POST /api/upload)
    → API processes with OpenAI, returns UploadResponse
    → Bot builds rich embed from response, replies in channel
```

---

## Phase 1: API-Side Preparation ✅ COMPLETE

All changes listed in CURRENT_STATE.md. Summary:
- DiscordServer entity: +UploadChannelId, AllowedRoleId, LogChannelId
- 4 new DTOs, 1 new service (DiscordServerService), 3 new endpoints
- UserService: GetOrCreateByDiscordIdAsync
- Upload pipeline: +discordChannelId/discordMessageId passthrough
- **Still need:** EF migration (`dotnet ef migrations add AddDiscordServerBotFields ...`)

---

## Phase 2: Bot Project Scaffold

### 2.1 — Create the project
- `dotnet new worker -n ApexGirlReportAnalyzer.Bot`
- Add to `.slnx`
- Add project reference to `ApexGirlReportAnalyzer.Models`
- NuGet packages: `Discord.Net` (v3.x), `Microsoft.Extensions.Http`

### 2.2 — Configuration classes
- `Bot/Configuration/DiscordBotOptions.cs` — BotToken, DevErrorChannelId
- `Bot/Configuration/ApiOptions.cs` — BaseUrl, ApiKey
- `Bot/appsettings.json` — structure with placeholders, real values via `dotnet user-secrets`

### 2.3 — ApiClient (typed HttpClient)
- `Bot/Services/ApiClient.cs`
- Configured with base URL + `X-API-Key` default request header
- Methods matching API endpoints:
  - `GetOrCreateUserAsync(string discordId)` → POST /api/user/get-or-create
  - `RegisterServerAsync(DiscordServerConfigRequest)` → POST /api/discord-server/config
  - `GetServerConfigAsync(string discordServerId)` → GET /api/discord-server/{id}
  - `UploadScreenshotAsync(...)` → POST /api/upload (multipart form)
  - `GetBattleReportsAsync(...)` → GET /api/battlereport

### 2.4 — DiscordBotService (BackgroundService)
- `Bot/Services/DiscordBotService.cs`
- Manages Discord client lifecycle (LoginAsync, StartAsync, StopAsync)
- Wires events: `Ready`, `InteractionCreated`, `MessageReceived`
- Registers slash commands on `Ready`
- Gateway intents: `Guilds | GuildMessages | MessageContent`

### 2.5 — Program.cs (host builder)
- Configure options from appsettings/user-secrets
- Register HttpClient for ApiClient
- Register DiscordSocketClient as singleton
- Register InteractionService
- Register DiscordBotService as hosted service

**Verify:** Build + run bot → it connects and shows online in Discord.

---

## Phase 3: /setup Slash Command

- `Bot/Modules/SetupModule.cs`
- Command: `/setup channel:#upload-here role:@Members` (role is optional, default = everyone)
- Requires `GuildPermission.Administrator`
- Calls `POST /api/discord-server/config` via ApiClient
- Replies with ephemeral embed confirming the configuration

**Verify:** Use `/setup` in a server → DB record created, bot confirms.

---

## Phase 4: Screenshot Listener (Core Feature)

### 4.1 — ScreenshotHandler
- `Bot/Services/ScreenshotHandler.cs`
- Flow:
  1. Ignore bot messages
  2. Check if channel is configured for uploads (GetServerConfigAsync, with in-memory cache)
  3. Check user has allowed role (if role is set on server config)
  4. Filter message attachments to images only
  5. Get-or-create user by Discord ID
  6. For each image attachment:
     - Add ⏳ reaction to message
     - Download image bytes
     - Call upload API (multipart form with image file)
     - On success: reply with rich embed, remove ⏳ reaction
     - On error: reply with error message, remove ⏳ reaction

### 4.2 — EmbedBuilderHelper
- `Bot/Helpers/EmbedBuilderHelper.cs`
- `BuildBattleReportEmbed(UploadResponse)`:
  - Title: battle type + date
  - Color: green for win, red for loss (compare fan counts)
  - Fields: player stats, enemy stats, skills breakdown
  - Footer: quota remaining
- `BuildReportListEmbed(BattleReportListResponse)`:
  - Compact multi-report summary for /reports command

### 4.3 — Wire into DiscordBotService
- `MessageReceived` event → create DI scope → resolve ScreenshotHandler → HandleAsync

**Verify:** Drop screenshot in configured channel → rich embed reply with full battle data.

---

## Phase 5: /reports Slash Command

- `Bot/Modules/ReportsModule.cs`
- Command: `/reports type:Arena date:2026-02-17 opponent:PlayerName limit:5`
- All params optional, default limit 5
- Gets user by Discord ID → calls GET /api/battlereport with filters
- Replies with compact report list embed

**Verify:** `/reports` after some uploads → report list displayed.

---

## Phase 6: Error Reporting

- `Bot/Services/ErrorReportingService.cs`
- Sends error embeds to a configured dev channel (DevErrorChannelId from config)
- Called from catch blocks in ScreenshotHandler and interaction modules
- Embed includes: exception type/message, guild/channel/user context, timestamp

**Verify:** Break API URL → drop screenshot → error appears in dev channel.

---

## Phase 7: Polish

- **Server config caching:** IMemoryCache with 5min TTL, invalidated on /setup
- **Rate limiting:** 1 upload per 3 seconds per user (ConcurrentDictionary with timestamps)
- **Graceful shutdown:** Properly disconnect Discord client in StopAsync
- **Logging integration:** Discord.Net Log event → ILogger bridge

---

## File Summary

### New files (Bot project — 12 files):
| File | Purpose |
|------|---------|
| `Bot/Bot.csproj` | Worker SDK project file |
| `Bot/Program.cs` | Host builder, DI setup |
| `Bot/appsettings.json` | Config structure |
| `Bot/Configuration/DiscordBotOptions.cs` | Bot token, dev channel |
| `Bot/Configuration/ApiOptions.cs` | API base URL, API key |
| `Bot/Services/DiscordBotService.cs` | Bot lifecycle (BackgroundService) |
| `Bot/Services/ApiClient.cs` | Typed HTTP client for API |
| `Bot/Services/ScreenshotHandler.cs` | Message → upload → embed flow |
| `Bot/Services/ErrorReportingService.cs` | Dev error channel reporting |
| `Bot/Modules/SetupModule.cs` | /setup slash command |
| `Bot/Modules/ReportsModule.cs` | /reports slash command |
| `Bot/Helpers/EmbedBuilderHelper.cs` | Discord embed builders |

### Key concepts to learn:
- **Worker Service** (`dotnet new worker`) — long-running background process
- **BackgroundService** — base class for hosted services in .NET
- **Discord.Net** — DiscordSocketClient, InteractionService, slash commands, embeds
- **Typed HttpClient** — DI-friendly HTTP client with base address and default headers
- **IMemoryCache** — simple in-process caching
