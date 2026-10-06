# TaskBoard

Kanban tarzı görev yönetimi uygulaması — ASP.NET Core (Clean Architecture) + React/TypeScript.

## Klasör yapısı

```
TaskBoard/
├── src/
│   ├── TaskBoard.Domain/          → Entity'ler, enum'lar (bağımlılık yok)
│   ├── TaskBoard.Application/     → İş kuralları, DTO'lar, servis arayüzleri
│   ├── TaskBoard.Infrastructure/  → EF Core, Redis, Hangfire, mail servisi
│   └── TaskBoard.API/             → Controller'lar, SignalR Hub, JWT ayarları
├── tests/
│   └── TaskBoard.Tests/           → xUnit
├── client/                        → React + TypeScript (Vite)
└── docker-compose.yml
```

Bağımlılık yönü: `API → Infrastructure → Application → Domain`
