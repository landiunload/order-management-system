# Многоступенчатая сборка: образ сборки отделён от итогового образа времени выполнения
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-stage
WORKDIR /source

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV NUGET_XMLDOC_MODE=skip

# Сначала копируем только файлы проектов — слой restore кешируется при неизменных зависимостях
COPY src/OrderManagement.Domain/OrderManagement.Domain.csproj src/OrderManagement.Domain/
COPY src/OrderManagement.Application/OrderManagement.Application.csproj src/OrderManagement.Application/
COPY src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj src/OrderManagement.Infrastructure/
COPY src/OrderManagement.WebApi/OrderManagement.WebApi.csproj src/OrderManagement.WebApi/
RUN dotnet restore --disable-parallel src/OrderManagement.WebApi/OrderManagement.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/OrderManagement.WebApi/OrderManagement.WebApi.csproj \
    --configuration Release \
    --output /application \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-stage
WORKDIR /application
COPY --from=build-stage /application .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderManagement.WebApi.dll"]
