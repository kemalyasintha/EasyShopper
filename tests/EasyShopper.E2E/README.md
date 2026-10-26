# EasyShopper E2E

This project runs against the actual Compose application, not stubs. It intentionally stays outside EShop.sln so the existing unit-test job does not require live infrastructure.

## Run locally

Use .NET from global.json, Docker Compose, and PowerShell. Copy .env.example to .env and supply local credentials, then run from the repository root:

```sh
docker compose -p easyshopper-e2e up -d --build mongo redis rabbitmq product-api product-query-api cart-api api-gateway
dotnet build tests/EasyShopper.E2E -c Release
pwsh tests/EasyShopper.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
dotnet test tests/EasyShopper.E2E -c Release --no-build --logger trx --results-directory TestResults
docker compose -p easyshopper-e2e down --volumes --remove-orphans
```

Wait for create_product and add_cart consumers before testing. The PR workflow automates readiness. Do not run against production. Fixture IDs are unique and cleanup deletes only their product and cart. Compose project names isolate volumes but published ports still require a free local machine.

## Coverage and evidence

- Switch EN → FR → reload → EN; assert exact labels and HTML language.
- POST product to gateway → RabbitMQ create_product consumer → MongoDB; verify actual stored ID, name and price and the gateway query result.
- POST cart to gateway → RabbitMQ add_cart consumer → Redis; verify stored user, product and quantity and gateway readback.
- French browser product lookup and add-to-cart → verify Redis and gateway readback.
- Invalid product ID returns 400.

No mocks, fixed sleeps for data consistency, or passing assertions based solely on HTTP acceptance. Bounded polling allows eventual consistency. Each browser scenario uses a new context. TRX, browser traces and failure screenshots accompany service logs as CI artifacts. Tests run sequentially by default.

The added storefront is deliberately small: product lookup and a one-item cart with localization. Product prices are not returned by the existing query contract, so UI cart prices are zero; this is not checkout or payment coverage. API fixtures independently test priced carts. Gateway acceptance is HTTP 202, not proof of completed persistence.

SpecFlow 3.9.74 is retained explicitly to meet the requested stack. SpecFlow is end of life (https://github.com/SpecFlowOSS); this is a legacy compatibility dependency. Migration to Reqnroll is recommended before long-term use. No claim of supported .NET 10 compatibility is made until the workflow build succeeds.

## Validation status

Source and workflow checks were performed in the authoring environment. No .NET SDK or Docker executable was available there, so package restore, compilation, browser execution and distributed tests have NOT been run. The PR workflow must pass before treating this as verified automation.

## Known application limits

The existing product handler catches persistence failures and does not pass its MongoDB session into repository writes. These tests detect missing persistence but do not establish transactional atomicity or exactly-once handling. The gateway's existing async routes are unauthenticated; this work does not add an authorization model. Cart commands now use the configured queue to prevent the existing add and remove consumers (both Cart messages) from receiving the same published command.

GitHub administrators must select the E2E job as a required status check if merges should be blocked on failures. A pull_request trigger alone does not configure branch protection.
