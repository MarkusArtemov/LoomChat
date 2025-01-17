# Verteilte Systeme – Hausarbeit

**Projektgruppe:** Markus Artemov und Matz Schultz

## Voraussetzungen

- .NET 8 SDK
- Docker oder Docker Desktop

## Schnellstart (Docker)

1. Repository klonen und ins Verzeichnis wechseln
2. `docker-compose up -d` (startet alle Services + PostgreSQL)

## Lokale Entwicklung

- `docker-compose up -d postgres` (nur DB)
- Wähle im jeweiligen Projekt (AuthService, ChatService, FileService) ein http-Profil
- Benötigte Services in Visual Studio starten
