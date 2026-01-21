# ApexGirl Report Analyzer - Database Schema Documentation

**Version:** 1.0  
**Last Updated:** January 2026  
**Database:** PostgreSQL with Entity Framework Core

---

## Overview

This schema supports a battle report analysis system for the mobile game Apex Girl. The system processes screenshot uploads via OpenAI Vision API, extracts structured battle data, and provides analytics while supporting multi-tier access control and Discord integration.

---

## Schema Design Principles

1. **Separation of Concerns**: Upload processing separated from extracted data
2. **Soft Deletes**: Uses `deletedAt` timestamps instead of hard deletes
3. **Version Tracking**: Tracks prompt versions and extraction versions for data migration
4. **Future-Proof Privacy**: Schema ready for server-based privacy controls (MVP defaults to public)
5. **Deduplication**: Image hashing prevents duplicate processing
6. **Audit Trail**: Timestamps and tracking on all entities

---

## Entity Relationship Diagram (Conceptual)

```
┌─────────────┐
│   ApiKey    │ (Service Authentication)
└─────────────┘

┌──────────┐         ┌──────────────┐         ┌────────────┐
│   User   │────────▶│    Upload    │────────▶│BattleReport│
└──────────┘         └──────────────┘         └────────────┘
     │                      │                         │
     │                      │                         ▼
     │                      │                  ┌─────────────┐
     │                      │                  │ BattleSide  │
     │                      │                  │  (Player)   │
     │                      │                  └─────────────┘
     │                      │                         │
     │                      ▼                         ▼
     │               ┌──────────────┐          ┌─────────────┐
     │               │AnalyticsEvent│          │ BattleSide  │
     │               └──────────────┘          │  (Enemy)    │
     │                                         └─────────────┘
     ▼
┌──────────┐         ┌──────────────┐
│ErrorReport│       │DiscordServer│
└──────────┘         └──────────────┘
                            │
                            ▼
                     ┌──────────┐
                     │   Tier   │
                     └──────────┘
                            │
                            ▼
                     ┌──────────┐
                     │TierLimit │
                     └──────────┘
```

---

## Tables

### 1. Authentication & Authorization

#### **ApiKey**
Service-to-service authentication for Discord bot and admin tools.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `key` | STRING | UNIQUE, NOT NULL | Hashed API key |
| `name` | STRING | NOT NULL | Descriptive name (e.g., "Discord Bot Production") |
| `scope` | STRING | NOT NULL | Permission scope (e.g., "bot", "admin") |
| `isActive` | BOOLEAN | NOT NULL, DEFAULT true | Quick enable/disable |
| `createdAt` | DATETIME | NOT NULL | Creation timestamp |
| `expiresAt` | DATETIME | NULLABLE | Optional expiration |
| `revokedAt` | DATETIME | NULLABLE | Revocation timestamp |
| `lastUsedAt` | DATETIME | NULLABLE | Last usage for monitoring |

**Indexes:**
- `key` (unique lookup)
- `isActive, expiresAt` (active key queries)

---

### 2. Core Identity

#### **User**
Represents individual users, primarily identified by Discord ID.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `discordId` | STRING | UNIQUE, NOT NULL | Discord user ID |
| `tierId` | UUID | FK → Tier, NOT NULL | User's tier level |
| `inGamePlayerId` | STRING | NULLABLE | Optional in-game account linking |
| `createdAt` | DATETIME | NOT NULL | Account creation |
| `deletedAt` | DATETIME | NULLABLE | Soft delete timestamp |

**Indexes:**
- `discordId` (lookup)
- `tierId` (tier queries)

**Relationships:**
- 1 User → Many Uploads
- 1 User → Many AnalyticsEvents
- 1 User → Many ErrorReports
- Many Users → 1 Tier

---

### 3. Discord Integration

#### **DiscordServer**
Represents Discord servers (guilds) using the bot.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `discordServerId` | STRING | UNIQUE, NOT NULL | Discord guild ID |
| `ownerDiscordId` | STRING | NOT NULL | Server owner's Discord ID |
| `serverTierId` | UUID | FK → Tier, NULLABLE | Server-level tier (optional) |
| `moderatorRoleIds` | JSON | NOT NULL | Array of Discord role IDs with permissions |
| `defaultReportPrivacy` | ENUM | NOT NULL, DEFAULT 'PUBLIC' | Default privacy for uploads (PUBLIC, SERVER_ONLY) |
| `createdAt` | DATETIME | NOT NULL | Server registration |
| `deletedAt` | DATETIME | NULLABLE | Soft delete timestamp |

