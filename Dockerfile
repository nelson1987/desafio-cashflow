# ============================================
# Cashflow API - Multi-stage Dockerfile
# ============================================
# Build otimizado para .NET 9.0 com suporte a
# múltiplas arquiteturas e cache de layers
# ============================================

# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Instala ICU para suporte a globalização e adiciona usuário não-root
RUN apk add --no-cache icu-libs && \
    adduser -D -h /app appuser && \
    chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia apenas os arquivos de projeto primeiro (cache de restore)
COPY ["Cashflow.sln", "./"]
COPY ["src/Cashflow/Cashflow.csproj", "src/Cashflow/"]
COPY ["src/Cashflow.WebApi/Cashflow.WebApi.csproj", "src/Cashflow.WebApi/"]
COPY ["src/Cashflow.Application/Cashflow.Application.csproj", "src/Cashflow.Application/"]
COPY ["src/Cashflow.Infrastructure/Cashflow.Infrastructure.csproj", "src/Cashflow.Infrastructure/"]
COPY ["tests/Cashflow.Tests/Cashflow.Tests.csproj", "tests/Cashflow.Tests/"]
COPY ["tests/Cashflow.Application.Tests/Cashflow.Application.Tests.csproj", "tests/Cashflow.Application.Tests/"]
COPY ["tests/Cashflow.IntegrationTests/Cashflow.IntegrationTests.csproj", "tests/Cashflow.IntegrationTests/"]
COPY ["workers/Cashflow.ConsolidationWorker/Cashflow.ConsolidationWorker.csproj", "workers/Cashflow.ConsolidationWorker/"]

# Restore de dependências (layer cacheada)
RUN dotnet restore "Cashflow.sln"

# Copia o restante do código fonte
COPY . .

# Build do projeto
WORKDIR "/src/src/Cashflow.WebApi"
RUN dotnet build "Cashflow.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Cashflow.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Variáveis de ambiente padrão
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "Cashflow.WebApi.dll"]
