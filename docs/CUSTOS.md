# 💰 Análise de Custos

Este documento apresenta a análise de custos de licenças e infraestrutura para o projeto Cashflow.

> **Última atualização:** Dezembro 2024

## 📋 Resumo Executivo

| Tipo de Custo | Valor |
|---------------|-------|
| **Licenças de Software** | **$0/mês** |
| **Local (Docker)** | **$0/mês** |
| **Cloud (Dev)** | ~$50-170/mês |
| **Cloud (Prod)** | ~$200-1.500/mês |

---

## 🆓 Custos de Licença

### Stack 100% Open Source

| Ferramenta | Licença | Custo | Uso no Projeto |
|------------|---------|-------|----------------|
| **.NET 9 / ASP.NET** | MIT | **$0** | Runtime + API |
| **PostgreSQL** | PostgreSQL License | **$0** | Banco de dados |
| **Redis** | BSD 3-Clause | **$0** | Cache distribuído |
| **RabbitMQ** | MPL 2.0 | **$0** | Mensageria |
| **Docker Engine** | Apache 2.0 | **$0** | Containers |
| **Kubernetes** | Apache 2.0 | **$0** | Orquestração |
| **Polly** | BSD 3-Clause | **$0** | Resiliência |
| **FluentValidation** | Apache 2.0 | **$0** | Validações |
| **Serilog** | Apache 2.0 | **$0** | Logging |
| **xUnit + Shouldly** | Apache 2.0 / BSD | **$0** | Testes |
| **OpenTelemetry** | Apache 2.0 | **$0** | Tracing |
| **Prometheus** | Apache 2.0 | **$0** | Métricas |
| **Grafana OSS** | AGPL v3 | **$0** | Dashboards |
| **Loki** | AGPL v3 | **$0** | Logs |
| **Jaeger** | Apache 2.0 | **$0** | Traces |

### ⚠️ Docker Desktop - Atenção!

| Tamanho da Empresa | Licença | Custo |
|--------------------|---------|-------|
| < 250 funcionários **E** < $10M receita | Gratuito | **$0** |
| ≥ 250 funcionários **OU** ≥ $10M receita | Business | **$24/usuário/mês** |

**Alternativas 100% gratuitas:**
- **Docker Engine** no WSL/Linux - O que usamos!
- **Podman** - Compatível com Docker
- **Rancher Desktop** - Interface gráfica gratuita

---

## 🏠 Custo Local (Docker Compose)

### Execução Local

| Recurso | Custo |
|---------|-------|
| Docker Engine | **$0** |
| PostgreSQL (container) | **$0** |
| Redis (container) | **$0** |
| RabbitMQ (container) | **$0** |
| Grafana (container) | **$0** |
| Prometheus (container) | **$0** |
| Loki (container) | **$0** |
| Jaeger (container) | **$0** |
| **TOTAL** | **$0/mês** |

> 💡 **Requisito:** Máquina com pelo menos 8GB RAM e Docker instalado.

---

## ☁️ Opções de Hospedagem Cloud

### Comparativo de Plataformas

| Plataforma | Tipo | Dev | Prod Básico | Observação |
|------------|------|-----|-------------|------------|
| **Railway** | PaaS | ~$5-20 | ~$50-150 | Mais simples |
| **Render** | PaaS | ~$7-25 | ~$50-200 | Boa opção |
| **Fly.io** | PaaS | ~$5-15 | ~$30-100 | Mais barato |
| **DigitalOcean** | IaaS | ~$24-50 | ~$100-300 | App Platform |
| **AWS** | IaaS | ~$50-150 | ~$300-800 | Mais complexo |
| **GCP** | IaaS | ~$50-170 | ~$300-1.000 | GKE Autopilot |
| **Azure** | IaaS | ~$50-150 | ~$300-800 | AKS |

---

## 🚂 Railway (Recomendado para Começar)

### Ambiente de Desenvolvimento

| Serviço | Configuração | Custo/mês |
|---------|--------------|-----------|
| **API** | 512MB RAM | ~$5 |
| **Worker** | 512MB RAM | ~$5 |
| **PostgreSQL** | 1GB | ~$7 |
| **Redis** | 256MB | ~$3 |
| **RabbitMQ** | Plugin | ~$0 (usar Redis como fila) |
| **TOTAL DEV** | | **~$20/mês** |

