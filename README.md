# Order Management System

REST API для управления заказами, построенный на **Clean Architecture** с применением **CQRS** и **Domain-Driven Design**.

## Стек технологий

- **.NET 10 / ASP.NET Core** — веб-фреймворк
- **Mediator** — реализация CQRS (команды и запросы), диспетчеризация генерируется на сборке
- **Entity Framework Core 10 + PostgreSQL** — доступ к данным
- **FluentValidation** — декларативная валидация входных данных
- **Serilog** — структурированное логирование
- **xUnit + NSubstitute** — модульные тесты
- **Docker / Docker Compose** — контейнеризация

## Архитектура

```
src/
├── OrderManagement.Domain          # Предметная область: сущности, объекты-значения,
│                                   # доменные события, бизнес-правила. Без внешних зависимостей.
├── OrderManagement.Application     # Сценарии использования: команды, запросы, валидаторы,
│                                   # конвейерные behaviors (логирование, валидация).
├── OrderManagement.Infrastructure  # Реализация хранилищ: EF Core, PostgreSQL, Unit of Work.
└── OrderManagement.WebApi          # HTTP-слой: контроллеры, промежуточное ПО, Swagger.
tests/
└── OrderManagement.UnitTests       # Модульные тесты домена и обработчиков.
```

Зависимости направлены строго внутрь: `WebApi → Application → Domain`, `Infrastructure → Application`.
Слой домена не знает ни о базе данных, ни о HTTP — поэтому бизнес-логика тестируется без инфраструктуры.

## Применённые принципы и паттерны

- **SOLID** — например, репозитории объявлены интерфейсами в домене и реализованы в инфраструктуре (DIP);
  валидация и логирование вынесены в отдельные pipeline behaviors (SRP, OCP).
- **Агрегат** (DDD) — заказ изменяется только через свои методы, инварианты нарушить невозможно.
- **Объекты-значения** — `MoneyAmount` и `DeliveryAddress` исключают целый класс ошибок с «голыми» примитивами.
- **Доменные события** — побочные процессы подключаются без изменения бизнес-логики.
- **Repository + Unit of Work** — одна бизнес-операция, одна транзакция.
- **Разделение чтения и записи** — запросы читают заказы без отслеживания изменений EF Core,
  команды берут агрегат с отслеживанием; сторона чтения не платит за снимки состояния.
- **ProblemDetails (RFC 9457)** — единый формат ошибок API.

## Запуск

```bash
# Полное окружение (PostgreSQL + API) в Docker
docker compose up --build

# Или локально: поднять базу и запустить API
docker compose up order-management-database --detach
dotnet run --project src/OrderManagement.WebApi
```

Swagger UI: `http://localhost:8080/swagger` (в Docker) или порт из вывода `dotnet run`.

## Тесты

```bash
dotnet test
```