**Indexes:**
- `discordServerId` (lookup)
- `serverTierId` (tier queries)

**Relationships:**
- 1 DiscordServer → Many Uploads
- 1 DiscordServer → Many AnalyticsEvents
- Many DiscordServers → 1 Tier

**Notes:**
- `moderatorRoleIds` stored as JSON array for flexibility
- `defaultReportPrivacy` enables future server-level privacy controls (MVP: all PUBLIC)

---

### 4. Tier & Rate Limiting

#### **Tier**
Defines service tiers (Free, Pro, Enterprise, etc.).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `name` | STRING | UNIQUE, NOT NULL | Tier name (e.g., "Free", "Pro") |
| `createdAt` | DATETIME | NOT NULL | Tier creation |

**Relationships:**
- 1 Tier → Many TierLimits
- 1 Tier → Many Users
- 1 Tier → Many DiscordServers

---

#### **TierLimit**
Rate limits associated with tiers, can apply to users or servers.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `tierId` | UUID | FK → Tier, NOT NULL | Associated tier |
| `scope` | ENUM | NOT NULL | Scope of limit (USER, SERVER) |
| `dailyRequestLimit` | INTEGER | NOT NULL | Requests per day |
| `monthlyRequestLimit` | INTEGER | NOT NULL | Requests per month |
| `createdAt` | DATETIME | NOT NULL | Limit creation |

**Indexes:**
- `tierId, scope` (composite lookup)

**Relationships:**
- Many TierLimits → 1 Tier

**Notes:**
- Allows different limits for individual users vs entire servers
- Same tier can have multiple limits with different scopes

---

### 5. Upload Processing

#### **Upload**
Tracks every screenshot upload attempt and processing metadata.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `userId` | UUID | FK → User, NOT NULL | Uploader |
| `discordServerId` | UUID | FK → DiscordServer, NULLABLE | Source Discord server (if applicable) |
| `discordChannelId` | STRING | NULLABLE | Source Discord channel |
| `discordMessageId` | STRING | NULLABLE | Source Discord message |
| `privacyScope` | ENUM | NOT NULL, DEFAULT 'PUBLIC' | Privacy level (PUBLIC, SERVER_ONLY) |
| `imageHash` | STRING | NOT NULL, INDEXED | SHA-256 hash for deduplication |
| `status` | ENUM | NOT NULL | Processing status (PENDING, SUCCESS, FAILED) |
| `failureReason` | STRING | NULLABLE | Error details if FAILED |
| `openAiModel` | STRING | NOT NULL | Model used (e.g., "gpt-4o") |
| `promptVersion` | STRING | NOT NULL | Prompt version for tracking changes |
| `tokenEstimate` | INTEGER | NOT NULL | Estimated tokens used |
| `estimatedCostUsd` | DECIMAL | NULLABLE | Calculated cost in USD |
| `createdAt` | DATETIME | NOT NULL | Upload timestamp |
| `deletedAt` | DATETIME | NULLABLE | Soft delete timestamp |

**Indexes:**
- `userId` (user queries)
- `discordServerId` (server queries)
- `imageHash` (deduplication)
- `status` (processing queue)
- `createdAt` (time-series queries)

**Relationships:**
- Many Uploads → 1 User
- Many Uploads → 1 DiscordServer (optional)
- 1 Upload → 1 BattleReport
- 1 Upload → Many AnalyticsEvents
- 1 Upload → Many ErrorReports

**Notes:**
- `imageHash` prevents reprocessing identical screenshots
- `privacyScope` defaults to PUBLIC (future feature support)
- Discord fields nullable to support non-Discord sources

---

### 6. Battle Data

#### **BattleReport**
Core battle metadata extracted from screenshots.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `uploadId` | UUID | FK → Upload, UNIQUE, NOT NULL | Source upload |
| `extractionVersion` | INTEGER | NOT NULL | Schema version for migrations |
| `battleDate` | DATETIME | NOT NULL | Battle timestamp from screenshot |
| `battleType` | STRING | NOT NULL | Battle type (e.g., "Parking War", "Great Win") |
| `createdAt` | DATETIME | NOT NULL | Extraction timestamp |
| `deletedAt` | DATETIME | NULLABLE | Soft delete timestamp |

**Indexes:**
- `uploadId` (unique constraint)
- `battleDate` (time-series analytics)
- `battleType` (filtering)

