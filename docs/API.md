# 🌐 API Reference

Documentação completa dos endpoints da Cashflow API.

## 📋 Informações Gerais

| Item | Valor |
|------|-------|
| **Base URL** | `http://localhost:5000` |
| **Swagger** | http://localhost:5000/swagger |
| **Content-Type** | `application/json` |
| **Formato de Data** | `YYYY-MM-DD` |

## 🔑 Tipos de Lançamento

| Valor | Tipo | Descrição |
|-------|------|-----------|
| `1` | Crédito | Entrada de dinheiro (aumenta saldo) |
| `2` | Débito | Saída de dinheiro (diminui saldo) |

---

## 📡 Endpoints

### 🏥 Health Check

#### `GET /health`

Verifica a saúde da aplicação e suas dependências.

**Response:** `200 OK`
```
Healthy
```

**Response:** `503 Service Unavailable`
```
Unhealthy
```

---

### 📊 Métricas

#### `GET /metrics`

Retorna métricas no formato Prometheus.

**Response:** `200 OK`
```
# HELP http_server_request_duration_seconds Duration of HTTP requests
# TYPE http_server_request_duration_seconds histogram
http_server_request_duration_seconds_bucket{...}
```

---

### 💰 Lançamentos

#### `POST /api/lancamentos`

Cria um novo lançamento.

**Request Body:**
```json
{
  "valor": 1500.00,
  "tipo": 1,
  "data": "2024-12-05",
  "descricao": "Venda de produtos"
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|-------------|-----------|
| `valor` | decimal | ✅ | > 0 |
| `tipo` | int | ✅ | 1 (Crédito) ou 2 (Débito) |
| `data` | string | ✅ | Formato YYYY-MM-DD |
| `descricao` | string | ✅ | 1-500 caracteres |

**Response:** `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "valor": 1500.00,
  "tipo": "Credito",
  "data": "2024-12-05T00:00:00",
  "descricao": "Venda de produtos"
}
```

**Response:** `400 Bad Request`
```json
{
  "errors": [
    "O valor deve ser maior que zero",
    "A descrição é obrigatória"
  ]
}
```

---

#### `GET /api/lancamentos`

Lista lançamentos com paginação.

**Query Parameters:**

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `pagina` | int | 1 | Número da página |
| `tamanhoPagina` | int | 10 | Itens por página (máx: 100) |

**Request:**
```
GET /api/lancamentos?pagina=1&tamanhoPagina=10
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "valor": 1500.00,
      "tipo": "Credito",
      "data": "2024-12-05T00:00:00",
      "descricao": "Venda de produtos"
    }
  ],
  "totalItems": 50,
  "pagina": 1,
  "tamanhoPagina": 10,
  "totalPaginas": 5,
  "temProximaPagina": true,
  "temPaginaAnterior": false
}
```

---

#### `GET /api/lancamentos/{id}`

Obtém um lançamento pelo ID.

**Request:**
```
GET /api/lancamentos/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "valor": 1500.00,
  "tipo": "Credito",
  "data": "2024-12-05T00:00:00",
  "descricao": "Venda de produtos"
}
```

**Response:** `404 Not Found`
```json
{
  "error": "Lançamento não encontrado"
}
```

---

#### `GET /api/lancamentos/data/{data}`

Lista lançamentos de uma data específica.

**Request:**
```
GET /api/lancamentos/data/2024-12-05
```

**Response:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "valor": 1500.00,
    "tipo": "Credito",
    "data": "2024-12-05T00:00:00",
    "descricao": "Venda de produtos"
  },
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "valor": 350.00,
    "tipo": "Debito",
    "data": "2024-12-05T00:00:00",
    "descricao": "Pagamento fornecedor"
  }
]
```

---

### 📈 Consolidado Diário

#### `GET /api/consolidado/{data}`

Obtém o saldo consolidado de uma data.

**Request:**
```
GET /api/consolidado/2024-12-05
```

**Response:** `200 OK`
```json
{
  "data": "2024-12-05T00:00:00",
  "totalCreditos": 2250.00,
  "totalDebitos": 550.00,
  "saldo": 1700.00,
  "quantidadeLancamentos": 5
}
```

