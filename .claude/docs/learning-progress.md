# Learning Objectives & Progress

This document tracks skills learned and interview preparation progress throughout the project.

---

## Core .NET Skills

### C# Language Features
- [x] **Nullable reference types** - Used extensively (`string?`, `Guid?`, `int?` in DTOs and entities)
- [x] **Records and init properties** - Used `private record PromptConfig(string Prompt, string Version)` in OpenAIService for internal data bundling
- [ ] **Pattern matching** - Limited use (some `is` checks for null)
- [x] **Async/await** - Used throughout (all service methods, database calls, OpenAI calls)
- [x] **LINQ** - Extensive use (EF Core queries, filtering, Include/ThenInclude)
- [x] **Dependency injection** - Core pattern (services injected in controllers and services)
- [x] **Interfaces and abstractions** - Created IOpenAIService, IUploadService, IUserService

### ASP.NET Core Web API
- [x] **Project structure** - Clean 4-project architecture (API/Core/Infrastructure/Models)
- [x] **Controllers** - Built UploadController, UserController, StatusController
- [ ] **Middleware** - Haven't created custom middleware yet
- [x] **Dependency injection** - Registered services in Program.cs (Scoped, Singleton)
- [x] **Configuration** - Used appsettings.json, user secrets for API keys
- [x] **OpenAPI/Swagger** - Automatic documentation, tested extensively
- [x] **Error handling** - Try-catch blocks, proper HTTP status codes (200, 400, 429)
- [x] **Validation** - Input validation (file size, type, user ID)

