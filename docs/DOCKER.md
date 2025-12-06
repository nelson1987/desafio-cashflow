# 🐳 Docker

Este documento explica como executar o projeto Cashflow usando Docker e Docker Compose.

## 📋 Pré-requisitos

| Ferramenta | Versão Mínima | Download |
|------------|---------------|----------|
| **Docker** | 24.0+ | [docker.com](https://docs.docker.com/get-docker/) |
| **Docker Compose** | 2.20+ | Incluído no Docker Desktop |

### Verificar instalação

```bash
docker --version
docker compose version
```

## 🚀 Início Rápido

### 1. Configurar variáveis de ambiente

Antes de iniciar, copie o arquivo de exemplo de variáveis de ambiente:

```bash
# Windows (PowerShell)
Copy-Item .env.example .env

# Windows (CMD)
copy .env.example .env

# Linux/Mac
cp .env.example .env
```

> ⚠️ **Importante:** Edite o arquivo `.env` para ajustar as senhas e configurações conforme seu ambiente, especialmente em produção!

### 2. Iniciar apenas a infraestrutura

Para desenvolvimento local, você pode iniciar apenas os serviços de infraestrutura:

```bash
# Iniciar PostgreSQL, Redis e RabbitMQ
docker compose up -d

# Verificar se os serviços estão rodando
docker compose ps
```

### 3. Iniciar com ferramentas de desenvolvimento

```bash
# Inclui Adminer (UI do Postgres) e Redis Commander
docker compose --profile tools up -d
```

### 4. Iniciar aplicação completa (quando disponível)

```bash
# Inclui API e Worker
docker compose --profile app up -d --build
```

### 5. Executar testes no container

```bash
docker compose --profile test up tests
```

## 📊 Serviços Disponíveis

### Infraestrutura Core

| Serviço | Porta | URL/Conexão | Descrição |
|---------|-------|-------------|-----------|
| **PostgreSQL** | 5432 | `localhost:5432` | Banco de dados principal |
| **Redis** | 6379 | `localhost:6379` | Cache distribuído |
| **RabbitMQ** | 5672 | `localhost:5672` | Message broker (AMQP) |
| **RabbitMQ UI** | 15672 | http://localhost:15672 | Interface de gerenciamento |

### Ferramentas de Desenvolvimento (profile: tools)

| Serviço | Porta | URL | Descrição |
|---------|-------|-----|-----------|
| **Adminer** | 8081 | http://localhost:8081 | Interface web para PostgreSQL |
| **Redis Commander** | 8082 | http://localhost:8082 | Interface web para Redis |

### Aplicação (profile: app)

| Serviço | Porta | URL | Descrição |
|---------|-------|-----|-----------|
| **API** | 5000 | http://localhost:5000 | Cashflow Web API |
| **Worker** | - | - | Worker de consolidação |

## 🔧 Configuração

### Variáveis de Ambiente

Crie um arquivo `.env` na raiz do projeto (baseado no `.env.example`):

```env
# PostgreSQL
POSTGRES_USER=cashflow
POSTGRES_PASSWORD=cashflow123
POSTGRES_DB=cashflow_db
POSTGRES_PORT=5432

# Redis
REDIS_PORT=6379

# RabbitMQ
RABBITMQ_USER=cashflow
RABBITMQ_PASSWORD=cashflow123
RABBITMQ_VHOST=cashflow
RABBITMQ_PORT=5672
RABBITMQ_MGMT_PORT=15672

# API
API_PORT=5000

# Ferramentas
ADMINER_PORT=8081
REDIS_COMMANDER_PORT=8082
```

### Credenciais Padrão

| Serviço | Usuário | Senha |
|---------|---------|-------|
| **PostgreSQL** | cashflow | cashflow123 |
| **RabbitMQ** | cashflow | cashflow123 |

## 📊 Observabilidade

Para monitoramento completo, suba também a stack de observabilidade:

```bash
# Subir infraestrutura e aplicação
docker compose --profile app up -d --build

# Subir observabilidade (Grafana, Prometheus, Loki, Jaeger)
docker compose -f docker-compose.observability.yml up -d
```

### Serviços de Observabilidade

| Serviço | Porta | URL | Descrição |
|---------|-------|-----|-----------|
| **Grafana** | 3000 | http://localhost:3000 | Dashboards (admin/cashflow123) |
| **Prometheus** | 9090 | http://localhost:9090 | Métricas (P95, RPS) |
| **Loki** | 3100 | http://localhost:3100 | Logs estruturados |
| **Jaeger** | 16686 | http://localhost:16686 | Traces distribuídos |

> 📖 Veja mais detalhes em [OBSERVABILIDADE.md](OBSERVABILIDADE.md)

## 📁 Estrutura de Arquivos Docker

```
├── Dockerfile                      # Build da API
├── Dockerfile.worker               # Build do Worker
├── docker-compose.yml              # Serviços de infraestrutura
├── docker-compose.override.yml     # Configurações de desenvolvimento
├── docker-compose.observability.yml # Grafana, Prometheus, Loki, Jaeger
├── .dockerignore                   # Arquivos ignorados no build
├── .env.example                    # Template de variáveis de ambiente
├── .env                            # Variáveis de ambiente (não versionado)
└── docker/
    ├── postgres/
    │   └── init/
    │       └── 01-init.sql         # Script de inicialização do banco
    └── observability/
        ├── prometheus.yml          # Configuração do Prometheus
        ├── loki-config.yml         # Configuração do Loki
        └── grafana/
            ├── provisioning/       # Datasources e dashboards
            └── dashboards/         # JSON dos dashboards
```

### `docker-compose.override.yml`

Este arquivo é usado para sobrescrever as configurações do `docker-compose.yml` em ambiente de desenvolvimento. Por padrão, ele:
- Adiciona a API e o Worker ao compose.
- Mapeia o código-fonte local para dentro dos containers, permitindo o hot-reload.
- Expõe as portas da aplicação.

Este arquivo é carregado automaticamente pelo Docker Compose, não sendo necessário especificá-lo com a flag `-f`.

## 💻 Comandos Úteis

### Gerenciamento de Containers

```bash
# Iniciar serviços
docker compose up -d

# Parar serviços
docker compose down

# Parar e remover volumes (CUIDADO: apaga dados!)
docker compose down -v

# Ver logs de todos os serviços
docker compose logs -f

# Ver logs de um serviço específico
docker compose logs -f postgres

# Reiniciar um serviço
docker compose restart redis

# Ver status dos serviços
docker compose ps
```

### Acesso aos Containers

```bash
# Acessar shell do PostgreSQL
docker compose exec postgres psql -U cashflow -d cashflow_db

# Acessar shell do Redis
docker compose exec redis redis-cli

# Executar query no PostgreSQL
docker compose exec postgres psql -U cashflow -d cashflow_db -c "SELECT * FROM cashflow.lancamentos;"
```

### Build e Desenvolvimento

```bash
# Rebuild das imagens
docker compose build --no-cache

# Build de um serviço específico
docker compose build api

# Ver imagens criadas
docker images | grep cashflow
```

## 🏥 Health Checks

Todos os serviços possuem health checks configurados:

```bash
# Verificar saúde dos containers
docker compose ps

# Detalhes do health check
docker inspect --format='{{json .State.Health}}' cashflow-postgres
```

### Status esperado

```
NAME                 STATUS                  PORTS
cashflow-postgres    running (healthy)       0.0.0.0:5432->5432/tcp
cashflow-redis       running (healthy)       0.0.0.0:6379->6379/tcp
cashflow-rabbitmq    running (healthy)       0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

## 🔌 Conectando a Aplicação

### Connection Strings

Use estas connection strings na sua aplicação .NET:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cashflow_db;Username=cashflow;Password=cashflow123",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "cashflow",
    "Password": "cashflow123",
    "VirtualHost": "cashflow"
  }
}
```

### Exemplo de appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cashflow_db;Username=cashflow;Password=cashflow123",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "cashflow",
    "Password": "cashflow123",
    "VirtualHost": "cashflow"
  }
}
```

## 🐛 Troubleshooting

### Porta já em uso

```bash
# Verificar o que está usando a porta
netstat -an | findstr :5432  # Windows
lsof -i :5432                # Linux/Mac

