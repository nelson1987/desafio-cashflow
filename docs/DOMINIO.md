# 🎯 Domínio - Modelagem DDD

Este documento detalha a modelagem de domínio do projeto Cashflow, seguindo os princípios do Domain-Driven Design (DDD).

## 📖 Contexto de Negócio

> *"Um comerciante precisa controlar o seu fluxo de caixa diário com os lançamentos (débito e crédito), também precisa de um relatório que disponibiliza o saldo diário consolidado."*

## 🗺️ Mapa de Contexto

```mermaid
graph TB
    subgraph BC["Bounded Context: Fluxo de Caixa"]
        FC["FluxoCaixa<br/>(Aggregate Root)"]
        L["Lancamento<br/>(Entity)"]
        SD["SaldoDiario<br/>(Value Object)"]
        TL["TipoLancamento<br/>(Enum)"]
    end
    
    User["👤 Comerciante"]
    
    User -->|registra lançamentos| FC
    User -->|consulta relatórios| FC
```

## 🧩 Building Blocks do DDD

### Aggregate Root: FluxoCaixa

O `FluxoCaixa` é o ponto de entrada para todas as operações do domínio.

```mermaid
classDiagram
    class FluxoCaixa {
        -List~Lancamento~ _lancamentos
        +IReadOnlyCollection~Lancamento~ Lancamentos
        +RegistrarCredito(valor, data, descricao) Lancamento
        +RegistrarDebito(valor, data, descricao) Lancamento
        +ObterSaldoDiario(data) SaldoDiario
        +ObterRelatorioConsolidado(dataInicio, dataFim) IEnumerable~SaldoDiario~
        +ObterSaldoAcumulado(data) decimal
        +ObterLancamentosDoDia(data) IEnumerable~Lancamento~
    }
```

**Responsabilidades:**
- Gerenciar a coleção de lançamentos
- Garantir consistência das operações
- Gerar relatórios consolidados

**Por que é um Aggregate Root?**
- É o ponto único de acesso para criar lançamentos
- Protege invariantes do domínio
- Controla o ciclo de vida dos lançamentos

### Entity: Lancamento

O `Lancamento` representa cada movimentação financeira no caixa.

```mermaid
classDiagram
    class Lancamento {
        +Guid Id
        +decimal Valor
        +TipoLancamento Tipo
        +DateTime Data
        +string Descricao
        +decimal ValorComSinal
        +EhDoDia(dia) bool
        -ValidarValor(valor) void
        -ValidarDescricao(descricao) void
    }
```

**Características de Entidade:**
- Possui **identidade única** (`Id`)
- Duas instâncias com mesmos atributos mas IDs diferentes são **diferentes**
- Mantém estado e comportamento

**Invariantes:**
- Valor deve ser maior que zero
- Descrição é obrigatória
- Data não pode ser alterada após criação

### Value Object: SaldoDiario

O `SaldoDiario` representa o saldo consolidado de um dia específico.

```mermaid
classDiagram
    class SaldoDiario {
        +DateTime Data
        +decimal TotalCreditos
        +decimal TotalDebitos
        +decimal Saldo
        +int QuantidadeLancamentos
        +Vazio(data)$ SaldoDiario
    }
```

**Características de Value Object:**
- **Sem identidade** - definido apenas por seus atributos
- **Imutável** - não pode ser alterado após criação
- Duas instâncias com mesmos valores são **iguais**
- Pode ser substituído, não modificado

**Por que é um Value Object?**
- Representa um conceito descritivo (o saldo de um dia)
- Não precisa ser rastreado por identidade
- É calculado a partir dos lançamentos

### Enumeration: TipoLancamento

```mermaid
classDiagram
    class TipoLancamento {
        <<enumeration>>
        Credito = 1
        Debito = 2
    }
```

**Semântica:**
- `Credito`: Entrada de dinheiro (aumenta saldo)
- `Debito`: Saída de dinheiro (diminui saldo)

## 🔄 Fluxos de Domínio

### Registrar Lançamento

```mermaid
sequenceDiagram
    actor C as Comerciante
    participant FC as FluxoCaixa
    participant L as Lancamento
    
    C->>FC: RegistrarCredito(100, hoje, "Venda")
    FC->>L: new Lancamento(100, Credito, hoje, "Venda")
    L->>L: ValidarValor(100) ✅
    L->>L: ValidarDescricao("Venda") ✅
    L-->>FC: lancamento criado
    FC->>FC: _lancamentos.Add(lancamento)
    FC-->>C: Lancamento
```

