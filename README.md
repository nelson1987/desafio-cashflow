# 🏗️ Projeto Cashflow - Arquitetura para um Sistema de Fluxo de Caixa Resiliente

Este projeto implementa um sistema de controle de fluxo de caixa, permitindo o registro de lançamentos de débitos e créditos e a consulta de saldos consolidados. Mais do que um simples CRUD, este repositório serve como um exemplo prático de como construir um sistema distribuído, robusto e observável utilizando tecnologias modernas e boas práticas de engenharia de software.

O objetivo principal é demonstrar as **decisões arquiteturais** por trás de um sistema pensado para crescer, com foco em resiliência, escalabilidade e manutenibilidade.

## 🏛️ A Decisão Arquitetural Central: Microsserviços vs. Monolito

Para um sistema de fluxo de caixa, a abordagem mais óbvia seria construir um **monolito**: uma única aplicação responsável por todas as regras de negócio, desde o registro de lançamentos até a geração de relatórios. Embora mais simples de iniciar, essa abordagem traz riscos significativos que comprometem o crescimento e a confiabilidade do sistema a longo prazo.

Optamos por uma **arquitetura de microsserviços**, dividindo o sistema em dois serviços principais e independentes:

1.  **`Cashflow.WebApi` (Serviço de Lançamentos)**: Uma API REST leve e de alta performance, cuja única responsabilidade é receber, validar e persistir os lançamentos de crédito e débito.
2.  **`Cashflow.ConsolidationWorker` (Serviço de Consolidação)**: Um worker de background que processa os lançamentos em segundo plano para calcular e armazenar o saldo diário consolidado.

Esses dois serviços se comunicam de forma assíncrona através de uma fila de mensagens (**RabbitMQ**).

### Por que essa abordagem?

A separação em microsserviços não foi uma escolha arbitrária, mas uma decisão estratégica para atender a requisitos não-funcionais críticos:

#### 1. **Resiliência e Tolerância a Falhas**
No nosso sistema, a operação de registrar um lançamento **nunca pode falhar** ou ficar indisponível por causa de outra parte do sistema.

*   **Cenário Monolítico**: Se o cálculo do saldo consolidado (uma operação potencialmente lenta ou complexa) estivesse no mesmo processo da API, um bug ou um pico de uso nesse cálculo poderia derrubar a aplicação inteira, impedindo o registro de novas transações.
*   **Nossa Abordagem**: A API apenas aceita o lançamento, o salva no banco de dados e publica um evento na fila. A resposta é imediata (ex: `201 Created`). Se o `ConsolidationWorker` estiver offline ou sobrecarregado, os lançamentos se acumulam na fila para serem processados depois, **sem nunca impactar a disponibilidade da API**. O desacoplamento via fila garante que o sistema continue operando mesmo com falhas parciais.

#### 2. **Performance e Experiência do Usuário**
A performance percebida pelo usuário é crucial.

*   **Cenário Monolítico**: Consultar um saldo consolidado exigiria uma consulta `SUM` no banco de dados a cada requisição, o que se torna lento com o aumento do volume de dados.
*   **Nossa Abordagem**: O `ConsolidationWorker` pré-calcula os saldos e os armazena em um cache de alta velocidade (**Redis**). Quando o usuário solicita um relatório, os dados são lidos diretamente do cache, resultando em uma resposta quase instantânea. A escrita é rápida (apenas um `INSERT` e uma publicação na fila) e a leitura do relatório também é extremamente rápida.

#### 3. **Escalabilidade Independente**
Diferentes partes de um sistema têm diferentes necessidades de carga.

*   **Cenário Monolítico**: Se a API recebe muitas requisições, precisaríamos escalar a aplicação inteira, incluindo a lógica de consolidação que talvez não precise de mais recursos.
*   **Nossa Abordagem**: Podemos escalar os serviços de forma independente. Se a API estiver recebendo um volume alto de novos lançamentos, podemos aumentar o número de réplicas do container `cashflow-api`. Se a fila de consolidação estiver crescendo, podemos escalar apenas o `cashflow-worker` para aumentar o poder de processamento, otimizando o uso de recursos e os custos de infraestrutura.

#### 4. **Manutenibilidade e Foco**
Código mais simples é mais fácil de manter.

