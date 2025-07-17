# Projeto Fluxo de Caixa Diário

Este projeto é uma **aplicação de exemplo para um sistema de Fluxo de Caixa Diário**, arquitetada com base em **microserviços**. Ele foi desenvolvido seguindo as **melhores práticas**, **padrões de projeto** e com foco em **escalabilidade** e **resiliência**.

---

## 🚀 Visão Geral da Arquitetura

O sistema é construído como um conjunto de **microserviços independentes** que se comunicam de forma **assíncrona** para garantir **alta disponibilidade** e **tolerância a falhas**.

### Decisões de Arquitetura Chave

* **Microserviços:** Separação das responsabilidades em serviços distintos (Lançamentos, Saldo Diário, Identity).
* **Comunicação Assíncrona:** Utilização de um **Message Broker (RabbitMQ)** para **desacoplamento temporal** e **resiliência** entre o serviço de Lançamentos e o de Consolidação Diária.
* **Buffer de Mensagens com Redis:** O serviço de Lançamentos utiliza o **Redis** como um **buffer temporário e seguro** para agrupar transações e enviá-las em lote para o RabbitMQ. Isso otimiza o *throughput* e reduz a carga no broker em picos de requisições.
* **Contêinerização:** Utilização de **Docker** e **Docker Compose** para **isolamento**, **portabilidade** e **orquestração** dos serviços.

---

## 🎨 Padrões de Projeto e Melhores Práticas

A aplicação adere a **princípios** e **padrões de design modernos** para promover **manutenibilidade**, **testabilidade** e **escalabilidade**.

### Padrões de Projeto (Design Patterns)

* **Repository Pattern:** Abstrai a camada de acesso a dados, facilitando a troca de implementações de persistência e isolando a lógica de negócio dos detalhes do banco de dados.
* **Mediator Pattern (MediatR) com CQRS (Command Query Responsibility Segregation):**
    * **CQRS:** Aprimora a **separação de responsabilidades**, dividindo as operações que mudam o estado do sistema (**Comandos**) das operações que apenas leem o estado (**Queries**). Isso permite otimizar e **escalar operações de leitura e escrita independentemente**.
    * **Mediator (MediatR):** Atua como um "dispatcher" para comandos e queries. Remove a dependência direta entre o emissor (ex: um Controller) e o receptor (o Handler), promovendo um **acoplamento fraco** e um fluxo de trabalho claro para o processamento de requisições.
    * **Benefícios:** **Controladores mais leves**, **intenções claras** da aplicação (Comandos/Queries), *Handlers* **pequenos e isolados** (fácil testabilidade), melhor **escalabilidade**, **organização de código** baseada em funcionalidade e **extensibilidade** (aberto para novas funcionalidades sem modificar código existente).
* **Dependency Injection (DI):** Gerencia as dependências das classes e componentes, promovendo a **inversão de controle**, facilitando a **testabilidade** e o **reuso de código**.

### Princípios SOLID

* **Single Responsibility Principle (SRP):** Cada classe possui uma única responsabilidade bem definida.
* **Open/Closed Principle (OCP):** Aberto para extensão, mas fechado para modificação.
* **Liskov Substitution Principle (LSP):** Classes derivadas devem ser substituíveis pelas suas classes base.
* **Interface Segregation Principle (ISP):** Interfaces pequenas e coesas.
* **Dependency Inversion Principle (DIP):** Módulos de alto nível não devem depender de módulos de baixo nível. Ambos devem depender de abstrações. Abstrações não devem depender de detalhes. Detalhes devem depender de abstrações.

---

## 🔒 Segurança e Autenticação (IdentityServer)

O projeto utiliza **IdentityServer** com protocolo **OpenID Connect (OIDC)** e a biblioteca **Duende IdentityServer**.

* **Controle de Escopos:** O IdentityServer está preparado para controlar escopos por tipos de permissões, de acordo com o papel do usuário, e delimitar acessos a operações de leitura, escrita e exclusão.

