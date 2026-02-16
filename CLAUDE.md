# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project State & Memory

**Before starting work, read `.claude/CURRENT_STATE.md`** - this is the project's memory file containing:
- What has been built and what works
- Current phase and status
- What's next (priority order)
- Known limitations and blockers

**Update CURRENT_STATE.md** when completing significant work (new features, phase completions, architectural changes).

**Additional documentation in `.claude/docs/`:**
- `code-standards.md` - **Read before writing code** - naming, patterns, conventions
- `roadmap.md` - Project phases and timeline
- `tech-decisions.md` - Technical decisions with rationale
- `learning-progress.md` - Skills learned and interview prep

## Project Overview

ApexGirl Report Analyzer - Battle report analysis API for Apex Girl mobile game using OpenAI Vision API.

**Tech Stack:** .NET 10.0, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, OpenAI API

## Build and Run Commands

```bash
# Build
dotnet build ApexGirlReportAnalyzer.slnx

# Run (starts at localhost:5057, Swagger UI at /swagger)
dotnet run --project ApexGirlReportAnalyzer.API

# Database migrations
dotnet ef database update --project ApexGirlReportAnalyzer.Infrastructure --startup-project ApexGirlReportAnalyzer.API

# Add new migration
dotnet ef migrations add [MigrationName] --project ApexGirlReportAnalyzer.Infrastructure --startup-project ApexGirlReportAnalyzer.API

# Set OpenAI API key (required for upload functionality)
dotnet user-secrets set "OpenAI:ApiKey" "your-key" --project ApexGirlReportAnalyzer.API
```

## Architecture

The project follows Clean Architecture with four layers:

```
ApexGirlReportAnalyzer.API          → Controllers, Helpers, Prompts (entry point)
ApexGirlReportAnalyzer.Core         → Service interfaces (contracts)
ApexGirlReportAnalyzer.Infrastructure → Services, DbContext, Migrations (implementation)
ApexGirlReportAnalyzer.Models       → DTOs and Entities
```

**Key patterns:**
- Services are registered via DI in `Program.cs` (scoped lifetime)
- All entities use soft deletes via `DeletedAt` field with EF Core query filters
- Percentage values stored as basis points (172% = 17200)
- Image deduplication via SHA-256 hashing

## Core Data Flow

Upload processing pipeline (`UploadController` → `UploadService` → `OpenAIService`):
1. Validate request (userId, image format/size)
2. Check quota (user-level and server-level for Discord)
3. Calculate SHA-256 hash, check for duplicates
4. Call OpenAI Vision API with prompt from `Prompts/BattleAnalysisPrompt.txt`
5. Parse response, save `BattleReport` + `BattleSides`
6. Return `UploadResponse` with extracted data and quota info

## Configuration

- Connection string: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- OpenAI settings: `appsettings.json` → `OpenAI:*` (API key via User Secrets)
- Database seeding: Set `Seed:WipeDatabase: true` in `appsettings.Development.json` to reset

## Database

PostgreSQL with EF Core. Key entities: `User`, `Upload`, `BattleReport`, `BattleSide`, `Tier`, `TierLimit`, `DiscordServer`, `ApiKey`.

See `database-schema.md` for comprehensive schema documentation.

## Key Technical Decisions

- **OpenAI Model:** gpt-4.1-mini (100% accuracy, 5x cheaper than gpt-4.1, 5.53s response time)
- **Image validation:** OpenAI-based detection (not OCR) - simpler and works across all devices
- **DateTime handling:** Always use `DateTime.SpecifyKind(..., DateTimeKind.Utc)` for PostgreSQL queries
- **Prompt storage:** External file at `Prompts/BattleAnalysisPrompt.txt` for fast iteration