**Relationships:**
- 1 BattleReport → 1 Upload
- 1 BattleReport → Many BattleSides (exactly 2: PLAYER + ENEMY)

**Notes:**
- `extractionVersion` critical for schema evolution
- `battleDate` is extracted from screenshot, not upload time

---

#### **BattleSide**
Represents one participant in a battle (player or enemy).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `battleReportId` | UUID | FK → BattleReport, NOT NULL | Parent battle |
| `side` | ENUM | NOT NULL | Participant type (PLAYER, ENEMY) |
| `username` | STRING | NOT NULL | In-game username |
| `groupTag` | STRING | NULLABLE | Alliance tag (e.g., "[ALTR]") |
| `level` | INTEGER | NULLABLE | Player level if visible |
| **Troop Statistics** |
| `fanCount` | INTEGER | NOT NULL | Initial troop count |
| `lossCount` | INTEGER | NOT NULL | Troops lost in battle |
| `injuredCount` | INTEGER | NOT NULL | Troops injured |
| `remainingCount` | INTEGER | NOT NULL | Surviving troops |
| `reinforceCount` | INTEGER | NOT NULL | Reinforcement troops |
| **Attributes** |
| `sing` | INTEGER | NOT NULL | Sing attribute total |
| `dance` | INTEGER | NOT NULL | Dance attribute total |
| **Skills** (stored as basis points, e.g., 172% = 17200) |
| `activeSkill` | INTEGER | NOT NULL | Active skill value |
| `basicAttackBonus` | INTEGER | NOT NULL | Basic attack bonus (basis points) |
| `reduceBasicAttackDamage` | INTEGER | NOT NULL | Damage reduction (basis points) |
| `skillBonus` | INTEGER | NOT NULL | Skill bonus (basis points) |
| `skillReduction` | INTEGER | NOT NULL | Skill reduction (basis points) |
| `extraDamage` | INTEGER | NOT NULL | Extra damage (basis points) |
| **Linking** |
| `inGamePlayerId` | STRING | NULLABLE | Link to User.inGamePlayerId |
| `createdAt` | DATETIME | NOT NULL | Extraction timestamp |

**Indexes:**
- `battleReportId` (parent lookup)
- `side` (filtering)
- `username` (player analytics)
- `groupTag` (alliance analytics)

**Relationships:**
- Many BattleSides → 1 BattleReport

**Notes:**
- Each battle has exactly 2 sides (PLAYER, ENEMY)
- Percentages stored as basis points (17200 = 172%) for precision
- `inGamePlayerId` enables linking battles to registered users

---

### 7. Analytics & Quality

#### **AnalyticsEvent**
Tracks usage, errors, and user-reported issues for analytics.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `userId` | UUID | FK → User, NOT NULL | Associated user |
| `discordServerId` | UUID | FK → DiscordServer, NULLABLE | Associated server |
| `uploadId` | UUID | FK → Upload, NULLABLE | Associated upload |
| `eventType` | ENUM | NOT NULL | Event category (REQUEST_SUCCESS, REQUEST_FAILED, INCORRECT_REPORT) |
| `metadata` | JSON | NULLABLE | Flexible additional context |
| `createdAt` | DATETIME | NOT NULL | Event timestamp |

**Indexes:**
- `eventType` (filtering)
- `createdAt` (time-series analytics)
- `userId` (user analytics)
- `discordServerId` (server analytics)

**Relationships:**
- Many AnalyticsEvents → 1 User
- Many AnalyticsEvents → 1 DiscordServer (optional)
- Many AnalyticsEvents → 1 Upload (optional)

**Notes:**
- `metadata` JSON allows flexible event-specific data
- Powers all analytics dashboards

---

#### **ErrorReport**
User-submitted corrections for incorrect extractions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PK | Unique identifier |
| `uploadId` | UUID | FK → Upload, NOT NULL | Incorrect upload |
| `userId` | UUID | FK → User, NOT NULL | Reporter |
| `reportedIssue` | STRING | NOT NULL | User's description of error |
| `correctedData` | JSON | NULLABLE | Manually corrected data |
| `resolvedAt` | DATETIME | NULLABLE | Resolution timestamp |
| `createdAt` | DATETIME | NOT NULL | Report timestamp |

**Indexes:**
- `uploadId` (lookup)
- `userId` (user reports)
- `resolvedAt` (filtering resolved/unresolved)

**Relationships:**
- Many ErrorReports → 1 Upload
- Many ErrorReports → 1 User