### Obter Saldo Diário

```mermaid
sequenceDiagram
    actor C as Comerciante
    participant FC as FluxoCaixa
    participant SD as SaldoDiario
    
    C->>FC: ObterSaldoDiario(hoje)
    FC->>SD: new SaldoDiario(hoje, lancamentos)
    SD->>SD: Filtrar lançamentos do dia
    SD->>SD: Somar créditos
    SD->>SD: Somar débitos
    SD->>SD: Calcular saldo
    SD-->>FC: SaldoDiario
    FC-->>C: SaldoDiario
```

### Gerar Relatório Consolidado

```mermaid
sequenceDiagram
    actor C as Comerciante
    participant FC as FluxoCaixa
    participant SD as SaldoDiario
    
    C->>FC: ObterRelatorioConsolidado(dia1, dia3)
    
    loop Para cada dia do período
        FC->>SD: new SaldoDiario(dia, lancamentos)
        SD-->>FC: SaldoDiario
    end
    
    FC-->>C: List<SaldoDiario>
```

## 📏 Regras de Negócio por Entidade

### Lancamento

| Código | Regra | Validação |
|--------|-------|-----------|
| RN01 | Valor maior que zero | `ValidarValor()` |
| RN02 | Descrição obrigatória | `ValidarDescricao()` |
| RN03 | Tipo válido (Crédito/Débito) | Enum fortemente tipado |
| RN04 | Data associada | Parâmetro obrigatório |

### SaldoDiario

| Código | Regra | Implementação |
|--------|-------|---------------|
| RN07 | Saldo = Créditos - Débitos | Propriedade calculada |
| RN08 | Considera apenas lançamentos do dia | Filtro por `EhDoDia()` |

### FluxoCaixa

| Código | Regra | Implementação |
|--------|-------|---------------|
| RN09 | Dias sem lançamentos têm saldo zero | `ObterRelatorioConsolidado()` |
| RN13 | DataInício <= DataFim | Validação no método |

## 🎨 Linguagem Ubíqua

Glossário de termos do domínio:

| Termo | Definição |
|-------|-----------|
| **Lançamento** | Registro de movimentação financeira (entrada ou saída) |
| **Crédito** | Entrada de dinheiro no caixa |
| **Débito** | Saída de dinheiro do caixa |
| **Saldo** | Diferença entre créditos e débitos |
| **Saldo Diário** | Consolidação de todas as movimentações de um dia |
| **Saldo Acumulado** | Soma de todos os lançamentos até uma data |
| **Fluxo de Caixa** | Controle de entradas e saídas financeiras |
| **Relatório Consolidado** | Visão do saldo diário para um período |

## 🏗️ Decisões de Design

### 1. Por que `ValorComSinal` no Lancamento?

```csharp
public decimal ValorComSinal => Tipo == TipoLancamento.Credito ? Valor : -Valor;
```

**Motivo:** Facilita cálculos de saldo sem precisar verificar o tipo a cada soma.

### 2. Por que `EhDoDia()` como método?

```csharp
public bool EhDoDia(DateTime dia) => Data.Date == dia.Date;
```

**Motivo:** Encapsula a lógica de comparação de datas (ignora hora), expressando a intenção do domínio.

### 3. Por que data é armazenada apenas como Date?

```csharp
Data = data.Date;  // Remove componente de hora
```

**Motivo:** O domínio trabalha com saldo **diário**, a hora não é relevante para o negócio.

### 4. Por que construtor privado vazio nas entidades?

```csharp
private Lancamento() { }
```

**Motivo:** Permite uso futuro de ORMs (Entity Framework) sem expor construtor público.

## 📚 Referências

- [Domain-Driven Design Reference - Eric Evans](https://www.domainlanguage.com/ddd/reference/)
- [Implementing Domain-Driven Design - Vaughn Vernon](https://www.amazon.com/Implementing-Domain-Driven-Design-Vaughn-Vernon/dp/0321834577)
- [DDD Building Blocks](https://martinfowler.com/bliki/EvansClassification.html)

