# (A) Base image (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# (B) Build image (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. SLN und alle relevanten .csproj kopieren
COPY LoomChat.sln .
COPY FileService/FileService.csproj FileService/
COPY CommonLibrary/CommonLibrary.csproj CommonLibrary/
# ggf. weitere Projekte, falls FileService sie direkt referenziert
# COPY ChatService/ChatService.csproj ChatService/
# COPY AuthService/AuthService.csproj AuthService/
# usw.

# 2. Restore
RUN dotnet restore FileService/FileService.csproj

# 3. Kopiere den gesamten Code
COPY . .

# 4. Build
RUN dotnet build FileService/FileService.csproj -c $BUILD_CONFIGURATION -o /app/build

# (C) Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish FileService/FileService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# (D) Final Stage (Runtime)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "De.Hsfl.LoomChat.File.dll"]