### Entity Framework Core
- [x] **DbContext** - Created AppDbContext with 10 entities
- [x] **Code-First approach** - Defined entities, EF generated database
- [x] **Migrations** - Created InitialCreate, AddUploadQuotaIndex migrations
- [x] **Fluent API** - Configured relationships, indexes in OnModelCreating
- [x] **Relationships** - One-to-many (User->Uploads), One-to-one (Upload->BattleReport)
- [x] **Querying** - LINQ with Include/ThenInclude, Where, Count, FirstOrDefault
- [ ] **Change tracking** - Used default tracking (haven't optimized with AsNoTracking)
- [x] **Transactions** - Implicit transactions in SaveChangesAsync
- [x] **Performance** - Added composite index for quota queries

---

## Database Design Skills

### Relational Database Concepts
- [x] **Normalization** - 3NF design (no data duplication, proper relationships)
- [x] **Foreign keys** - All relationships have FK constraints
- [x] **Indexes** - Created composite index (UserId, Status, CreatedAt)
- [x] **Constraints** - Unique indexes on DiscordId, ImageHash
- [x] **Soft deletes** - DeletedAt column on all major entities
- [x] **Audit columns** - CreatedAt on all entities (BaseEntity pattern)

### PostgreSQL Specific
- [x] **JSON columns** - Used for ModeratorRoleIds (array), Metadata (flexible data)
- [x] **GUID/UUID** - All primary keys are GUIDs
- [x] **Indexes** - B-tree composite index for performance
- [x] **Query optimization** - Learned about timezone handling (Npgsql requirements)

---

## Architecture & Design Patterns

### Clean Architecture
- [x] **Separation of concerns** - API (controllers) -> Core (interfaces) -> Infrastructure (implementations)
- [x] **Dependency inversion** - Services depend on interfaces (IUserService, IOpenAIService)
- [x] **Domain-driven design basics** - Entities represent domain concepts (Upload, BattleReport)
- [ ] **Repository pattern** - Deferred (using DbContext directly)
- [x] **Service layer** - Business logic in services (UploadService, UserService)

### SOLID Principles
- [x] **S - Single Responsibility** - Each service has one job (UserService = users, UploadService = uploads)
- [x] **O - Open/Closed** - Can extend via new services without modifying existing
- [x] **L - Liskov Substitution** - Interfaces are contracts (can swap implementations)
- [x] **I - Interface Segregation** - Small focused interfaces (IUserService has 3 methods)
- [x] **D - Dependency Inversion** - Depend on IUserService, not UserService concrete class

---

## External Integrations

### HTTP/REST APIs
- [x] **RESTful principles** - Resource-based URLs (/api/upload, /api/user)
- [x] **HTTP status codes** - 200 (success), 400 (bad request), 429 (rate limit), 500 (error)
- [x] **Request/response patterns** - DTOs for all requests/responses
- [x] **Authentication** - API key planned (not yet implemented)
- [x] **Rate limiting** - Quota system (daily/monthly limits per tier)

### OpenAI API Integration
- [x] **Vision API** - Used gpt-4.1-mini for image analysis
- [x] **Prompt engineering** - Refined prompt over 500 iterations to 100% accuracy
- [x] **Structured outputs** - Consistent JSON responses with validation
- [x] **Cost management** - Token counting, cost calculation per request
- [x] **Error handling** - Try-catch, invalid image detection, graceful failures
- [ ] **Rate limit handling** - OpenAI hasn't rate-limited yet (may add exponential backoff later)

---

## Phase 1 Skills Self-Assessment

**Rate yourself (1-5):**

**Before Project:**
- C# fundamentals: 2/5 (PHP background, learning C#)
- ASP.NET Core: 1/5 (never used)
- Entity Framework: 1/5 (never used)
- Testing: 2/5 (basic knowledge)
- Architecture: 2/5 (understood concepts, never applied)

**After Phase 1:**
- C# fundamentals: 4/5 (comfortable with async/await, LINQ, DI)
- ASP.NET Core: 4/5 (built working API, understand pipeline)
- Entity Framework: 4/5 (migrations, relationships, queries working)
- Testing: 2/5 (no tests written yet - Phase 2)
- Architecture: 4/5 (clean architecture implemented and understood)

**Confidence Level:**
- Can build a new .NET API from scratch: Yes
- Can explain architecture decisions: Yes
- Can debug EF Core issues: Yes (learned timezone handling)
- Can integrate external APIs: Yes (OpenAI integration working)
- Ready for interviews: Yes (have concrete examples)

---

## Interview Story Template

**The Problem:**
"As an Apex Girl player, I needed to analyze battle reports to improve strategy. Manual tracking was tedious and error-prone."

**The Solution:**
"I built a .NET API that uses OpenAI Vision to automatically extract and store battle data from screenshots. After testing 100+ requests across multiple models, I chose gpt-4.1-mini which achieved 100% accuracy while being 5x cheaper than gpt-4.1."

**The Architecture:**
"I used clean 3-layer architecture: API for HTTP, Core for interfaces, Infrastructure for implementations. This separation made refactoring painless - when I needed to extract UserService from UploadService, it took minutes not hours."

**The Challenges:**
"The biggest challenge was PostgreSQL timezone handling. Npgsql requires explicit UTC DateTimes, but .Date strips the Kind. I debugged the error, understood the requirement, and now explicitly specify DateTimeKind.Utc. This taught me the importance of understanding your dependencies."

**The Results:**
"The application achieves 100% extraction accuracy, processes screenshots in 5.5 seconds, and costs $0.0016 per upload. I validated this through 100+ test requests. The invalid image detection works across Android and iPhone screenshots without filename dependencies."

**What I Learned:**
"This project taught me that empirical testing beats assumptions. I tested 3 OpenAI models systematically and saved 80% on API costs by choosing the right one. I also learned that starting with clean architecture makes future changes easy - the UserService extraction was trivial because dependencies were already abstracted."

---

## Interview Questions You Can Now Answer

**C# & .NET:**
- "Explain the difference between `string?` and `string!`" - Used `?` for nullable fields in DTOs, `!` for EF Core navigation properties
- "When would you use async/await vs Task.Result?" - All database and API calls are async to avoid blocking threads
- "What's the benefit of dependency injection?" - Makes code testable, allows swapping implementations, manages lifetimes

**ASP.NET Core:**
- "Walk me through the request pipeline in ASP.NET Core" - Request -> Controller -> Service Layer -> Database/OpenAI -> Response
- "How do you handle errors globally in an API?" - Try-catch in services, return appropriate HTTP status codes, log errors
- "Explain the difference between Scoped, Transient, and Singleton services" - Scoped = per request (DbContext), Transient = per injection, Singleton = app lifetime

**Entity Framework:**
- "How do you handle relationships in EF Core?" - Navigation properties + foreign keys, Include for eager loading
- "What's the difference between Add and Update in EF Core?" - Add for new entities, Update for existing, EF tracks changes automatically
- "How do migrations work?" - Code-first: Change entities -> Create migration -> Apply to database

**Database:**
- "Why did you choose GUIDs over auto-incrementing integers?" - Distributed-friendly, non-sequential (security), no conflicts, can generate client-side
- "Explain your approach to soft deletes" - DeletedAt column, global query filter (WHERE DeletedAt IS NULL), preserves data
- "How did you handle the JSON data in your schema?" - PostgreSQL JSON columns for flexible data (ModeratorRoleIds, Metadata)

**Architecture:**
- "Walk me through your project's architecture" - 3-layer: API -> Core (interfaces) -> Infrastructure (implementations). Models shared.
- "Why did you separate Core from Infrastructure?" - Core defines contracts, Infrastructure implements. Can swap database/API implementations.
- "Give me an example of where you applied SOLID principles" - Extracted UserService (SRP), used interfaces (DIP), small focused contracts (ISP)

**OpenAI Integration:**
- "How did you integrate with OpenAI's API?" - HttpClient with Bearer token, POST to /v1/chat/completions, parse JSON response
- "Explain your prompt engineering process" - 500+ iterations testing different prompts, achieved 100% extraction accuracy
- "How do you handle API rate limits?" - Tier-based quota system (daily/monthly), return HTTP 429 when exceeded
