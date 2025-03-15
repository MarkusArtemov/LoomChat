# Verteilte Systeme - Chat Anwendung

## Voraussetzungen

- .NET 8 SDK
- Docker oder Docker Desktop

## Schnellstart (Docker)

1. Repository klonen und ins Verzeichnis wechseln
2. **Gesamte Umgebung** (alle Services + PostgreSQL) starten:
 ```bash
   docker compose --profile allservices up -d
   ```

## Lokale Entwicklung

- **Nur die Datenbank (und pgAdmin) starten** (wenn du die Services lokal in Visual Studio laufen lassen willst):
```bash
 docker compose up -d
```

- Wähle im jeweiligen Projekt (AuthService, ChatService, FileService) ein http-Profil
- Benötigte Services in Visual Studio starten