---

## 📈 Resiliência e Desempenho

O sistema é projetado para ser **resiliente a falhas** e lidar com **picos de tráfego**, garantindo a **integridade** e **disponibilidade** dos dados.

### Requisitos Não Funcionais Abordados

* **Serviço de Lançamento Não Indisponível se o Serviço de Consolidado Cair:**
    * **Padrão de Mensageria (Publish-Subscribe):** O Serviço de Lançamentos publica eventos de transação no **RabbitMQ** e não espera por uma resposta do Serviço de Consolidado. Se este estiver offline, as mensagens se acumulam nas filas e são processadas quando ele voltar, garantindo **desacoplamento temporal**.
    * **Tratamento de ACK/NACK:** Confirmação da mensagem (`BasicAck`) após processamento bem-sucedido ou rejeição (`BasicNack`) com opção de re-enfileirar em caso de falha.
    * **Circuit Breaker (Opcional):** Para qualquer dependência síncrona futura, um *Circuit Breaker* (e.g., Polly) impediria tentativas repetidas de conexões falhas, evitando lentidão.
* **Picos de 50 Requisições/Segundo com Máximo de 5% de Perda de Requisições:**
    * **Assincronicidade:** O consumo de eventos pelo Serviço de Consolidado é assíncrono, permitindo que ele processe em seu próprio ritmo.
    * **Filas de Mensagens:** O Message Broker atua como um **buffer** para picos de tráfego.
    * **Escalabilidade Horizontal:** O Serviço de Consolidação Diária pode ser **escalado horizontalmente** (múltiplas instâncias consumindo da mesma fila) para distribuir a carga.
    * **Qualidade de Serviço (QoS) do Message Broker:** O RabbitMQ permite configurar o `prefetch count` para evitar sobrecarga de consumidores.
    * **Retries e Dead-Letter Queues (DLQ):** Em caso de falha no processamento, o Message Broker pode re-tentar (`retries`) ou mover a mensagem para uma DLQ para análise, **minimizando a perda de dados** e garantindo resiliência. A perda de 5% de requisições será minimizada por essas estratégias.
* **Disponibilidade e Tolerância a Falhas:**
    * Se o **LançamentosAPI** cair, mensagens já publicadas estarão no **Redis** (buffer) e, posteriormente, no **RabbitMQ**.
    * Se o **Consumidor** cair, as mensagens se acumulam na fila do **RabbitMQ** e são processadas quando ele voltar, sem perda de dados.
    * Falhas transitórias no processamento de mensagens são tratadas com *retries*.

---

## 💻 Tecnologias e Recursos Utilizados

### Desenvolvimento

