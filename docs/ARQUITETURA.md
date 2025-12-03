# 🏗️ Arquitetura

Este documento explica as decisões arquiteturais do projeto Cashflow e os motivos por trás de cada escolha.

## 📐 Visão Geral da Arquitetura

```mermaid
flowchart TB
    subgraph Solution["📦 Cashflow.sln"]
        subgraph Src["src/"]
            Cashflow["🎯 Cashflow<br/>(Class Library)<br/><br/>• Entidades<br/>• Value Objects<br/>• Agregados"]
        end
        
        subgraph Tests["tests/"]
            CashflowTests["🧪 Cashflow.Tests<br/>(xUnit + Shouldly)<br/><br/>• Testes Unitários<br/>• Cobertura de Regras"]
        end
    end
    
    CashflowTests -->|referencia| Cashflow
```

## 🤔 Por que essa estrutura?

### Separação `src/` e `tests/`

```
├── src/           # Código de produção
└── tests/         # Código de testes
```

**Motivos:**
- ✅ **Clareza**: Separação óbvia entre código de produção e testes
- ✅ **Build**: Permite compilar apenas o código de produção para deploy
- ✅ **Convenção**: Padrão amplamente adotado na comunidade .NET
- ✅ **CI/CD**: Facilita configuração de pipelines

### Class Library para o Domínio

Optamos por uma **Class Library** (`Cashflow`) em vez de uma aplicação executável.

**Motivos:**
- ✅ **Reutilização**: O domínio pode ser referenciado por qualquer tipo de aplicação (API, Console, Worker, etc.)
- ✅ **Isolamento**: Regras de negócio ficam isoladas de frameworks e infraestrutura
- ✅ **Testabilidade**: Facilita testes unitários sem dependências externas
- ✅ **Evolução**: Permite adicionar camadas (API, Persistência) sem modificar o domínio

## 🎯 Princípios Arquiteturais

### 1. Domain-Driven Design (DDD) Tático

Utilizamos os building blocks do DDD para modelar o domínio:

```mermaid
classDiagram
    class FluxoCaixa {
        <<Aggregate Root>>
        -List~Lancamento~ _lancamentos
        +RegistrarCredito()
        +RegistrarDebito()
        +ObterSaldoDiario()
        +ObterRelatorioConsolidado()
    }
    
    class Lancamento {
        <<Entity>>
        +Guid Id
        +decimal Valor
        +TipoLancamento Tipo
        +DateTime Data
        +string Descricao
        +ValorComSinal()
        +EhDoDia()
    }
    
    class SaldoDiario {
        <<Value Object>>
        +DateTime Data
        +decimal TotalCreditos
        +decimal TotalDebitos
        +decimal Saldo
        +int QuantidadeLancamentos
    }
    
    class TipoLancamento {
        <<Enumeration>>
        Credito
        Debito
    }
    
    FluxoCaixa "1" *-- "many" Lancamento : contém
    FluxoCaixa ..> SaldoDiario : gera
    Lancamento --> TipoLancamento : usa
```

| Conceito | Implementação | Propósito |
|----------|---------------|-----------|
| **Entity** | `Lancamento` | Objeto com identidade única |
| **Value Object** | `SaldoDiario` | Objeto imutável definido por atributos |
| **Aggregate Root** | `FluxoCaixa` | Ponto de entrada para o agregado |
| **Enum** | `TipoLancamento` | Valores enumerados do domínio |

### 2. Rich Domain Model

O domínio contém **comportamento**, não apenas dados:

```csharp
// ❌ Modelo Anêmico (evitamos)
public class Lancamento
{
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
}

// ✅ Modelo Rico (adotamos)
public class Lancamento
{
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    
    public Lancamento(decimal valor, TipoLancamento tipo, ...)
    {
        ValidarValor(valor);  // Validação no construtor
        // ...
    }
    
    public decimal ValorComSinal => ...;  // Comportamento
    public bool EhDoDia(DateTime dia) => ...; // Comportamento
}
```

**Benefícios:**
- ✅ Regras de negócio encapsuladas na entidade
- ✅ Impossível criar objeto em estado inválido
- ✅ Comportamento junto com os dados

### 3. Fail Fast

Validações são feitas no momento da criação:

```csharp
public Lancamento(decimal valor, TipoLancamento tipo, DateTime data, string descricao)
{
    ValidarValor(valor);      // Falha imediatamente se inválido
    ValidarDescricao(descricao);
    
    // Só atribui se passar nas validações
    Valor = valor;
    // ...
}
```

**Benefícios:**
- ✅ Erros são detectados o mais cedo possível
- ✅ Não existe objeto em estado inconsistente
- ✅ Stack trace aponta exatamente onde ocorreu o problema

### 4. Imutabilidade (onde faz sentido)

Propriedades com `private set` para proteger o estado:

```csharp
public decimal Valor { get; private set; }  // Só pode ser definido internamente
```

**Benefícios:**
- ✅ Evita modificações acidentais
- ✅ Thread-safety mais simples
- ✅ Facilita raciocínio sobre o código

## 📁 Estrutura de Pastas

```
Cashflow.sln
│
├── src/
│   └── Cashflow/
│       ├── Cashflow.csproj
│       ├── TipoLancamento.cs      # Enum
│       ├── Lancamento.cs          # Entidade
│       ├── SaldoDiario.cs         # Value Object
│       └── FluxoCaixa.cs          # Agregado Raiz
│
├── tests/
│   └── Cashflow.Tests/
│       ├── Cashflow.Tests.csproj
│       ├── LancamentoTests.cs
│       ├── SaldoDiarioTests.cs
│       └── FluxoCaixaTests.cs
│
└── docs/
    ├── README.md
    ├── ARQUITETURA.md             # Este arquivo
    ├── DOMINIO.md
    ├── TESTES.md
    └── ROADMAP.md
```

## 🔄 Fluxo de Dependências

```mermaid
flowchart BT
    Tests["🧪 Cashflow.Tests<br/>(Projeto de Teste)"]
    Domain["🎯 Cashflow<br/>(Class Library)<br/><br/>Sem dependências externas"]
    
    Tests -->|referencia| Domain
```

**Importante:** O projeto de domínio (`Cashflow`) não tem dependências externas, apenas do .NET. Isso é intencional para:

- ✅ Manter o domínio puro e testável
- ✅ Evitar acoplamento com frameworks
- ✅ Facilitar evolução independente

## 🚀 Evolução Futura

A arquitetura foi pensada para permitir evolução:

```mermaid
flowchart TB
    subgraph Apresentação["Camada de Apresentação"]
        API["🌐 API<br/>(HTTP)"]
        Worker["⚙️ Worker<br/>(Jobs)"]
        Console["💻 Console<br/>(CLI)"]
    end
    
    subgraph Domínio["Camada de Domínio"]
        Domain["🎯 Cashflow<br/>(Domain - atual)"]
    end
    
    subgraph Infraestrutura["Camada de Infraestrutura"]
        Infra["🗄️ Cashflow.Infra<br/>(Persistência)"]
    end
    
    API --> Domain
    Worker --> Domain
    Console --> Domain
    Domain --> Infra
```

## 📚 Referências

- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Project Structure Best Practices](https://docs.microsoft.com/en-us/dotnet/core/porting/project-structure)
