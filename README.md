# Order Management System

Небольшой REST API управления заказами на .NET 10, PostgreSQL и Clean Architecture.
Команды и запросы диспетчеризуются сгенерированным кодом Mediator, без runtime-reflection.

## Архитектура

```text
WebApi ───────► Application ───────► Domain
  │                  ▲
  └──► Infrastructure┘
```

- `Domain` — агрегат заказа, value objects, инварианты и события; внешних пакетов нет.
- `Application` — CQRS-сценарии, DTO, валидация и порты `IOrderRepository`/`IUnitOfWork`.
- `Infrastructure` — EF Core 10, PostgreSQL, транзакции и реализации портов.
- `WebApi` — HTTP-контракты, Problem Details (RFC 9457), таймауты и composition root.
- `tests` — xUnit v3/v4 на Microsoft Testing Platform.

Чтение выполняется без EF tracking, путь изменения включает его явно. Статус заказа —
optimistic concurrency token, поэтому параллельные изменения возвращают `409`, а не
молча перезаписывают друг друга. Ограничены тело запроса (1 МиБ), длительность запроса
(30 секунд), страница (100 записей) и заказ (100 позиций).

## Запуск

Требуются .NET SDK `10.0.400` и Docker Compose. Перед первым запуском задайте
локальный пароль (файл `.env` не попадает в Git):

```powershell
Copy-Item .env.example .env
# Замените значение ORDER_MANAGEMENT_DB_PASSWORD в .env.
docker compose up --build
```

API: `http://localhost:8080`; Swagger в Development: `/swagger`; liveness: `/health/live`.

Локальный запуск без контейнера API:

```powershell
docker compose up order-management-database --detach
$env:ConnectionStrings__OrderManagementDatabase = "Host=localhost;Port=5432;Database=order_management;Username=order_management_user;Password=<пароль из .env>;GSS Encryption Mode=Disable"
dotnet run --project src/OrderManagement.WebApi
```

Production-конфигурация `ConnectionStrings__OrderManagementDatabase` должна поступать
из секрет-хранилища или переменных среды.

## Проверка

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet list OrderManagementSystem.slnx package --outdated --include-transitive
dotnet list OrderManagementSystem.slnx package --vulnerable --include-transitive
dotnet list OrderManagementSystem.slnx package --deprecated --include-transitive
```

Версии NuGet централизованы в `Directory.Packages.props`. Restore проверяет прямые и
транзитивные уязвимости уровня `low` и выше, а найденные `NU1901`–`NU1904` ломают сборку.
Текущий граф NuGet использует только open-source лицензии (MIT, Apache-2.0, BSD-3-Clause
и PostgreSQL); это следует перепроверять после каждого обновления зависимостей.
Сводка лицензий: [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

## Границы готовности

Swagger и автоматическое `EnsureCreated` включены только в Development. Для production
нужны управляемые EF-миграции, TLS на reverse proxy, аутентификация/авторизация,
ротация секретов, резервное копирование и мониторинг. Доменные события сейчас
внутрипроцессные; перед добавлением критичных внешних эффектов нужен transactional outbox.
