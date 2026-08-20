# Spec: Enterprise IoT Sensor Monitoring Platform

## Objective

Fiziksel cihaz olmadan, multi-tenant hiyerarşi (Company → Facility → Zone → Sensor) üzerinde sensör ölçümlerini toplayan, dinamik alarm kuralları ve bakım süreçleri olan, Grafana ile izlenen bir .NET 10 IoT platformu.

Kullanıcı: müşteri firmalar ve operatörler. Başarı: `docker compose up -d` ile API, Worker, PostgreSQL ve Grafana ayağa kalkar; aktif sensörler kademeli veri üretir; alarm ve kalibrasyon kuralları çalışır.

## Tech Stack

- .NET 10 (C#)
- ASP.NET Core Web API (controllers)
- .NET Worker Service
- PostgreSQL + EF Core (Code-First, migrations)
- Serilog (Information=yeşil, Warning=sarı, Error=kırmızı)
- JWT Bearer (BCrypt, SuperAdmin / CompanyAdmin / Operator)
- FluentValidation (request DTO'lar)
- Docker Compose (API, Worker, PostgreSQL, Grafana)
- Kubernetes manifests (`k8s/`)

## Commands

```
docker compose up -d --build
dotnet restore IoTSensorMonitoring.sln
dotnet build IoTSensorMonitoring.sln
dotnet ef migrations add <Name> --project src/IoTSensorMonitoring.Infrastructure --startup-project src/IoTSensorMonitoring.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/IoTSensorMonitoring.Infrastructure --startup-project src/IoTSensorMonitoring.Api
dotnet run --project src/IoTSensorMonitoring.Api
dotnet run --project src/IoTSensorMonitoring.Worker
dotnet test IoTSensorMonitoring.sln
kubectl apply -k k8s
```

## Project Structure

```
src/
  IoTSensorMonitoring.Domain          → Entities, enums (bağımlılık yok)
  IoTSensorMonitoring.Application     → DTO, interface, iş kuralları (→ Domain)
  IoTSensorMonitoring.Infrastructure  → EF Core, PostgreSQL, repository (→ Application)
  IoTSensorMonitoring.Api             → REST, Swagger, Serilog host (→ Application, Infrastructure)
  IoTSensorMonitoring.Worker          → Simülatör + rules engine host (→ Application, Infrastructure)
k8s/                                  → Kubernetes (Deployment / Service / PVC)
tests/
  IoTSensorMonitoring.Tests           → xUnit + Moq + FluentAssertions
```

Bağımlılık yönü: Api/Worker → Infrastructure → Application → Domain. Domain hiçbir katmana referans vermez.

## Domain Model

Company → Facility → Zone → Sensor → Measurement / AlertRule / AlertHistory / MaintenanceLog  
Company → DeviceModel → Sensor

Enum alanlar (DBML varchar notları): `SensorStatus`, `SensorMetric`, `ComparisonOperator`, `AlertSeverity`, `MaintenanceActionType`, `UserRole`.  
`DeviceModel.SupportedMetrics` şemadaki gibi string (ör. `Temperature,Humidity,Pressure`).  
`User.CompanyId` null ise SuperAdmin (sistem geneli); dolu ise şirket kullanıcısı.  
`AlertHistory.ResolvedByUserId` JWT’deki kullanıcı id’si (nullable Guid).

## Auth

- `POST /api/auth/login` (anonim), `GET /api/auth/me`
- `GET /api/users`, `POST /api/users` (SuperAdmin / CompanyAdmin; kullanıcı oluşturma, self-register değil)
- JWT claim: `sub`, `email`, `role`, `company_id` (SuperAdmin’de yok)
- EF global query filter: CompanyAdmin / Operator yalnız kendi şirketini görür; SuperAdmin ve Worker filtre uygulamaz
- DeviceModel şirket kataloğu (`CompanyId`); tesis değil işletme bazlı. Tenant filter var; yazma SuperAdmin / CompanyAdmin
- Seed (Development, `SeedSettings.Enabled`): SuperAdmin + demo katalog. Şifre hepsi `Admin123!`
  - SuperAdmin: `admin@iot.local`
  - Nova Enerji: `ayse.kaya@nova.local` (CompanyAdmin), `mehmet.demir@nova.local` (Operator)
  - Atlas Lojistik: `elif.yildiz@atlas.local` (CompanyAdmin), `can.oz@atlas.local` (Operator)
  - 2 tesis, 7 bölge, 3 cihaz modeli, 7 sensör (5 Active), 4 alarm kuralı. Ölçümleri Worker üretir. Idempotent (şirket `ContactEmail` varsa atlar).

## Worker

Worker, Application’daki `ISensorSimulationService` ile her `IntervalSeconds` (varsayılan 10) bir döngü çalıştırır. JWT yok; `SystemCurrentUser` tenant filtresini kapatır.

- Yalnız `SensorStatus.Active` sensörler
- Ölçüm: son değerden kademeli sapma (random walk); yalnızca `DeviceModel.SupportedMetrics` alanları doldurulur
- Alarm: aktif `AlertRule` eşiği aşılırsa `AlertHistory` yazılır; aynı kural için çözülmemiş kayıt varsa tekrar yazılmaz
- Kalibrasyon: `CalibrationPeriodDays` doluysa vadesi geçmiş / `CalibrationWarningDays` (7) içindeyse Warning log
- Döngü başına DI scope (DbContext sızıntısı olmasın)

## Grafana

`docker compose up -d --build` API, Worker, PostgreSQL, Grafana ve PulseGrid UI'yi kaldırır.

- PulseGrid UI: `http://localhost:4040` (nginx; `/api` ve `/oauth` API'ye proxy)
- API Swagger: `http://localhost:8080` (admin@iot.local / Admin123!)
- Grafana: `http://localhost:3000`
  - SuperAdmin acil giriş: `admin` / `admin` (Main Org, tüm şirketler)
  - Şirket kullanıcısı: **Sign in with PulseGrid** → PulseGrid e-posta/şifre
- Worker container içinde çalışır; JWT yok

Şirket izolasyonu (RLS):

- API, her şirket için Postgres rolü (`g_c_<guid>`) ve Grafana Organization oluşturur
- Rol `app.company_id` session ayarı taşır; `grafana_reader` üzerindeki RLS politikaları yalnız o şirketin satırlarını gösterir
- Şirket Grafana kullanıcısı Viewer'dır; Explore/SQL ile başka şirketi göremez
- SuperAdmin Grafana Main Org datasource'u `iot` kullanıcısıdır (RLS uygulanmaz)

Dashboard `grafana/dashboards/iot-monitoring.json` Main Org'a provision edilir; şirket org'larına API kopyalar.

API container açılışta `Database.Migrate` + SuperAdmin seed çalıştırır; Grafana tenant sync arka planda org/rol/datasource üretir.

## Kubernetes

Docker Desktop Kubernetes açıkken (önce `docker compose down`):

```
docker compose build
kubectl apply -k k8s
```

- API: `http://localhost:30080`
- Grafana: `http://localhost:30300` (PulseGrid OAuth veya admin/admin)
- Namespace: `iot`
- İmajlar: `iot-api:local`, `iot-worker:local` (`imagePullPolicy: IfNotPresent`)

## Code Style

- PascalCase tipler/metodlar, `_camelCase` private field
- Async suffix: `GetByIdAsync`
- Entity: Domain; DTO: Application; DbContext/config: Infrastructure
- Request DTO doğrulama: Application `Validations/` altında FluentValidation; Domain entity'ye validator yok
- Nullable enabled, implicit usings

## Testing Strategy

`tests/IoTSensorMonitoring.Tests` — xUnit, Moq, FluentAssertions. Servisler mock repository ile; controller Authorize attribute'ları reflection ile. Request validator'lar gerçek FluentValidation kurallarıyla. Integration / WebApplicationFactory bu fazda yok.

## Boundaries

- Always: Katman kurallarına uy; Domain'e EF/ASP.NET paketi ekleme; log seviyelerini doğru kullan
- Ask first: Yeni NuGet, şema değişikliği, yeni proje
- Never: Secret commit, Domain'den dış katmana referans, katman atlayarak Data'yı Api'den çağırma (repository somut tipi)

## Success Criteria (bu faz)

- [x] 5 proje + 1 solution
- [x] `dotnet build` hatasız
- [x] Referans grafiği yukarıdaki gibi
- [x] Katman klasör iskeleti hazır
- [x] JWT + tenant filtre
- [x] `dotnet test` yeşil (Auth, TenantGuard, Authorize, Company/Facility/Alert)
- [x] Worker simülatör + alarm motoru + kalibrasyon uyarısı
- [x] FluentValidation (request DTO'lar, Application katmanı)
- [x] Grafana dashboard
- [x] `docker compose up -d --build` → API + Worker + PostgreSQL + Grafana
- [x] Kubernetes manifests (`kubectl apply -k k8s`)

## Open Questions

- (yok)
