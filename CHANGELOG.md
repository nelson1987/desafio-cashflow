# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

## [Unreleased]

### Adicionado
- Manual de instalação completo (`docs/INSTALACAO.md`)
- Seção Quick Start no README
- Troubleshooting para erro de ICU no Alpine
- Explicação sobre o `docker-compose.override.yml` na documentação do Docker.

### Corrigido
- Instalação de `icu-libs` no Dockerfile para suporte a globalização
- Remoção de `target: build` no docker-compose.override.yml
- Remoção de volumes desnecessários na configuração de produção
- Remoção de caminhos locais fixos nos comandos de instalação.
- Consistência dos comandos `docker compose` entre os documentos.
- Exemplo de `curl` no `README.md` para incluir o campo `descricao`.

## [1.5.0] - 2024-12-05

### Adicionado
- Stack de observabilidade completa
  - OpenTelemetry para tracing distribuído
  - Prometheus para métricas (P95, RPS, Error Rate)
  - Loki para logs estruturados
  - Jaeger para visualização de traces
  - Grafana com dashboards pré-configurados
- `docker-compose.observability.yml` para stack de monitoramento
- Extensões de observabilidade para API e Worker
- Documentação de observabilidade (`docs/OBSERVABILIDADE.md`)

### Alterado
- Atualização do README com links para observabilidade
- Atualização do ROADMAP com status de v1.5

## [1.4.0] - 2024-12-04

### Adicionado
- Testes de performance com K6
  - Smoke test (validação rápida)
  - Load test para consolidado (55 RPS, P95 < 100ms)
  - Load test para lançamentos
  - Stress test
- Integração de K6 no GitHub Actions
- Release automático no CI/CD
- Badge de CI/CD no README

### Alterado
- Atualização da documentação de testes
- Workflow unificado de CI/CD

## [1.3.0] - 2024-12-03

### Adicionado
- Projeto `Cashflow.ConsolidationWorker`
- Consumer RabbitMQ para processamento assíncrono
- Polly para resiliência (retry, circuit breaker)
- Health check via arquivo para Docker
- `Dockerfile.worker` para build do Worker

### Alterado
- Estrutura de diretórios para incluir `workers/`

## [1.2.0] - 2024-12-02

### Adicionado
- Projeto `Cashflow.WebApi` com Minimal API
- Endpoints de Lançamentos (CRUD)
- Endpoints de Consolidado (período, recálculo)
- Health Checks (PostgreSQL, Redis, RabbitMQ)
- Validações com FluentValidation
- Middleware de tratamento de erros
- Documentação de API (`docs/API.md`)
- Arquivo `api.http` para testes

## [1.1.0] - 2024-12-01

### Adicionado
- Projeto `Cashflow.Infrastructure`
- Entity Framework Core com PostgreSQL
- Redis para cache distribuído
- RabbitMQ para mensageria
- Implementação dos repositórios
- Docker Compose para infraestrutura

## [1.0.0] - 2024-11-30

### Adicionado
- Projeto `Cashflow` (Domain)
- Modelagem DDD completa
- Entidade `Lancamento`
- Value Object `SaldoDiario`
- Agregado `FluxoCaixa`
- Eventos de domínio
- Testes unitários do domínio
- Documentação inicial

[Unreleased]: https://github.com/nelson1987/desafio-cashflow/compare/v1.5.0...HEAD
[1.5.0]: https://github.com/nelson1987/desafio-cashflow/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/nelson1987/desafio-cashflow/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/nelson1987/desafio-cashflow/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/nelson1987/desafio-cashflow/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/nelson1987/desafio-cashflow/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/nelson1987/desafio-cashflow/releases/tag/v1.0.0

