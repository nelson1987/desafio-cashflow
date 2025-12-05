# 🚀 Manual de Instalação

Guia completo para executar o projeto Cashflow com todas as ferramentas.

## 📋 Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) (24.0+)
- [Docker Compose](https://docs.docker.com/compose/install/) (2.20+)

> **Nota:** Não é necessário ter .NET instalado. Tudo roda via Docker!

---

## 🐧 Instalação no WSL/Linux

### Comando Único (Recomendado)

```bash
cd /mnt/c/git/desafio-cashflow && \
docker compose --profile app up -d --build && \
docker compose -f docker-compose.observability.yml up -d && \
docker network connect desafio-cashflow_cashflow-network cashflow-grafana 2>/dev/null; \
docker network connect desafio-cashflow_cashflow-network cashflow-prometheus 2>/dev/null; \
docker network connect desafio-cashflow_cashflow-network cashflow-loki 2>/dev/null; \
docker network connect desafio-cashflow_cashflow-network cashflow-jaeger 2>/dev/null; \
echo "⏳ Aguardando API iniciar..." && \
sleep 10 && \
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

# 5. Verificar health check
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
Write-Host "Aguardando API iniciar..." -ForegroundColor Yellow; `
Start-Sleep -Seconds 10; `
Invoke-RestMethod -Uri "http://localhost:5000/health"
```

---

## ✅ Verificação

### Health Check da API

```bash
# Deve retornar "Healthy"
curl http://localhost:5000/health
```

### Verificar todos os containers

```bash
docker ps --format "table {{.Names}}\t{{.Status}}" | grep cashflow
```

**Saída esperada:**
```
cashflow-api          Up X minutes (healthy)
cashflow-worker       Up X minutes
cashflow-postgres     Up X minutes (healthy)
cashflow-redis        Up X minutes (healthy)
cashflow-rabbitmq     Up X minutes (healthy)
cashflow-grafana      Up X minutes
cashflow-jaeger       Up X minutes
cashflow-loki         Up X minutes
cashflow-prometheus   Up X minutes
```

### Testar endpoints da API

```bash
# Criar um lançamento de crédito
curl -X POST http://localhost:5000/api/lancamentos \
  -H "Content-Type: application/json" \
  -d '{"valor": 100, "tipo": 1, "data": "2024-12-05", "descricao": "Teste"}'

# Listar lançamentos
curl "http://localhost:5000/api/lancamentos?pagina=1&tamanhoPagina=10"

# Obter consolidado
curl http://localhost:5000/api/consolidado/2024-12-05
```

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
| **RabbitMQ** | http://localhost:15672 | cashflow / cashflow123 |

---

## 🛑 Parar Tudo

```bash
# Parar aplicação e infraestrutura
docker compose --profile app down

# Parar observabilidade
docker compose -f docker-compose.observability.yml down

# Parar TUDO e remover volumes (CUIDADO: apaga dados!)
docker compose --profile app down -v
docker compose -f docker-compose.observability.yml down -v
```

---

## 🔄 Reiniciar

```bash
# Reiniciar apenas a API
docker compose restart api

# Reiniciar tudo
docker compose --profile app restart
```

---

## 📊 Ver Logs

```bash
# Logs da API
docker compose logs -f api

# Logs do Worker
docker compose logs -f worker

# Logs de todos
docker compose --profile app logs -f
```

---

## 🐛 Troubleshooting

### API não inicia

```bash
# Ver logs detalhados
docker compose logs api

# Verificar se PostgreSQL está healthy
docker compose ps postgres
```

### Porta já em uso

```bash
# Verificar o que está usando a porta
lsof -i :5000

# Parar containers e tentar novamente
docker compose down
docker compose --profile app up -d
```

### Problemas de rede

```bash
# Recriar rede
docker network rm desafio-cashflow_cashflow-network
docker compose --profile app up -d
```

---

## 📚 Documentação Adicional

- [API Reference](API.md) - Documentação dos endpoints
- [Observabilidade](OBSERVABILIDADE.md) - Grafana, Prometheus, Jaeger
- [Docker](DOCKER.md) - Configurações avançadas
- [Arquitetura](ARQUITETURA.md) - Decisões técnicas

