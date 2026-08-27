# Многоступенчатая сборка: образ сборки отделён от итогового образа времени выполнения
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS build-stage
WORKDIR /source

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV NUGET_XMLDOC_MODE=skip

# Сначала копируем только файлы проектов — слой restore кешируется при неизменных зависимостях
COPY Directory.Build.props Directory.Packages.props ./
COPY src/OrderManagement.Domain/OrderManagement.Domain.csproj src/OrderManagement.Domain/packages.lock.json src/OrderManagement.Domain/
COPY src/OrderManagement.Application/OrderManagement.Application.csproj src/OrderManagement.Application/packages.lock.json src/OrderManagement.Application/
COPY src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj src/OrderManagement.Infrastructure/packages.lock.json src/OrderManagement.Infrastructure/
COPY src/OrderManagement.WebApi/OrderManagement.WebApi.csproj src/OrderManagement.WebApi/packages.lock.json src/OrderManagement.WebApi/
RUN dotnet restore --locked-mode src/OrderManagement.WebApi/OrderManagement.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/OrderManagement.WebApi/OrderManagement.WebApi.csproj \
    --configuration Release \
    --output /application \
    --no-self-contained \
    -p:UseAppHost=false \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble-chiseled AS runtime-stage
WORKDIR /application
COPY --from=build-stage /application .
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "OrderManagement.WebApi.dll"]
