FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ApexGirlReportAnalyzer.slnx .
COPY ApexGirlReportAnalyzer.Bot/ApexGirlReportAnalyzer.Bot.csproj ApexGirlReportAnalyzer.Bot/
COPY ApexGirlReportAnalyzer.Models/ApexGirlReportAnalyzer.Models.csproj ApexGirlReportAnalyzer.Models/
RUN dotnet restore ApexGirlReportAnalyzer.Bot/ApexGirlReportAnalyzer.Bot.csproj

# Copy source and publish
COPY ApexGirlReportAnalyzer.Bot/ ApexGirlReportAnalyzer.Bot/
COPY ApexGirlReportAnalyzer.Models/ ApexGirlReportAnalyzer.Models/
RUN dotnet publish ApexGirlReportAnalyzer.Bot/ApexGirlReportAnalyzer.Bot.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApexGirlReportAnalyzer.Bot.dll"]