# Alterar a porta no .env
POSTGRES_PORT=5433
```

### Container não inicia

```bash
# Ver logs detalhados
docker compose logs postgres

# Remover container e volume para reiniciar do zero
docker compose down -v
docker compose up -d
```

### Problemas de permissão no Windows

```powershell
# Executar PowerShell como Administrador
# Verificar se Docker Desktop está rodando
```

### Limpar tudo e recomeçar

```bash
# Remove containers, volumes, redes e imagens
docker compose down -v --rmi local
docker system prune -af

# Recria tudo
docker compose up -d
```

### Container crashando com código 139 (ICU Error)

Se o container da API ou Worker ficar reiniciando com exit code 139 e o log mostrar:

```
Couldn't find a valid ICU package installed on the system
```

Isso significa que a biblioteca ICU não está instalada no Alpine. Verifique se o Dockerfile contém:

```dockerfile
RUN apk add --no-cache icu-libs && \
    adduser -D -h /app appuser && \
    chown -R appuser:appuser /app
```

Após corrigir, reconstrua as imagens:

```bash
docker compose --profile app up -d --build --force-recreate
```

## 📊 Monitoramento

### Grafana (Métricas, Logs, Traces)

Acesse http://localhost:3000 com as credenciais:
- **Usuário:** admin
- **Senha:** cashflow123

**Dashboards disponíveis:**
- 📊 Cashflow Overview - P95, RPS, Error Rate
- 📝 Application Logs - Logs estruturados com trace ID

### Jaeger (Traces Distribuídos)

Acesse http://localhost:16686 para visualizar traces:
- Selecione o serviço `cashflow-api` ou `cashflow-worker`
- Visualize o caminho das requisições

### Prometheus (Métricas)

Acesse http://localhost:9090 para queries de métricas:
```promql
# P95 Latency
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le))
```

### RabbitMQ Management UI

Acesse http://localhost:15672 com as credenciais:
- **Usuário:** cashflow
- **Senha:** cashflow123

### Adminer (PostgreSQL UI)

Acesse http://localhost:8081 e conecte com:
- **Sistema:** PostgreSQL
- **Servidor:** postgres
- **Usuário:** cashflow
- **Senha:** cashflow123
- **Base de dados:** cashflow_db

### Redis Commander

Acesse http://localhost:8082 para visualizar dados do Redis.

## 🚀 Produção

Para produção, use imagens otimizadas e configure:

```bash
# Build de produção
docker build -t cashflow-api:latest --target final .
docker build -t cashflow-worker:latest -f Dockerfile.worker --target final .

# Tag para registry
docker tag cashflow-api:latest seu-registry/cashflow-api:v1.0.0
docker push seu-registry/cashflow-api:v1.0.0
```

### Considerações de Produção

- ✅ Use secrets para senhas (Docker Swarm ou Kubernetes)
- ✅ Configure limites de recursos (CPU/memória)
- ✅ Use volumes persistentes com backup
- ✅ Configure logging centralizado (Loki)
- ✅ Implemente monitoramento (Prometheus/Grafana) - **Já implementado**
- ✅ Configure tracing distribuído (Jaeger) - **Já implementado**
- ✅ Use HTTPS/TLS para comunicação
- ✅ Configure OpenTelemetry para observabilidade - **Já implementado**

## 📚 Referências

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [Redis Docker Hub](https://hub.docker.com/_/redis)
- [RabbitMQ Docker Hub](https://hub.docker.com/_/rabbitmq)