* **Backend:** ASP.NET Core Web APIs (C#)
* **Frontend:** ASP.NET Core Web Application (Razor Pages)
* **Autenticação:** Duende IdentityServer (OpenID Connect)
* **Mensageria:** RabbitMQ
* **Buffer de Mensagens:** Redis
* **Banco de Dados:** MySQL
* **Validações:** **FluentValidation** (backend) e **jQuery Validation** (frontend).

### Observabilidade

* **Logs:** **Serilog**, **NLog**.
* **Monitoramento:** **Health Checks**, **Prometheus** e **Grafana**.
* **Tracing:** **Jaeger** e **OpenTelemetry**.

### Testes

* **Testes Unitários:**
    * **Frameworks:** xUnit
    * **Mocks:** Moq
    * **Asserções:** FluentAssertions
    * **Dados Falsos:** Bogus (com Faker)
    * **Nomenclatura:** `"Method_Scenario_ExpectedResult"`
    * **Organização:** Arrange-Act-Assert (AAA)
* **Testes de Integração:**
    * Realizados em memória e utilizando **Testcontainers** (ex: `Testcontainers.MySql`) para provisionar dependências de forma isolada e descartável.
* **Testes de Carga:**
    * **Framework:** **k6** (Grafana Labs)
    * **Regras de Configuração (Exemplo de Carga):**
        * Fórmula: `RPS = (VUs * IterationsPerSecond)`
        * `Sleep(1s)`: Cada Virtual User (VU) "dorme" por 1 segundo, permitindo idealmente 1 requisição por segundo por VU.
        * **Cálculo Estimado:** 50 VUs \* 1 RPS/VU = **50 RPS**.
    * **Métricas Chave:** `http_req_duration` (p95 crucial), `http_req_failed` (monitorar < 5%), `http_reqs`, `vus`/`vus_max`, `data_received`/`data_sent`. O k6 apresenta um resumo final indicando o status dos *thresholds*.

### Conteinerização

* **Docker:** `Dockerfile`s com o padrão **multi-stage build** para criar imagens otimizadas.
* **Docker Compose:** Orquestrador para definir, executar e gerenciar todos os serviços da aplicação e suas dependências.

---

## 🚀 Como Rodar o Projeto (Dockerizado)

1.  Navegue até a pasta raiz do seu projeto (onde se encontra o arquivo `docker-compose.yml`).
2.  **Confie no certificado HTTPS (apenas uma vez, se necessário):**
    ```bash
    dotnet dev-certs https --trust
    ```
3.  **Construa as imagens e inicie os contêineres:**
    ```bash
    docker-compose up --build -d
    ```
    *Ou, se preferir iniciar serviços específicos:*
    ```bash
    docker-compose up --build -d identityserver lancamentosapi saldodiarioapi fluxocaixadiario_web
    ```

### Verificação e Logs

* **Verificar o status dos contêineres:**
    ```bash
    docker-compose ps
    ```
* **Ver os logs de todos os serviços (em tempo real):**
    ```bash
    docker-compose logs -f
    ```
* **Verificar os logs de um serviço específico (ex: `lancamentos_tests`):**
    ```bash
    docker-compose logs lancamentos_tests
    ```

### Execução de Testes

* **Execução dos Testes Unitários da API de Lançamentos:**
    ```bash
    docker-compose run --rm lancamentos_tests dotnet test "./src/FluxoCaixaDiario.Lancamentos.Tests/FluxoCaixaDiario.Lancamentos.Tests.csproj" --logger "trx;LogFileName=test-results.trx;ResultsDirectory=/app/testresults"
    ```
* **Execução dos Testes Unitários da API de Saldo Diário:**
    ```bash
    docker-compose run --rm saldodiario_tests dotnet test --logger "trx;LogFileName=test-results.trx" --results-directory "/app/testresults"
    ```
* **Execução dos Testes de Carga do k6 (separadamente):**
    ```bash
    docker-compose run --rm k6 k6 run /scripts/lancamentos_load_test_autenticado.js
    docker-compose run --rm k6 k6 run /scripts/saldo_diario_load_test_autenticado.js
    ```

### Parar e Remover Contêineres

* **Para parar e remover todos os contêineres e redes criadas pelo Docker Compose:**
    ```bash
    docker-compose down
    ```

---

## 🔮 Possíveis Evoluções e Melhorias Futuras

* **Controle de Acesso Fino (IdentityServer):** A funcionalidade de controle de escopos por tipos de permissões pode ser expandida e integrada mais profundamente com as regras de negócio para um controle de acesso ainda mais granular.
* **CI/CD com Análise Estática:** Implementar um pipeline de Continuous Integration/Continuous Deployment (CI/CD) com ferramentas de análise estática de código (ex: SonarQube) para garantir a qualidade contínua.
* **Monitoramento e Dimensionamento de Filas (RabbitMQ):** Monitorar ativamente o tamanho das filas, a latência de processamento e a saúde dos serviços para otimizar os parâmetros de `BatchIntervalMilliseconds` e `MaxBatchSize`. Escalar horizontalmente os serviços (Lançamentos e Consumidor) conforme a carga.
