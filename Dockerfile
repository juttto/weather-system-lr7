# ЛР7: Dockerfile для розгортання WeatherSystem.API на Render (багатоетапна збірка)
# Етап 1 — збирання: образ з .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish WeatherSystem.API/WeatherSystem.API.csproj -c Release -o /app/publish

# Етап 2 — запуск: легкий образ лише з ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
# Порт задає змінна ASPNETCORE_URLS (на Render — http://0.0.0.0:10000)
EXPOSE 10000
ENTRYPOINT ["dotnet", "WeatherSystem.API.dll"]
