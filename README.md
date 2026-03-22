![LICENSE.md](https://img.shields.io/badge/license-Non--Commercial-blue)

# ApexGirl Report Analyzer

Battle report analysis API + Discord bot for the Apex Girl mobile game.
Analyzes battle report screenshots using OpenAI Vision API and stores results for your group.

## Tech Stack

- .NET 10.0 — ASP.NET Core Web API + Discord bot (Discord.Net)
- Entity Framework Core + PostgreSQL
- OpenAI Vision API (gpt-4.1-mini)
- Docker + Docker Compose

---

## Deployment

### Prerequisites

- A server with Docker and Docker Compose installed
- A Discord bot token ([Discord Developer Portal](https://discord.com/developers/applications))
- An OpenAI API key
- Your Discord user ID (enable Developer Mode → right-click your name → Copy User ID)

### 1. Get the files onto your server

Copy `docker-compose.yml` and `.env.example` to a directory on your server:

```bash
scp docker-compose.yml .env.example user@your-server:~/apexgirl/
```

### 2. Configure secrets

```bash
cd ~/apexgirl
cp .env.example .env
nano .env   # fill in all values except API_KEY — you'll generate that next
```

### 3. Start the database and API

```bash
docker compose pull
docker compose up -d db api
```

Wait about 30 seconds for the API to start and run migrations.

### 4. Generate the bot API key

```bash
docker compose run --rm api dotnet ApexGirlReportAnalyzer.API.dll \
  --generate-apikey --name "Discord Bot"
```

Copy the printed key (starts with `agra_`) and add it to `.env`:

```bash
nano .env   # set API_KEY=agra_...
```

### 5. Start the bot

```bash
docker compose up -d
```

All three services (db, api, bot) are now running.

### 6. Configure your Discord server

In your Discord server, run:

```
/setup init upload-channel:#your-channel
```

### Updating

```bash
docker compose pull
docker compose up -d
```

Migrations run automatically on API startup.

---

## Local Development

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL
- An OpenAI API key

### Setup

```bash
# Set required secrets
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project ApexGirlReportAnalyzer.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=apexgirl_dev;Username=postgres;Password=..." --project ApexGirlReportAnalyzer.API

# Run migrations
dotnet ef database update --project ApexGirlReportAnalyzer.Infrastructure --startup-project ApexGirlReportAnalyzer.API

# Start the API (Swagger UI at http://localhost:5057/swagger)
dotnet run --project ApexGirlReportAnalyzer.API

# Generate a local API key for the bot
dotnet run --project ApexGirlReportAnalyzer.API -- --generate-apikey --name "Discord Bot"

# Set bot secrets
dotnet user-secrets set "Bot:Token" "..." --project ApexGirlReportAnalyzer.Bot
dotnet user-secrets set "Api:ApiKey" "agra_..." --project ApexGirlReportAnalyzer.Bot
dotnet user-secrets set "Bot:DeveloperId" "your-discord-id" --project ApexGirlReportAnalyzer.Bot

# Start the bot
dotnet run --project ApexGirlReportAnalyzer.Bot
```

---

## License

This project is source-available under a custom non-commercial license.

You are free to view, study, and use this software for personal and educational
purposes. Commercial use, redistribution, or offering this software as a
service (including APIs or SaaS) is not permitted without explicit permission
from the author.

This project is intended for learning, contributions, and portfolio review.

## Contributing

Contributions are welcome!

This project is open for learning, experimentation, and collaboration.
If you'd like to contribute, please read the
[CONTRIBUTING.md](./CONTRIBUTING.md) file first.

By contributing, you agree that your contributions may be used and relicensed
by the project owner, including for commercial purposes.