**Notes:**
- Critical for improving prompt engineering
- `correctedData` can store manual fixes for training/validation

---

## Enums

### **UploadStatus**
```
PENDING   - Queued for processing
SUCCESS   - Successfully extracted
FAILED    - Processing failed
```

### **BattleSideType**
```
PLAYER    - The user's side
ENEMY     - The opponent's side
```

### **TierScope**
```
USER      - Limit applies per user
SERVER    - Limit applies per Discord server
```

### **AnalyticsEventType**
```
REQUEST_SUCCESS     - Successful upload/extraction
REQUEST_FAILED      - Failed processing attempt
INCORRECT_REPORT    - User reported incorrect extraction
```

### **PrivacyScope** (Future Feature)
```
PUBLIC        - Visible to all users (MVP default)
SERVER_ONLY   - Visible only to Discord server members
```

---

## Data Storage Considerations

### **Percentages as Basis Points**
Skills stored as integers in basis points (1 basis point = 0.01%):
- **Example:** 172% = 17200 basis points
- **Rationale:** Avoids floating-point precision issues in calculations

### **JSON Columns**
Used for flexible, schema-less data:
- `DiscordServer.moderatorRoleIds` - Discord role IDs array
- `AnalyticsEvent.metadata` - Event-specific context
- `ErrorReport.correctedData` - Flexible correction storage

### **Soft Deletes**
All major entities use `deletedAt` instead of hard deletes:
- Preserves audit trail
- Enables data recovery
- Maintains referential integrity

### **Image Storage**
Screenshots NOT stored in database:
- `Upload.imageHash` for deduplication only
- Actual images stored in cloud storage (S3, Azure Blob, etc.)
- Database stores reference/URL only (future enhancement)

---

## Migration Strategy

### **Version Tracking**
- `BattleReport.extractionVersion` - Tracks data schema changes
- `Upload.promptVersion` - Tracks OpenAI prompt evolution

### **Future Schema Changes**
When changing extracted data structure:
1. Increment `extractionVersion`
2. Add migration logic in code to handle old versions
3. Optionally reprocess old uploads with new extraction logic

---

## Performance Considerations

### **Indexing Strategy**
- All foreign keys indexed
- Time-series columns (`createdAt`) indexed for analytics
- Lookup columns (`discordId`, `imageHash`) indexed
- Composite indexes on common query patterns

### **Partitioning (Future)**
For high volume, consider partitioning:
- `Upload` by `createdAt` (monthly partitions)
- `AnalyticsEvent` by `createdAt` (weekly partitions)

### **Archival Strategy**
- Soft-deleted records archived after 90 days
- Old uploads archived after 1 year (configurable by tier)
- Battle data retained indefinitely for analytics

---

## Privacy & Compliance

### **User Data**
- Minimal PII stored (only Discord ID)
- Soft deletes enable "right to be forgotten" compliance
- `User.deletedAt` marks account as deleted without data loss

### **Report Privacy (Future Feature)**
- `Upload.privacyScope` and `DiscordServer.defaultReportPrivacy` ready
- Implementation requires:
  - Authorization middleware
  - Server membership validation
  - Analytics filtering by privacy scope

---

## Development Phases

### **Phase 1: MVP (Current)**
- ✅ All tables implemented
- ✅ Privacy fields present but default to PUBLIC
- ✅ No permission checks (single user testing)
- ✅ Basic analytics (all data visible)

### **Phase 2: Discord Bot**
- Bot authenticates with ApiKey
- Users auto-created on first upload
- Discord context tracked
- Tier limits enforced

### **Phase 3: Multi-User (Alpha)**
- Permission system activated
- Server-based privacy enforced
- Separate analytics endpoints (global vs server)
- ErrorReport workflow enabled

### **Phase 4: Public Release**
- Rate limiting active
- Tier system fully implemented
- Data archival automated
- Privacy controls in Discord bot UI

---

## Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 2026 | Initial schema design |

---

## Notes for Future Development

### **Planned Additions (Not in MVP)**
- `UserSession` table for web OAuth
- `Alliance` table for formal alliance management
- `AuditLog` table for compliance tracking
- Image storage references in Upload
- Rate limiting cache (Redis)

### **Known Limitations**
- No multi-region support (single database)
- No real-time analytics (batch processing)
- No automated data retention policies (manual for now)

### **Testing Considerations**
- Seed data scripts for tiers
- Factory classes for entity creation
- Test data cleanup strategy
- Mock Discord IDs for integration tests