---

#### `GET /api/consolidado/periodo`

Obtém relatório consolidado por período.

**Query Parameters:**

| Parâmetro | Tipo | Obrigatório | Descrição |
|-----------|------|-------------|-----------|
| `dataInicio` | string | ✅ | Data inicial (YYYY-MM-DD) |
| `dataFim` | string | ✅ | Data final (YYYY-MM-DD) |

**Request:**
```
GET /api/consolidado/periodo?dataInicio=2024-12-01&dataFim=2024-12-31
```

**Response:** `200 OK`
```json
{
  "dataInicio": "2024-12-01T00:00:00",
  "dataFim": "2024-12-31T00:00:00",
  "saldos": [
    {
      "data": "2024-12-01T00:00:00",
      "totalCreditos": 1000.00,
      "totalDebitos": 200.00,
      "saldo": 800.00,
      "quantidadeLancamentos": 3
    },
    {
      "data": "2024-12-02T00:00:00",
      "totalCreditos": 500.00,
      "totalDebitos": 150.00,
      "saldo": 350.00,
      "quantidadeLancamentos": 2
    }
  ],
  "resumo": {
    "totalCreditos": 1500.00,
    "totalDebitos": 350.00,
    "saldoFinal": 1150.00,
    "totalLancamentos": 5,
    "diasComMovimentacao": 2
  }
}
```

**Response:** `400 Bad Request`
```json
{
  "error": "O período máximo é de 90 dias"
}
```

---

#### `POST /api/consolidado/{data}/recalcular`

Força o recálculo do consolidado de uma data.

**Request:**
```
POST /api/consolidado/2024-12-05/recalcular
```

**Response:** `200 OK`
```json
{
  "data": "2024-12-05T00:00:00",
  "totalCreditos": 2250.00,
  "totalDebitos": 550.00,
  "saldo": 1700.00,
  "quantidadeLancamentos": 5
}
```

---

## 🧪 Testando a API

### Usando o arquivo .http (VS Code / Cursor)

1. Abra o arquivo `api.http` na raiz do projeto
2. Use a extensão REST Client
3. Clique em "Send Request" acima de cada requisição

### Usando cURL

```bash
# Health check
curl http://localhost:5000/health

# Criar lançamento
curl -X POST http://localhost:5000/api/lancamentos \
  -H "Content-Type: application/json" \
  -d '{"valor": 100, "tipo": 1, "data": "2024-12-05", "descricao": "Teste"}'

# Listar lançamentos
curl "http://localhost:5000/api/lancamentos?pagina=1&tamanhoPagina=10"

# Obter consolidado
curl http://localhost:5000/api/consolidado/2024-12-05
```

### Usando PowerShell

```powershell
# Health check
Invoke-RestMethod -Uri "http://localhost:5000/health"

# Criar lançamento
$body = @{
    valor = 100
    tipo = 1
    data = "2024-12-05"
    descricao = "Teste"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/lancamentos" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

# Listar lançamentos
Invoke-RestMethod -Uri "http://localhost:5000/api/lancamentos?pagina=1&tamanhoPagina=10"
```

---

## ❌ Códigos de Erro

| Código | Significado |
|--------|-------------|
| `200 OK` | Sucesso |
| `201 Created` | Recurso criado |
| `400 Bad Request` | Dados inválidos |
| `404 Not Found` | Recurso não encontrado |
| `500 Internal Server Error` | Erro interno |
| `503 Service Unavailable` | Serviço indisponível |

---

## 📊 Observabilidade

### Endpoints de monitoramento

| Endpoint | Descrição |
|----------|-----------|
| `/health` | Health check da aplicação |
| `/metrics` | Métricas Prometheus |

### Dashboards disponíveis

- **Grafana**: http://localhost:3000 (admin/cashflow123)
- **Jaeger**: http://localhost:16686 (traces distribuídos)
- **Prometheus**: http://localhost:9090 (queries de métricas)

---

## 📚 Referências

- [Swagger UI](http://localhost:5000/swagger)
- [Documentação de Observabilidade](OBSERVABILIDADE.md)
- [Guia de Docker](DOCKER.md)

