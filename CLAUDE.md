# TemperaturaVS — Plataforma de Microsserviços (.NET 8 + Angular 17)

## Visão Geral

Plataforma distribuída com 4 APIs ASP.NET Core 8, frontend Angular 17, rodando em Kubernetes (Minikube):
- Consulta de temperatura/clima (OpenWeatherMap)
- Geolocalização por IP (ip-api.com)
- Calculadora de idade e signo zodiacal
- **Fila de pedidos com Service Bus + Event Hub (RabbitMQ)** — 10.000 pedidos no SQLite
- Mensageria event-driven com RabbitMQ
- Observabilidade: Prometheus + OpenTelemetry (OTLP)
- Cache com Redis; Persistência com MongoDB + SQLite

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                        MINIKUBE CLUSTER                         │
│  ┌────────────┐   ┌──────────────┐   ┌──────────────────────┐  │
│  │  frontend  │──▶│ api-weather  │──▶│  RabbitMQ (AMQP)    │  │
│  │  Angular   │   │  .NET 8      │   │  Service Bus (direct)│  │
│  │  :80/3000  │   │  :8080/3001  │   │  Event Hub (fanout)  │  │
│  └────────────┘   └──────────────┘   └──────────────────────┘  │
│         │         ┌──────────────┐              ▲              │
│         ├────────▶│ api-location │──────────────┘              │
│         │         │  .NET 8      │                             │
│         │         │  :8080/3002  │                             │
│         │         └──────────────┘                             │
│         │         ┌──────────────┐                             │
│         ├────────▶│  api-person  │                             │
│         │         │  .NET 8      │                             │
│         │         │  :8080/3003  │                             │
│         │         └──────────────┘                             │
│         │         ┌──────────────┐   ┌──────────────────────┐  │
│         └────────▶│  api-orders  │──▶│ orders.servicebus    │  │
│                   │  .NET 8      │   │  (direct exchange)   │  │
│                   │  + Worker    │◀──│  order-processing    │  │
│                   │  :8080/3004  │   └──────────────────────┘  │
│                   └──────┬───────┘                             │
│                          │ publica status changes              │
│                   ┌──────▼──────────────────────────────┐     │
│                   │  orders.eventhub (fanout exchange)   │     │
│                   │  ├── audit-events                    │     │
│                   │  ├── analytics-events                │     │
│                   │  └── notification-events             │     │
│                   └─────────────────────────────────────┘     │
│  ┌──────────┐  ┌──────────┐  ┌─────────────────────────────┐  │
│  │ MongoDB  │  │  Redis   │  │ SQLite (PVC)                │  │
│  │ :27017   │  │ :6379    │  │ transactions.db + orders.db │  │
│  └──────────┘  └──────────┘  └─────────────────────────────┘  │
│  ┌──────────┐  ┌──────────────────────────────────────────┐   │
│  │Prometheus│  │ OTel Collector :4317(gRPC) / :4318(HTTP) │   │
│  │  :9090   │  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Stack Tecnológica