### Ambiente de Produção

| Serviço | Configuração | Custo/mês |
|---------|--------------|-----------|
| **API** | 2GB RAM, 2 réplicas | ~$40 |
| **Worker** | 1GB RAM | ~$10 |
| **PostgreSQL** | 4GB, backups | ~$25 |
| **Redis** | 1GB | ~$10 |
| **TOTAL PROD** | | **~$85/mês** |

---

## 🪰 Fly.io (Mais Econômico)

### Ambiente de Desenvolvimento

| Serviço | Configuração | Custo/mês |
|---------|--------------|-----------|
| **API** | shared-cpu-1x, 256MB | ~$2 |
| **Worker** | shared-cpu-1x, 256MB | ~$2 |
| **PostgreSQL** | 1GB (Fly Postgres) | ~$7 |
| **Redis** | Upstash (serverless) | ~$0-5 |
| **TOTAL DEV** | | **~$15/mês** |

### Ambiente de Produção

| Serviço | Configuração | Custo/mês |
|---------|--------------|-----------|
| **API** | dedicated-cpu-1x, 1GB, 2 réplicas | ~$30 |
| **Worker** | shared-cpu-1x, 512MB | ~$5 |
| **PostgreSQL** | 2GB HA | ~$20 |
| **Redis** | Upstash Pro | ~$10 |
| **TOTAL PROD** | | **~$65/mês** |

---

## ☁️ Google Cloud Platform (Produção Escalável)

### Ambiente de Desenvolvimento

| Serviço | Configuração | Custo/mês |
|---------|--------------|-----------|
| **GKE Autopilot** | 2 vCPU, 4GB RAM | ~$80 |
| **Cloud SQL** | 1 vCPU, 3.75GB, 20GB SSD | ~$35 |
| **Memorystore Redis** | 1GB Basic | ~$35 |
| **Cloud Pub/Sub** | < 10GB/mês | ~$0 (free tier) |
| **Load Balancer** | 1 regra | ~$18 |
| **TOTAL DEV** | | **~$170/mês** |

### Ambiente de Produção (50 req/s)

| Serviço | Configuração | Custo/mês |
|---------|--------------|-----------|
| **GKE Autopilot** | 8 vCPU, 32GB RAM (auto-scale) | ~$400 |
| **Cloud SQL** | 4 vCPU, 16GB, 200GB SSD, HA | ~$350 |
| **Memorystore Redis** | 5GB Standard (HA) | ~$175 |
| **Cloud Pub/Sub** | ~500GB/mês | ~$50 |
| **Load Balancer** | 5 regras | ~$60 |
| **Cloud Monitoring** | Métricas + Logs | ~$50 |
| **TOTAL PROD** | | **~$1.085/mês** |

---

## 📊 Observabilidade na Cloud

### Opção 1: Self-Hosted (Recomendado)

| Ferramenta | Custo |
|------------|-------|
| Grafana OSS | **$0** |
| Prometheus | **$0** |
| Loki | **$0** |
| Jaeger | **$0** |
| **TOTAL** | **$0** (apenas infra) |

> Custo adicional de infra: ~$20-50/mês para containers extras

### Opção 2: Grafana Cloud (Managed)

| Tier | Métricas | Logs | Traces | Custo/mês |
|------|----------|------|--------|-----------|
| **Free** | 10K séries | 50GB | 50GB | **$0** |
| **Pro** | 50K séries | 100GB | 100GB | ~$50 |
| **Advanced** | Ilimitado | Ilimitado | Ilimitado | ~$300+ |

### Opção 3: Datadog (Enterprise)

| Tier | Custo |
|------|-------|
| **APM** | ~$31/host/mês |
| **Logs** | ~$1.27/GB ingestado |
| **Infrastructure** | ~$15/host/mês |
| **TOTAL (1 host)** | **~$50-100/mês** |

---

## 💡 Estratégias de Redução de Custos

