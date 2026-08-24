# EasyShopper

[![CI](https://github.com/kemalyasintha/EasyShopper/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/kemalyasintha/EasyShopper/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Architecture](https://img.shields.io/badge/architecture-event--driven-0A66C2)

EasyShopper is an event-driven e-commerce microservices reference project built with ASP.NET Core, MassTransit, RabbitMQ, MongoDB, Redis, and Ocelot. It demonstrates asynchronous commands, separated command/query APIs, distributed workflow coordination, API gateway patterns, caching, authentication, and automated quality gates.

The solution began on .NET Core 3.1 and .NET Standard 2.0. It has since been modernized to .NET 10, upgraded to current messaging and data-access libraries, hardened against committed secrets, and placed behind a GitHub Actions build, test, and dependency-audit gate.

> This is a portfolio and engineering reference system. It demonstrates architecture and modernization decisions; it is not presented as a production commerce platform.

## Project highlights

- Nine API projects organized around gateway, user, product, order, cart, inventory, and wallet capabilities.
- RabbitMQ messaging through MassTransit for asynchronous commands and events.
- MassTransit Courier routing slips for the multi-step order-placement workflow.
- Ocelot API Gateway for routing, aggregation, caching, and rate-limiting examples.
- MongoDB databases separated by domain and Redis-backed cart storage.
- JWT authentication with the signing key removed from committed configuration.
- CI enforcement for Release builds, compiler warnings, automated tests, and vulnerable NuGet packages.
- Eighteen projects retargeted and validated on .NET 10.

## Architecture

```mermaid
flowchart LR
    Client[Client] --> Gateway[Ocelot API Gateway]

    Gateway -->|HTTP queries| UserQuery[User Query API]
    Gateway -->|HTTP queries| ProductQuery[Product Query API]
    Gateway -->|HTTP queries| OrderAPI[Order API]
    Gateway -->|Asynchronous commands| RabbitMQ[(RabbitMQ)]

    RabbitMQ --> UserAPI[User API]
    RabbitMQ --> ProductAPI[Product API]
    RabbitMQ --> OrderAPI
    RabbitMQ --> CartAPI[Cart API]
    RabbitMQ --> InventoryAPI[Inventory API]
    RabbitMQ --> WalletAPI[Wallet API]

    OrderAPI --> Courier[MassTransit Courier<br/>Routing Slip]
    Courier --> WalletAPI
    Courier --> InventoryAPI
    Courier --> OrderAPI
    Courier --> CartAPI

    UserAPI --> UserDB[(MongoDB<br/>User)]
    UserQuery --> UserDB
    ProductAPI --> ProductDB[(MongoDB<br/>Product)]
    ProductQuery --> ProductDB
    OrderAPI --> OrderDB[(MongoDB<br/>Order)]
    InventoryAPI --> StockDB[(MongoDB<br/>Stock)]
    WalletAPI --> WalletDB[(MongoDB<br/>Wallet)]
    CartAPI --> Redis[(Redis)]
```

### Order-placement workflow

The order service builds a routing slip that coordinates four activities:

1. Process the customer's wallet transaction.
2. Allocate inventory for the requested products.
3. Update the order state.
4. Update the customer's cart.

MassTransit Courier carries the workflow variables between services and supports compensating activities when a later operation fails.

## Services

| Component | Responsibility | Primary technology |
|---|---|---|
| API Gateway | Entry point, downstream routing, aggregation, rate limiting, and asynchronous command publication | Ocelot, ASP.NET Core |
| User API | User creation commands | MassTransit, MongoDB |
| User Query API | Authentication and user reads | JWT, MongoDB |
| Product API | Product creation commands | MassTransit, MongoDB |
| Product Query API | Product reads | MongoDB |
| Order API | Order creation and routing-slip coordination | MassTransit Courier, MongoDB |
| Cart API | Cart commands and cart state | Redis |
| Inventory API | Stock allocation and release | MongoDB, MassTransit |
| Wallet API | Debit, credit, and compensation operations | MongoDB, MassTransit |
| Infrastructure | Shared contracts, messaging registration, authentication, security, and workflow activities | .NET class library |

## Modernization work

| Area | Before | Current state |
|---|---|---|
| Target frameworks | .NET Core 3.1 / .NET Standard 2.0 | .NET 10 across all 18 projects |
| SDK selection | Developer-machine dependent | Pinned through `global.json` |
| Messaging | MassTransit 6.3.2 | MassTransit and RabbitMQ transport 8.5.7 |
| API gateway | Ocelot 16.0.1 | Ocelot 25.0.0 |
| MongoDB driver | 2.10.3 | 3.11.0 with affected queries updated |
| API documentation | Swashbuckle 6.2.3 | Swashbuckle 10.2.3 where required |
| JSON serialization | Legacy Newtonsoft.Json usage | Application code migrated to `System.Text.Json` |
| Authentication | JWT key stored in configuration | Key externalized and validation hardened |
| Repository hygiene | IDE-generated files tracked | `.vs` and user-specific project files removed |
| Delivery validation | Manual local validation | GitHub Actions build, test, and vulnerability gate |

## Continuous integration

The `CI` workflow runs for pull requests and pushes to `main`:

```mermaid
flowchart LR
    Trigger[Push or pull request] --> Checkout[Checkout]
    Checkout --> SDK[Install SDK from global.json]
    SDK --> Restore[Restore]
    Restore --> Build[Release build<br/>warnings as errors]
    Build --> Test[Automated tests]
    Test --> Audit[Direct and transitive<br/>NuGet vulnerability audit]
```

The workflow fails when:

- the solution cannot be restored or compiled;
- the compiler produces a warning;
- any automated test fails; or
- a vulnerable direct or transitive NuGet dependency is detected.

## Validate the repository

### Prerequisites

- .NET 10 SDK compatible with the version in `global.json`
- Git

Run the same core checks used by CI:

```bash
dotnet restore EShop.sln
dotnet build EShop.sln --configuration Release --no-restore --warnaserror
dotnet test EShop.sln --configuration Release --no-build --logger "console;verbosity=minimal"
dotnet list EShop.sln package --vulnerable --include-transitive
```

At the current checkpoint, the solution builds successfully, the automated test suite passes, and the configured NuGet sources report no known vulnerable packages.

## Runtime dependencies

Running the complete distributed application requires:

| Dependency | Default local endpoint | Purpose |
|---|---|---|
| RabbitMQ | `localhost:5672` | Commands, events, request/response, and routing-slip activities |
| MongoDB | `localhost:27017` | User, product, order, inventory, and wallet persistence |
| Redis | `localhost:6379` | Cart storage and gateway caching examples |

The current configuration uses development-only local endpoints. Docker Compose orchestration and environment-based runtime configuration are listed in the roadmap below.

## Local secrets

The JWT signing key is not committed to `appsettings.json`. Configure it locally for both token creation and validation:

```bash
dotnet user-secrets set "jwt:SecretKey" "replace-with-a-random-secret-of-at-least-64-characters" --project EShop.ApiGateway/EShop.ApiGateway.csproj
dotnet user-secrets set "jwt:SecretKey" "replace-with-the-same-random-secret" --project EShop.User.Query.Api/EShop.User.Query.Api.csproj
```

The `guest` RabbitMQ credentials in committed configuration are intended only for an isolated local development broker. Production credentials must come from a secret store or environment variables.

## Repository structure

```text
EasyShopper/
|-- .github/workflows/          # CI build, test, and dependency audit
|-- EShop.ApiGateway/           # Ocelot gateway and async command middleware
|-- EShop.Infrastructure/       # Contracts, messaging, security, and activities
|-- EShop.User.*/               # User command, query, and persistence projects
|-- EShop.Product.*/            # Product command, query, and persistence projects
|-- EShop.Order.*/              # Order API and persistence projects
|-- EShop.Cart.*/               # Redis-backed cart projects
|-- EShop.Inventory.*/          # Inventory API and persistence projects
|-- EShop.Wallet.*/             # Wallet API and persistence projects
|-- EShop.*.Test/               # Automated test projects
|-- EShop.sln
`-- global.json
```

## Engineering decisions demonstrated

- Event-driven communication reduces direct coupling between command-producing and command-handling services.
- Command and query APIs are separated for the user and product domains.
- Routing slips coordinate a distributed business process without a shared database transaction.
- Domain-specific MongoDB databases reduce persistence coupling.
- Redis provides a fast store for short-lived cart data.
- API gateway policies centralize selected cross-cutting traffic concerns.
- CI makes build quality and dependency health visible and repeatable.

## Roadmap

- Add Dockerfiles and Docker Compose for one-command local startup.
- Replace fixed local endpoints with environment-driven service configuration.
- Expand unit and integration coverage for messaging, compensation, authentication, and persistence paths.
- Add health checks, structured logging, metrics, and distributed tracing.
- Add Testcontainers-based integration tests for RabbitMQ, MongoDB, and Redis.
- Add deployment infrastructure for an Azure-hosted demonstration environment.
- Capture a short end-to-end order workflow demo for the repository and LinkedIn Featured section.

## Author

**Kemal Yasintha**<br>
Senior Full Stack Engineer | .NET | Azure | Distributed Systems | DevOps<br>
[LinkedIn](https://www.linkedin.com/in/kemalyasintha/) | [GitHub](https://github.com/kemalyasintha)
