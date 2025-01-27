# (A) Base-Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# (B) Build-Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. SLN und alle relevanten csproj kopieren
COPY LoomChat.sln .
COPY ChatService/ChatService.csproj ChatService/
COPY CommonLibrary/CommonLibrary.csproj CommonLibrary/
# Wenn du weitere Projekte hast, die ChatService direkt/indirekt referenziert, auch diese kopieren.
# Bsp: COPY FileService/FileService.csproj FileService/

# 2. Restore
RUN dotnet restore ChatService/ChatService.csproj

# 3. Restliche Dateien kopieren
COPY . .

# 4. Build
RUN dotnet build ChatService/ChatService.csproj -c $BUILD_CONFIGURATION -o /app/build

# (C) Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish ChatService/ChatService.csproj -c $BUILD_CONFIGURATION \
    -o /app/publish /p:UseAppHost=false

# (D) Final-Stage für Runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "De.Hsfl.LoomChat.Chat.dll"]