| Camada | Tecnologia |
|--------|-----------|
| Backend | ASP.NET Core 8 (C#) |
| Frontend | Angular 17 (standalone components, lazy routes) |
| Orquestração | Kubernetes / Minikube |
| Mensageria | RabbitMQ (Service Bus + Event Hub simulados) |
| Cache | Redis (StackExchange.Redis) |
| NoSQL | MongoDB (MongoDB.Driver) |
| Relacional | SQLite (EF Core para orders, ADO.NET para transactions) |
| Observabilidade | OpenTelemetry OTLP + Prometheus (prometheus-net) |

---

## Serviços

### `api-weather` (local :3001, cluster :8080)
- `GET /weather?city=<cidade>` — temperatura atual
- `GET /forecast?city=<cidade>&days=<n>` — previsão N dias
- `GET /health` — healthcheck
- `GET /metrics` — métricas Prometheus

**Fluxo**: Redis (TTL 10min) → OpenWeatherMap API → RabbitMQ topic → MongoDB + SQLite

---

### `api-location` (local :3002, cluster :8080)
- `GET /location[?ip=<ip>]` — geolocalização por IP
- `GET /health`

**Fluxo**: Redis (TTL 24h) → ip-api.com → RabbitMQ topic → MongoDB + SQLite

---

### `api-person` (local :3003, cluster :8080)
- `POST /person` — `{ "name": "João", "birthdate": "1990-05-15" }`
- `GET /health`

**Resposta**: nome, idade (anos/meses/dias), signo zodiacal

---

### `api-orders` (local :3004, cluster :8080) — **NOVO**
- `GET  /api/orders?page=1&pageSize=50&status=Pending` — lista paginada
- `GET  /api/orders/stats` — contagem por status
- `POST /api/orders/process` — enfileira pendentes no Service Bus
- `POST /api/orders/reset` — reseta todos para Pending
- `GET  /health`

**Modelo de Order**:
```
Id:          int (PK, auto-increment)
OrderNumber: int (100001–110000)
TotalValue:  decimal (R$10,00 a R$9.999,99, aleatório)
Status:      Pending | Processing | Completed | Failed
CreatedAt:   DateTime
ProcessedAt: DateTime?
ErrorMessage: string?
```

**Fluxo de processamento**:
```
POST /process
  → enfileira IDs pendentes no Service Bus (orders.servicebus, direct)
  → OrderProcessorWorker (BackgroundService) consome com ack manual
  → atualiza status: Pending → Processing → Completed(90%) | Failed(10%)
  → publica cada mudança no Event Hub (orders.eventhub, fanout)
  → 3 consumer groups: audit-events, analytics-events, notification-events
```

---

## RabbitMQ — Service Bus vs Event Hub

| Azure | Local (RabbitMQ) | Exchange Type |
|-------|-----------------|---------------|
| Service Bus Queue | `orders.servicebus` → `order-processing` | direct |
| Event Hub | `orders.eventhub` | fanout |
| Event Hub Consumer Group | `audit-events`, `analytics-events`, `notification-events` | — |

---

## Observabilidade

### OpenTelemetry
Cada API exporta traces via OTLP para `http://otel-collector:4318`.

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("api-orders"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint)));
```

### Métricas Prometheus (prometheus-net)
Endpoint `/metrics` automático. Métricas do api-orders:
- `orders_processed_total{status}` — total processado por status
- `order_processing_duration_seconds` — histograma de latência
- `orders_enqueued_total` — total enfileirado
- `http_requests_total` + `http_request_duration_seconds` — padrão ASP.NET

---

## Port-Forwards

```powershell
.\portforward.ps1           # Todos os serviços
.\portforward.ps1 -Stop     # Para tudo
.\portforward.ps1 -Service orders  # Só api-orders
```

| Serviço | URL |
|---------|-----|
| Frontend Angular | http://localhost:3000 |
| api-weather Swagger | http://localhost:3001/swagger |
| api-location Swagger | http://localhost:3002/swagger |
| api-person Swagger | http://localhost:3003/swagger |
| api-orders Swagger | http://localhost:3004/swagger |
| RabbitMQ UI | http://localhost:15672 (guest/guest) |
| Prometheus | http://localhost:9090 |

---

## Estrutura de Diretórios

```
TemperaturaVsDotNet/
├── CLAUDE.md
├── portforward.ps1
├── docker-compose.dev.yaml
├── .env
├── .claude/commands/
│   ├── start.md, deploy.md, status.md, logs.md, teardown.md
│   ├── portforward.md, seed.md
├── api-weather/   (ASP.NET Core 8)
│   ├── WeatherApi.csproj, Program.cs, Dockerfile
│   ├── Controllers/WeatherController.cs
│   ├── Models/WeatherModels.cs
│   └── Services/{Weather,Cache,Messaging,Persistence}Service.cs
├── api-location/  (ASP.NET Core 8)
├── api-person/    (ASP.NET Core 8)
├── api-orders/    (ASP.NET Core 8)
│   ├── OrdersApi.csproj, Program.cs, Dockerfile
│   ├── Controllers/OrdersController.cs
│   ├── Models/Order.cs
│   ├── Data/{OrdersDbContext,OrderSeeder}.cs
│   ├── Services/{Order,ServiceBus,EventHub}Service.cs
│   └── Workers/OrderProcessorWorker.cs
├── frontend/      (Angular 17)
│   ├── src/app/{app.component,app.routes}.ts
│   ├── src/app/services/api.service.ts
│   ├── src/app/pages/{home,weather,location,person,orders}/
│   ├── Dockerfile, nginx.conf, package.json
└── k8s/
    ├── namespace.yaml
    ├── configmaps/{otel-config,otel-config-local,prometheus-local}.yaml
    ├── volumes/sqlite-pvc.yaml
    ├── deployments/{api-weather,api-location,api-person,api-orders,
    │               frontend,rabbitmq,mongodb,redis,prometheus,otel-collector}.yaml
    └── services/services.yaml
