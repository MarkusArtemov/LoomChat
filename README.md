# Verteilte Systeme – Hausarbeit

**Projektgruppe:** Markus Artemov und Matz Schultz

## Voraussetzungen

- .NET 8 SDK
- Docker oder Docker Desktop

## Schnellstart (Docker)

1. Repository klonen und ins Verzeichnis wechseln
2. **Gesamte Umgebung** (alle Services + PostgreSQL) starten:
3. 
   ```bash docker compose --profile allservices up -d```

- **Nur die Datenbank (und pgAdmin) starten** (wenn du die Services lokal in Visual Studio laufen lassen willst):
- 
  ```bash docker compose up -d```

Wähle in jedem Projekt (AuthService, ChatService, FileService) das gewünschte `http`-Profil und starte es über deine IDE. So kommunizieren die lokal gestarteten Services mit der in Docker laufenden Datenbank.
