# 🚀 Manual de Instalação

Guia completo para executar o projeto Cashflow com todas as ferramentas.

## 📋 Pré-requisitos

| Ferramenta | Versão | Obrigatório |
|------------|--------|-------------|
| [Docker](https://docs.docker.com/get-docker/) | 24.0+ | ✅ |
| [Docker Compose](https://docs.docker.com/compose/install/) | 2.20+ | ✅ |
| [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0+ | ⚠️ Apenas para desenvolvimento local |

> **💡 Nota:** Não é necessário ter .NET instalado para rodar a aplicação. Tudo roda via Docker!

---

## 🐧 Instalação Rápida (WSL/Linux)

### Comando Único

```bash
cd /mnt/c/git/desafio-cashflow && \
docker compose --profile app up -d --build && \
docker compose -f docker-compose.observability.yml up -d && \
docker network connect desafio-cashflow_cashflow-network cashflow-grafana 2>/dev/null; \
docker network connect desafio-cashflow_cashflow-network cashflow-prometheus 2>/dev/null; \
docker network connect desafio-cashflow_cashflow-network cashflow-loki 2>/dev/null; \
docker network connect desafio-cashflow_cashflow-network cashflow-jaeger 2>/dev/null; \
echo "⏳ Aguardando API iniciar..." && \
sleep 15 && \
curl -s http://localhost:5000/health && echo " ✅ API está saudável!"
```

### Passo a Passo

```bash
# 1. Navegar para o projeto
cd /mnt/c/git/desafio-cashflow

# 2. Subir infraestrutura + API + Worker
docker compose --profile app up -d --build

# 3. Subir observabilidade (Grafana, Prometheus, Loki, Jaeger)
docker compose -f docker-compose.observability.yml up -d

# 4. Conectar containers na mesma rede
docker network connect desafio-cashflow_cashflow-network cashflow-grafana 2>/dev/null
docker network connect desafio-cashflow_cashflow-network cashflow-prometheus 2>/dev/null
docker network connect desafio-cashflow_cashflow-network cashflow-loki 2>/dev/null
docker network connect desafio-cashflow_cashflow-network cashflow-jaeger 2>/dev/null

# 5. Aguardar e verificar health check
sleep 15
curl http://localhost:5000/health
```

---

## 🪟 Instalação no Windows (PowerShell)

### Comando Único

```powershell
cd C:\git\desafio-cashflow; `
docker compose --profile app up -d --build; `
docker compose -f docker-compose.observability.yml up -d; `
docker network connect desafio-cashflow_cashflow-network cashflow-grafana 2>$null; `
docker network connect desafio-cashflow_cashflow-network cashflow-prometheus 2>$null; `
docker network connect desafio-cashflow_cashflow-network cashflow-loki 2>$null; `
docker network connect desafio-cashflow_cashflow-network cashflow-jaeger 2>$null; `
Write-Host "⏳ Aguardando API iniciar..." -ForegroundColor Yellow; `
Start-Sleep -Seconds 15; `
Invoke-RestMethod -Uri "http://localhost:5000/health"
```

---

## ✅ Verificação

### Health Check da API

```bash
# Linux/WSL
curl http://localhost:5000/health
# Deve retornar: Healthy

# PowerShell
Invoke-RestMethod -Uri "http://localhost:5000/health"
```

### Verificar todos os containers

```bash
docker ps --format "table {{.Names}}\t{{.Status}}" | grep cashflow
```

**Saída esperada:**

```
cashflow-api          Up X minutes (healthy)
cashflow-worker       Up X minutes (healthy)
cashflow-postgres     Up X minutes (healthy)
cashflow-redis        Up X minutes (healthy)
cashflow-rabbitmq     Up X minutes (healthy)
cashflow-grafana      Up X minutes
cashflow-jaeger       Up X minutes
cashflow-loki         Up X minutes
cashflow-prometheus   Up X minutes
```

---

## 🧪 Testar a API

### Via cURL

```bash
# Verificar health
curl http://localhost:5000/health

# Criar um lançamento de crédito (tipo=1)
curl -X POST http://localhost:5000/api/lancamentos \
  -H "Content-Type: application/json" \
  -d '{"valor": 100.50, "tipo": 1, "data": "2024-12-05", "descricao": "Venda produto X"}'

# Criar um lançamento de débito (tipo=2)
curl -X POST http://localhost:5000/api/lancamentos \
  -H "Content-Type: application/json" \
  -d '{"valor": 30.00, "tipo": 2, "data": "2024-12-05", "descricao": "Compra de material"}'

# Listar lançamentos
curl "http://localhost:5000/api/lancamentos?pagina=1&tamanhoPagina=10"

# Obter consolidado do dia
curl http://localhost:5000/api/consolidado/2024-12-05
```

### Via REST Client (VS Code/Cursor)

Use o arquivo [`api.http`](../api.http) na raiz do projeto com a extensão **REST Client**.

---

## 🌐 URLs de Acesso

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **API Swagger** | http://localhost:5000/swagger | - |
| **API Health** | http://localhost:5000/health | - |
| **API Métricas** | http://localhost:5000/metrics | - |
| **Grafana** | http://localhost:3000 | admin / cashflow123 |
| **Jaeger** | http://localhost:16686 | - |
| **Prometheus** | http://localhost:9090 | - |
| **Loki** | http://localhost:3100 | - |
| **RabbitMQ** | http://localhost:15672 | cashflow / cashflow123 |

---

## 🛠️ Modos de Execução

### Apenas Infraestrutura (para desenvolvimento local)

```bash
docker compose up -d
# Sobe: PostgreSQL, Redis, RabbitMQ

# Depois execute a API localmente:
dotnet run --project src/Cashflow.WebApi
```

### Infraestrutura + API + Worker

```bash
docker compose --profile app up -d --build
```

### Tudo (com Observabilidade)

```bash
docker compose --profile app up -d --build
docker compose -f docker-compose.observability.yml up -d
# + conectar networks (ver comando completo acima)
```

---

## 🛑 Parar os Serviços

### Parar aplicação

```bash
docker compose --profile app down
```

### Parar observabilidade

```bash
docker compose -f docker-compose.observability.yml down
```

### Parar TUDO e remover volumes (⚠️ CUIDADO: apaga dados!)

```bash
docker compose --profile app down -v
docker compose -f docker-compose.observability.yml down -v
```

---

## 🔄 Comandos Úteis

### Reiniciar serviços

```bash
# Reiniciar apenas a API
docker compose restart api

# Reiniciar tudo
docker compose --profile app restart
```

### Ver logs

```bash
# Logs da API
docker compose logs -f api

# Logs do Worker
docker compose logs -f worker

# Logs de todos
docker compose --profile app logs -f
```

### Reconstruir imagens

```bash
docker compose --profile app up -d --build --force-recreate
```

---

## 🐛 Troubleshooting

### API não inicia (Restarting)

```bash
# Ver logs detalhados
docker compose logs api

# Verificar se as dependências estão healthy
docker compose ps
```

### Erro de porta em uso

```bash
# Verificar o que está usando a porta
lsof -i :5000  # Linux
netstat -ano | findstr :5000  # Windows

# Parar processos e tentar novamente
docker compose --profile app down
docker compose --profile app up -d
```

### Problemas de rede entre containers

```bash
# Recriar rede
docker network rm desafio-cashflow_cashflow-network 2>/dev/null
docker compose --profile app down
docker compose --profile app up -d
```

### Container crashando com código 139

Isso geralmente indica falta de biblioteca ICU. Verifique se o Dockerfile tem:

```dockerfile
RUN apk add --no-cache icu-libs
```

---

## 📊 Verificar Métricas no Grafana

1. Acesse http://localhost:3000
2. Login: `admin` / `cashflow123`
3. Vá em **Dashboards** → **Cashflow Overview**
4. Visualize:
   - 📈 RPS (Requests per Second)
   - ⏱️ P95 Latency
   - ❌ Error Rate
   - 📝 Logs estruturados

---

## 📚 Documentação Adicional

| Documento | Descrição |
|-----------|-----------|
| [API Reference](API.md) | Documentação dos endpoints |
| [Observabilidade](OBSERVABILIDADE.md) | Grafana, Prometheus, Jaeger |
| [Docker](DOCKER.md) | Configurações avançadas |
| [Arquitetura](ARQUITETURA.md) | Decisões técnicas |
| [Testes](TESTES.md) | Estratégia de testes |