### 1. Committed Use Discounts (GCP/AWS)

| Compromisso | Desconto |
|-------------|----------|
| 1 ano | **~20-30%** |
| 3 anos | **~50-60%** |

### 2. Spot/Preemptible Instances

| Uso | Economia |
|-----|----------|
| Workers não-críticos | **Até 80%** |
| Ambientes de dev/staging | **Até 80%** |

### 3. Reserved Instances (Banco de Dados)

| Compromisso | Economia |
|-------------|----------|
| 1 ano | **~30%** |
| 3 anos | **~50%** |

### 4. Escalabilidade Inteligente

- GKE Autopilot escala para zero quando não há carga
- Horário comercial apenas para dev
- Serverless para cargas variáveis

---

## 📈 Custos com Otimizações (GCP)

| Ambiente | Normal | CUD 1 ano | CUD 3 anos |
|----------|--------|-----------|------------|
| **Desenvolvimento** | $170/mês | $136/mês | **$85/mês** |
| **Produção** | $1.085/mês | $868/mês | **$543/mês** |

---

## 🎯 Recomendação por Cenário

### 🧪 Desenvolvimento/Testes

| Opção | Custo | Recomendação |
|-------|-------|--------------|
| **Local (Docker)** | $0 | ⭐⭐⭐⭐⭐ Ideal |
| **Fly.io** | ~$15 | ⭐⭐⭐⭐ Bom |
| **Railway** | ~$20 | ⭐⭐⭐ OK |

### 🚀 Produção Pequena (< 10 req/s)

| Opção | Custo | Recomendação |
|-------|-------|--------------|
| **Fly.io** | ~$65 | ⭐⭐⭐⭐⭐ Melhor custo-benefício |
| **Railway** | ~$85 | ⭐⭐⭐⭐ Bom |
| **DigitalOcean** | ~$100 | ⭐⭐⭐ OK |

### 🏢 Produção Escalável (50+ req/s)

| Opção | Custo | Recomendação |
|-------|-------|--------------|
| **GCP (otimizado)** | ~$543 | ⭐⭐⭐⭐⭐ Escalável |
| **AWS (otimizado)** | ~$500 | ⭐⭐⭐⭐ Alternativa |
| **Azure (otimizado)** | ~$550 | ⭐⭐⭐⭐ Enterprise |

---

## 📊 Comparativo Visual

```
Custo Mensal por Opção (USD)

Local (Docker)     |  $0
Fly.io (Dev)       |████ $15
Railway (Dev)      |█████ $20
Fly.io (Prod)      |████████████████ $65
Railway (Prod)     |█████████████████████ $85
GCP (Dev)          |██████████████████████████████████ $170
GCP (Prod)         |████████████████████████████████████████████████████████████████████████████████████████████████████████████ $543 (otimizado)
```

---

## 🧮 TCO (Total Cost of Ownership) - 1 Ano

| Cenário | Mensal | Anual |
|---------|--------|-------|
| **Local** | $0 | $0 |
| **Fly.io Dev** | $15 | $180 |
| **Fly.io Prod** | $65 | $780 |
| **Railway Prod** | $85 | $1.020 |
| **GCP Dev (otimizado)** | $85 | $1.020 |
| **GCP Prod (otimizado)** | $543 | $6.516 |

---

## 📚 Referências

- [Railway Pricing](https://railway.app/pricing)
- [Fly.io Pricing](https://fly.io/docs/about/pricing/)
- [GCP Pricing Calculator](https://cloud.google.com/products/calculator)
- [Grafana Cloud Pricing](https://grafana.com/pricing/)
- [Upstash Pricing](https://upstash.com/pricing)

---

## ✅ Conclusão

| Fase | Recomendação | Custo |
|------|--------------|-------|
| **Desenvolvimento** | Docker Local | **$0** |
| **MVP/Staging** | Fly.io | **~$15-65/mês** |
| **Produção Inicial** | Railway ou Fly.io | **~$65-100/mês** |
| **Produção Escalável** | GCP/AWS com CUD | **~$500-600/mês** |

> 💡 **Dica:** Comece local ($0), valide o produto, depois escale conforme a demanda.
