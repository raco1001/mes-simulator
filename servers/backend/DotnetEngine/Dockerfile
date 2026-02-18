# Backend (DotnetEngine) - Dockerfile
# Build context: servers/backend (프로젝트 루트)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DotnetEngine/DotnetEngine.csproj DotnetEngine/
RUN dotnet restore DotnetEngine/DotnetEngine.csproj

COPY DotnetEngine/ DotnetEngine/
WORKDIR /src/DotnetEngine
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

# 기본 포트 (launchSettings와 동일)
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "DotnetEngine.dll"]
