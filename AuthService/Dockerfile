# (A) Base-Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# (B) Build-Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Kopiere Sln und csproj-Dateien (aller Projekte) an die richtigen Stellen:
COPY LoomChat.sln .
COPY AuthService/AuthService.csproj AuthService/
COPY CommonLibrary/CommonLibrary.csproj CommonLibrary/
# Weitere csproj von ChatService, FileService, etc. wenn sie in der selben sln sind
# COPY ChatService/ChatService.csproj ChatService/
# ...

# 2. Restore
RUN dotnet restore AuthService/AuthService.csproj

# 3. Kopiere den Rest
COPY . .  
# --> Jetzt liegen alle Ordner (AuthService, CommonLibrary, ChatService etc.) in /src

# 4. Build
RUN dotnet build AuthService/AuthService.csproj -c $BUILD_CONFIGURATION -o /app/build

# (C) Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish AuthService/AuthService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# (D) Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "De.Hsfl.LoomChat.Auth.dll"]