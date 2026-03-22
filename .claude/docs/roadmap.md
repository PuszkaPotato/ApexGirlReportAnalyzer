# Project Roadmap

This document tracks project phases, timeline, and progress.

---

## Phase 1: Foundation & Core API - COMPLETE

**Duration:** 3 weeks (~20-30 hours) estimated
**Actual:** 5 days (January 20-25, 2026) - AHEAD OF SCHEDULE
**Status:** COMPLETE
**Goal:** Working upload endpoint that processes screenshots and stores data
**Result:** EXCEEDED - Production-ready with 100% extraction accuracy

### Deliverables - ALL COMPLETE

**Core Functionality:**
- Working API that processes screenshots
- 100% extraction accuracy (validated with 100+ test requests)
- Invalid image detection (OpenAI-based validation)
- Deduplication working (SHA-256 hash-based)
- Quota system functional (daily/monthly limits per tier)
- Cost tracking ($0.0016 per upload)

**Technical Implementation:**
- Clean 4-layer architecture (API/Core/Infrastructure/Models)
- 10 database entities with proper relationships
- EF Core migrations tracked and applied
- OpenAI Vision API integration
- Model optimization (gpt-4.1-mini selected)
- Separated controllers (Upload, User, Status)
- UserService extracted for reusability
- Database index for performance
- PostgreSQL timezone handling (UTC enforcement)
- External prompt storage

**Documentation:**
- Technical decisions logged (23 decisions documented)
- Phase 1 lessons learned captured
- Learning objectives updated
- Roadmap adjusted to reality

### Phase 1 Retrospective

**What Worked Well:**
1. **Empirical Testing Approach** - 100+ test requests gave confidence
2. **Clean Architecture** - Made refactoring painless
3. **Iterative Prompt Engineering** - 500 iterations achieved 100% accuracy
4. **YAGNI Principle** - Deferred repository pattern correctly
5. **Fast Iteration** - External prompt file enabled rapid testing

**What Took Longer Than Expected:**
- **Prompt Engineering** - Expected 50 iterations, took 500 (still worth it)
- **Model Testing** - Thorough testing of 3 models added time but saved future costs

**What Went Faster Than Expected:**
- **Database Setup** - Clean schema design, no major issues
- **OpenAI Integration** - Straightforward once prompt was refined
- **Refactoring** - Clean architecture made UserService extraction easy
- **Overall Timeline** - Completed in 5 days vs 3 weeks estimated

---

## Phase 1.5: Documentation & Reflection - COMPLETE

**Duration:** 1-2 days (Jan 25-26, 2026)
**Status:** COMPLETE

**Tasks:**
- Document technical decisions
- Update learning objectives
- Capture lessons learned

---

## Phase 2: Testing & Quality - SKIPPED (deferred)

**Original Estimate:** 2 weeks (15-20 hours)
**Status:** DEFERRED — decided to build Discord bot first, add tests later

---

## Phase 2.5: API Expansion - COMPLETE

**Estimate:** 1 week (8-10 hours)
**Status:** COMPLETE

**Deliverables:**
- BattleReport query endpoint with filtering + pagination
- BattleReport by ID endpoint
- API key authentication (handler, Swagger integration, CLI key generation)
- Code standards established
- Shared code extracted (BattleReportMapper, HashHelper)
- Discord bot API endpoints (server config, user get-or-create)

---

## Phase 3: Discord Bot - IN PROGRESS

**Estimate:** 3 weeks (20-25 hours)
**Status:** IN PROGRESS — API-side prep complete, bot project next
**Goal:** Real users can interact via Discord

**Deliverables:**
- API-side endpoints for bot (server config, user get-or-create) ✓
- Bot project scaffold (Worker Service + Discord.Net) ✓
- Config classes + ApiClient (typed HttpClient for all API calls) ✓
- DiscordBotService (BackgroundService managing Discord client lifecycle)
- /setup slash command (configure upload channel)
- Screenshot listener (core feature — image → API → embed reply)
- /reports slash command (query battle reports)
- Error reporting to dev channel
- Polish (caching, rate limiting, graceful shutdown)

**Detailed plan:** See `.claude/docs/discord-bot-plan.md`

---

## Phase 4: Analytics & Polish

**Estimate:** 2 weeks (15-20 hours)
**Goal:** Analytics, admin features, UX refinement

**Deliverables:**
- Analytics dashboard
- Admin panel
- User management
- UX improvements based on feedback

---

## Phase 5: Deployment & Launch

**Estimate:** 2 weeks (10-15 hours)
**Goal:** Production deployment, monitoring, public launch

**Deliverables:**
- Production deployment (Railway)
- Monitoring and alerting
- Documentation for users
- Public launch

---

## Success Criteria

### Portfolio Goals
- Demonstrates full-stack .NET skills - Yes: API, services, database all working
- Shows understanding of clean architecture - Yes: 3-layer separation implemented
- Includes comprehensive testing - Deferred to Phase 2 as planned
- Has real usage data - Yes: 100+ test requests, validated accuracy
- Deployed and accessible - Not yet: Phase 5
- Well-documented for recruiters - In progress

### Technical Goals
- 70%+ test coverage - Phase 2
- 90%+ extraction accuracy - EXCEEDED: 100% accuracy
- < 10 second response time - EXCEEDED: 5.53s average
- < $50/month operating costs - EXCEEDED: $0.0016/upload = $0.80/month for 500 uploads
- 99% uptime once stable - Not yet deployed

### User Goals (Future)
- 10+ active users in first month - Phase 3: Discord bot
- 50+ battle reports analyzed - Phase 3: Discord bot
- Positive feedback from community - Phase 3: Discord bot
- Feature requests collected - Phase 3: Discord bot

---

## Timeline Summary

**Total Revised Timeline:**
- Original: 12 weeks
- Revised: 8-10 weeks (faster due to Phase 1 efficiency)
- Current Status: Week 1 complete, ahead of schedule

| Phase | Estimated | Actual | Status |
|-------|-----------|--------|--------|
| Phase 1: Foundation | 3 weeks | 5 days | COMPLETE |
| Phase 1.5: Documentation | 2 days | ~2 days | COMPLETE |
| Phase 2: Testing | 1 week | - | DEFERRED |
| Phase 2.5: API Expansion | 1 week | ~3 sessions | COMPLETE |
| Phase 3: Discord Bot | 3 weeks | - | IN PROGRESS |
| Phase 4: Analytics | 2 weeks | - | UPCOMING |
| Phase 5: Deployment | 2 weeks | - | UPCOMING |

---

## Cost Projections

**Production (Estimated 500 uploads/month):**
- 500 uploads x $0.0016 = $0.80/month
- PostgreSQL (Railway) = $5/month (or free tier)
- Hosting (Railway) = $0-5/month (free tier then $5)
- **Total: $5.80-10.80/month**

**At Scale (10,000 uploads/month):**
- 10,000 uploads x $0.0016 = $16/month
- Database + hosting = $10/month
- **Total: $26/month**