```

---

## Setup Rápido

### Dev local (docker-compose)
```powershell
docker-compose -f docker-compose.dev.yaml up --build
```

### Kubernetes (Minikube)
```powershell
# Iniciar e configurar
minikube start --memory=6144 --cpus=4 --driver=docker
& minikube -p minikube docker-env --shell powershell | Invoke-Expression

# Build das 5 imagens
docker build -t api-weather:local ./api-weather
docker build -t api-location:local ./api-location
docker build -t api-person:local ./api-person
docker build -t api-orders:local ./api-orders
docker build -t frontend:local ./frontend

# Deploy
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/volumes/
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/services/

# Aguardar RabbitMQ primeiro (outros services dependem)
kubectl wait --for=condition=ready pod -l app=rabbitmq -n temperaturaapp --timeout=120s
kubectl wait --for=condition=ready pod --all -n temperaturaapp --timeout=180s

# Port-forwards
.\portforward.ps1
```

---

## Skills disponíveis

| Skill | Descrição |
|-------|-----------|
| `/start` | Inicializa Minikube + build + deploy completo |
| `/deploy <svc>` | Rebuild e redeploy de um serviço |
| `/status` | Estado completo do cluster |
| `/logs <svc>` | Ver logs em tempo real |
| `/portforward` | Abre port-forwards para todos os serviços |
| `/portforward stop` | Para todos os port-forwards |
| `/seed process` | Enfileira pedidos no Service Bus |
| `/seed reset` | Reseta todos pedidos para Pending |
| `/teardown` | Destrói o cluster |

---

## Variáveis de Ambiente

```bash
OPENWEATHER_API_KEY=your_key_here
WEATHER_API_KEY=your_key_here
RABBITMQ_URL=amqp://guest:guest@localhost:5672
MONGODB_URI=mongodb://localhost:27017/temperaturadb
REDIS_URL=localhost:6379
SQLITE_PATH=./data/transactions.db
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318
K8S_NAMESPACE=temperaturaapp
```

---

## Pré-requisitos

- Docker Desktop 24+
- Minikube 1.32+
- kubectl 1.28+
- .NET SDK 8.0 (dev local)
- Node.js 20 (dev local do frontend)

---

## Troubleshooting

**api-orders demora a iniciar**: seed de 10.000 pedidos na primeira vez (~5s). Aguardar readinessProbe.

**RabbitMQ connection refused**: aguardar readiness probe do RabbitMQ (~30s). Use `kubectl wait --for=condition=ready pod -l app=rabbitmq`.

**Imagem não encontrada**: verificar contexto Docker — `& minikube -p minikube docker-env --shell powershell | Invoke-Expression` antes do build.

**Port-forward cai**: rodar `.\portforward.ps1` novamente; o script monitora e reinicia automaticamente.
