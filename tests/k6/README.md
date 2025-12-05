# 🚀 Testes de Performance (K6)

Este diretório contém os testes de performance do Cashflow usando [K6](https://k6.io/).

## 📋 Requisitos Não-Funcionais

| Requisito | Meta | Teste |
|-----------|------|-------|
| **Throughput** | 50 RPS em produção | Testamos com 55 RPS |
| **Latência** | P95 < 100ms | Consolidado Diário |
| **Taxa de Erro** | < 5% | Todas as operações |

## 🧪 Testes Disponíveis

| Teste | Arquivo | Descrição |
|-------|---------|-----------|
| **Smoke** | `smoke-test.js` | Validação rápida da API |
| **Consolidado** | `consolidado-load-test.js` | Teste de carga do endpoint consolidado |
| **Lançamentos** | `lancamentos-load-test.js` | Teste misto de CRUD |
| **Stress** | `stress-test.js` | Encontrar limite da aplicação |

## 🔧 Instalação do K6

### Windows (Chocolatey)
```powershell
choco install k6
```

### Windows (Winget)
```powershell
winget install k6 --source winget
```

### Linux/Mac
```bash
# Mac
brew install k6

# Linux (Debian/Ubuntu)
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Docker
```bash
docker pull grafana/k6
```

## 🏃 Executando os Testes

### Pré-requisitos

1. API rodando em `http://localhost:5000`
2. Infraestrutura (PostgreSQL, Redis, RabbitMQ) ativa

```bash
# Iniciar infraestrutura
docker compose up -d

# Iniciar API
dotnet run --project src/Cashflow.WebApi
```

### Smoke Test (Validação Rápida)

```bash
# K6 instalado
k6 run tests/k6/smoke-test.js

# Via Docker
docker run --rm -i --network host grafana/k6 run - < tests/k6/smoke-test.js
```

### Teste de Carga - Consolidado (55 RPS)

```bash
# K6 instalado
k6 run tests/k6/consolidado-load-test.js

# Com URL customizada
k6 run -e BASE_URL=http://api.exemplo.com tests/k6/consolidado-load-test.js

# Via Docker
docker run --rm -i --network host grafana/k6 run - < tests/k6/consolidado-load-test.js
```

### Teste de Carga - Lançamentos

```bash
k6 run tests/k6/lancamentos-load-test.js
```

### Stress Test (Encontrar Limite)

```bash
k6 run tests/k6/stress-test.js
```

## 📊 Interpretando Resultados

### Métricas Principais

| Métrica | Descrição | Meta |
|---------|-----------|------|
| `http_req_duration` | Tempo de resposta | P95 < 100ms |
| `http_req_failed` | Taxa de falha | < 5% |
| `http_reqs` | Requisições/segundo | > 50 |
| `vus` | Usuários virtuais | - |

### Exemplo de Output

```
     ✓ status is 200
     ✓ response time < 100ms

     checks.........................: 100.00% ✓ 6600  ✗ 0
     data_received..................: 2.1 MB  35 kB/s
     data_sent......................: 528 kB  8.8 kB/s
     http_req_duration..............: avg=45ms min=12ms med=42ms max=98ms p(90)=78ms p(95)=89ms
     http_reqs......................: 6600    55/s
     
     ✓ http_req_duration............: p(95)<100ms
     ✓ http_req_failed..............: rate<0.05
```

### Status dos Thresholds

- ✓ = Passou
- ✗ = Falhou

## 🐳 Docker Compose (Opcional)

Para rodar K6 junto com a infraestrutura:

```yaml
# docker-compose.override.yml
services:
  k6:
    image: grafana/k6
    network_mode: host
    volumes:
      - ./tests/k6:/scripts
    command: run /scripts/consolidado-load-test.js
    profiles:
      - performance
```

Executar:
```bash
docker compose --profile performance up k6
```

## 📈 Exportando Resultados

### Para JSON
```bash
k6 run --out json=results.json tests/k6/consolidado-load-test.js
```

### Para CSV
```bash
k6 run --out csv=results.csv tests/k6/consolidado-load-test.js
```

### Para InfluxDB + Grafana
```bash
k6 run --out influxdb=http://localhost:8086/k6 tests/k6/consolidado-load-test.js
```

## 🔗 Referências

- [K6 Documentation](https://k6.io/docs/)
- [K6 Thresholds](https://k6.io/docs/using-k6/thresholds/)
- [K6 Scenarios](https://k6.io/docs/using-k6/scenarios/)

