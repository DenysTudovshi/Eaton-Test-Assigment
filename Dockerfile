FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY src/ ./src/
COPY data/ ./data/
RUN dotnet publish src/ItemFinder.ConsoleApp -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "ItemFinder.ConsoleApp.dll"]