*   **Cenário Monolítico**: Com o tempo, as regras de negócio se misturam, tornando a aplicação um "grande emaranhado" difícil de entender, testar e modificar.
*   **Nossa Abordagem**: Cada microsserviço tem um escopo bem definido. Um desenvolvedor trabalhando no `Worker` não precisa se preocupar com os detalhes dos endpoints da API, e vice-versa. Isso reduz a carga cognitiva e permite que as equipes trabalhem em paralelo com mais segurança.

## 🛠️ Justificativa da Stack Tecnológica

Cada ferramenta foi escolhida para reforçar os pilares da nossa arquitetura.

| Categoria | Tecnologia | Por quê? |
| :--- | :--- | :--- |
| **Runtime** | **.NET 9 / C# 13** | Plataforma moderna, de alta performance, open-source e com excelente suporte a desenvolvimento para nuvem e containers. |
| **API** | **ASP.NET Minimal API** | Framework extremamente leve e rápido, ideal para microsserviços onde o baixo consumo de memória e o tempo de boot rápido são importantes. |
| **Modelagem** | **Domain-Driven Design (DDD)** | Para organizar a complexidade do negócio. O Domínio é o coração do software, livre de dependências de infraestrutura, garantindo que a lógica de negócio seja clara, testável e duradoura. |
| **Banco de Dados** | **PostgreSQL** | Um dos bancos de dados relacionais open-source mais robustos e confiáveis do mercado, garantindo a integridade dos dados transacionais. |
| **Mensageria** | **RabbitMQ** | Message broker maduro e confiável que implementa o padrão AMQP e serve como a "espinha dorsal" da nossa comunicação assíncrona, garantindo o desacoplamento e a resiliência entre os serviços. |
| **Cache** | **Redis** | Cache em memória de altíssima velocidade, usado para armazenar os saldos consolidados e entregar relatórios com latência mínima, evitando consultas pesadas ao banco de dados. |
| **Containers** | **Docker / Docker Compose** | Padrão da indústria para empacotar e executar aplicações de forma isolada e consistente em qualquer ambiente. Usamos *multi-stage builds* para criar imagens de produção otimizadas, menores e mais seguras. |
| **Testes** | **xUnit, Moq, Testcontainers, K6** | Uma estratégia de testes completa: **xUnit/Moq** para testes unitários rápidos; **Testcontainers** para testes de integração confiáveis que sobem instâncias reais de Postgres e Redis em Docker; e **K6** para testes de performance que validam os requisitos de carga (55 RPS). |
| **Observabilidade** | **OpenTelemetry, Prometheus, Grafana, Loki** | Em um sistema distribuído, é impossível depurar sem visibilidade. Adotamos uma stack completa de observabilidade para coletar **métricas** (Prometheus), **logs** (Loki) e **traces distribuídos** (Jaeger/OTLP), centralizando tudo em dashboards **Grafana**. |
| **CI/CD** | **GitHub Actions** | Automação completa do ciclo de vida do software. O pipeline compila, testa (todos os níveis), analisa o código, constrói as imagens Docker e, na branch `main`, realiza um release automático com versionamento semântico baseado nas mensagens de commit (Conventional Commits). |

## 🚀 Como Executar

O projeto foi pensado para ser executado com um único comando, subindo toda a infraestrutura e aplicação com Docker.

> **Nota:** Execute o comando abaixo a partir do diretório raiz do projeto.

```bash
# Sobe a API, Worker, Banco de Dados, Cache, Fila e a stack de Observabilidade
docker compose --profile app --profile observability up -d --build
```

Após alguns instantes, a API estará disponível em `http://localhost:5000`.

**Serviços principais:**
| Serviço | URL | Descrição |
| :--- | :--- | :--- |
| **API Swagger** | `http://localhost:5000/swagger` | Documentação interativa da API |
| **Grafana** | `http://localhost:3000` | Dashboards de Métricas, Logs e Traces |
| **RabbitMQ UI** | `http://localhost:15672` | Gerenciamento da fila de mensagens |

Para mais detalhes sobre a execução e todos os serviços disponíveis, consulte o [**Manual de Instalação**](docs/INSTALACAO.md).

## 🎯 Conclusão

Este projeto é um exemplo prático de que, mesmo para um problema de negócio aparentemente simples, aplicar uma arquitetura bem fundamentada resulta em um software de maior qualidade, preparado para o futuro. As escolhas feitas aqui visam demonstrar um caminho para a construção de sistemas que são não apenas funcionais, mas também resilientes, escaláveis e fáceis de manter.
