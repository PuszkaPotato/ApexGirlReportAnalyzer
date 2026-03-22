FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ApexGirlReportAnalyzer.slnx .
COPY ApexGirlReportAnalyzer.API/ApexGirlReportAnalyzer.API.csproj ApexGirlReportAnalyzer.API/
COPY ApexGirlReportAnalyzer.Core/ApexGirlReportAnalyzer.Core.csproj ApexGirlReportAnalyzer.Core/
COPY ApexGirlReportAnalyzer.Infrastructure/ApexGirlReportAnalyzer.Infrastructure.csproj ApexGirlReportAnalyzer.Infrastructure/
COPY ApexGirlReportAnalyzer.Models/ApexGirlReportAnalyzer.Models.csproj ApexGirlReportAnalyzer.Models/
RUN dotnet restore ApexGirlReportAnalyzer.API/ApexGirlReportAnalyzer.API.csproj

# Copy source and publish
COPY ApexGirlReportAnalyzer.API/ ApexGirlReportAnalyzer.API/
COPY ApexGirlReportAnalyzer.Core/ ApexGirlReportAnalyzer.Core/
COPY ApexGirlReportAnalyzer.Infrastructure/ ApexGirlReportAnalyzer.Infrastructure/
COPY ApexGirlReportAnalyzer.Models/ ApexGirlReportAnalyzer.Models/
RUN dotnet publish ApexGirlReportAnalyzer.API/ApexGirlReportAnalyzer.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApexGirlReportAnalyzer.API.dll"]
