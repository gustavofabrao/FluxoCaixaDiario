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
* **Tracing:** **Jaeger** e **
