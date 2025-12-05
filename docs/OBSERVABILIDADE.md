# 📊 Observabilidade

Este documento descreve a stack de observabilidade do projeto Cashflow.

## 🛠️ Stack

| Ferramenta | Propósito | Porta | URL |
|------------|-----------|-------|-----|
| **Grafana** | Dashboards | 3000 | http://localhost:3000 |
| **Prometheus** | Métricas | 9090 | http://localhost:9090 |
| **Loki** | Logs | 3100 | http://localhost:3100 |
| **Jaeger** | Traces | 16686 | http://localhost:16686 |

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         OBSERVABILIDADE                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐     OpenTelemetry (OTLP)      ┌──────────────┐       │
│  │  Cashflow    │ ─────────────────────────────▶│   Jaeger     │       │
│  │    API       │                               │  (Traces)    │       │
│  │              │     /metrics (Prometheus)     └──────────────┘       │
│  │              │ ─────────────────────────────▶┌──────────────┐       │
│  │              │                               │  Prometheus  │       │
│  │              │     Serilog → Loki            │  (Métricas)  │       │
│  │              │ ─────────────────────────────▶└──────────────┘       │
│  └──────────────┘                               ┌──────────────┐       │
│                                                 │    Loki      │       │
│  ┌──────────────┐     OpenTelemetry (OTLP)      │   (Logs)     │       │
│  │  Cashflow    │ ─────────────────────────────▶└──────────────┘       │
│  │   Worker     │                                      │               │
│  │              │     Serilog → Loki                   │               │
│  │              │ ─────────────────────────────────────│               │
│  └──────────────┘                                      │               │
│                                                        ▼               │
│                                                ┌──────────────┐        │
│                                                │   Grafana    │        │
│                                                │ (Dashboards) │        │
│                                                └──────────────┘        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## 🚀 Como Executar

### 1. Subir a stack de observabilidade

```bash
# Criar a rede (se não existir)
docker network create cashflow-network

# Subir os serviços de observabilidade
docker compose -f docker-compose.observability.yml up -d

# Verificar se todos estão rodando
docker compose -f docker-compose.observability.yml ps
```

### 2. Executar a aplicação

```bash
# Subir infraestrutura (PostgreSQL, Redis, RabbitMQ)
docker compose up -d

# Executar a API
dotnet run --project src/Cashflow.WebApi
```

### 3. Acessar os dashboards

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **Grafana** | http://localhost:3000 | admin / cashflow123 |
| **Jaeger** | http://localhost:16686 | - |
| **Prometheus** | http://localhost:9090 | - |

## 📈 Métricas Disponíveis (Prometheus)

### Latência
```promql
# P50 (mediana)
histogram_quantile(0.50, sum(rate(http_server_request_duration_seconds_bucket{job="cashflow-api"}[5m])) by (le))

# P95 (requisito: < 100ms)
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket{job="cashflow-api"}[5m])) by (le))

# P99
histogram_quantile(0.99, sum(rate(http_server_request_duration_seconds_bucket{job="cashflow-api"}[5m])) by (le))
```

### Throughput
```promql
# Requests por segundo (RPS)
sum(rate(http_server_request_duration_seconds_count{job="cashflow-api"}[1m]))

# Por endpoint
sum(rate(http_server_request_duration_seconds_count{job="cashflow-api"}[1m])) by (http_route)
```

### Taxa de Erro
```promql
# Percentual de erros 5xx
sum(rate(http_server_request_duration_seconds_count{job="cashflow-api", http_response_status_code=~"5.."}[5m])) 
/ sum(rate(http_server_request_duration_seconds_count{job="cashflow-api"}[5m])) * 100
```

## 📝 Logs Estruturados (Loki)

### Consultas LogQL

```logql
# Todos os logs da aplicação
{app=~"cashflow.*"}

# Apenas erros
{app=~"cashflow.*"} |= "error"

# Logs do endpoint de consolidado
{app="cashflow-api"} |~ "consolidado"

# Logs com trace ID específico
{app=~"cashflow.*"} |= "abc123"
```

### Campos disponíveis

| Campo | Descrição |
|-------|-----------|
| `app` | Nome da aplicação (cashflow-api, cashflow-worker) |
| `env` | Ambiente (Development, Production) |
| `level` | Nível do log (Information, Warning, Error) |
| `TraceId` | ID do trace (correlação com Jaeger) |

## 🔍 Traces Distribuídos (Jaeger)

### Acessando traces

1. Acesse http://localhost:16686
2. Selecione o serviço `cashflow-api` ou `cashflow-worker`
3. Clique em "Find Traces"

### Correlação de traces

Os traces são automaticamente correlacionados com logs no Grafana através do campo `TraceId`.

```
API Request (trace-abc123)
  └── PostgreSQL Query (20ms)
  └── Redis Cache (2ms)
  └── RabbitMQ Publish (5ms)
        └── Worker Process (trace-abc123)
              └── PostgreSQL Update (15ms)
```

## 📊 Dashboards Grafana

### Dashboard: Cashflow - Overview

Métricas exibidas:
- 🎯 **P95 Latency** - Meta: < 100ms
- 📊 **Request Rate** - Meta: 50 RPS
- ❌ **Error Rate** - Meta: < 5%
- 📈 **Latency Percentiles** (P50, P90, P95, P99)
- 📊 **Requests by Endpoint**
- 📝 **Application Logs** (integrado com Loki)

### Acessando o dashboard

1. Acesse http://localhost:3000
2. Login: admin / cashflow123
3. Navegue para: Dashboards → Cashflow → Overview

## 🔧 Configuração

### API (appsettings.json)

```json
{
  "Observability": {
    "OtlpEndpoint": "http://localhost:4317",
    "LokiUrl": "http://localhost:3100"
  }
}
```

### Worker (appsettings.json)

```json
{
  "Observability": {
    "OtlpEndpoint": "http://localhost:4317",
    "LokiUrl": "http://localhost:3100"
  }
}
```

### Docker (variáveis de ambiente)

```yaml
environment:
  - Observability__OtlpEndpoint=http://jaeger:4317
  - Observability__LokiUrl=http://loki:3100
```

## 📋 Requisitos Não-Funcionais

| Métrica | Requisito | Como Medir |
|---------|-----------|------------|
| **Latência P95** | < 100ms | Prometheus + Grafana |
| **Throughput** | 50 RPS | Prometheus + Grafana |
| **Taxa de Erro** | < 5% | Prometheus + Grafana |
| **Logs** | Estruturados | Loki + Grafana |
| **Traces** | Distribuídos | Jaeger |

## 🛑 Troubleshooting

### Prometheus não coleta métricas

```bash
# Verificar se a API expõe /metrics
curl http://localhost:5000/metrics

# Verificar configuração do Prometheus
docker exec cashflow-prometheus cat /etc/prometheus/prometheus.yml
```

### Loki não recebe logs

```bash
# Verificar se Loki está rodando
curl http://localhost:3100/ready

# Verificar logs do Serilog no console da API
```

### Jaeger não mostra traces

```bash
# Verificar se Jaeger está pronto
curl http://localhost:16686/api/services

# Verificar endpoint OTLP
curl http://localhost:4317
```

## 📚 Referências

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Grafana Loki](https://grafana.com/docs/loki/latest/)
- [Jaeger Tracing](https://www.jaegertracing.io/docs/)
- [Prometheus](https://prometheus.io/docs/)
- [Serilog.Sinks.Grafana.Loki](https://github.com/serilog-contrib/serilog-sinks-grafana-loki)

