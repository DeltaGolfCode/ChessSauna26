# Payment Gateway

This is an implementation of a payment gateway API. A merchant calls it to process a card payment through an acquiring bank, and to retrieve the details of payments it has made previously.

## Solution structure

| Project | Purpose |
|---|---|
| `PaymentGateway.Api` | ASP.NET Core minimal API: endpoints, logging (Serilog) and startup |
| `PaymentGateway.Logic` | Validation, payment processing, the bank client and data access (EF Core) |
| `Tests/PaymentGateway.Logic.Tests` | Unit tests (xUnit v3 + NSubstitute) |
| `Tests/PaymentGatway.Api.IntegrationTests` | API tests using `WebApplicationFactory`, with an in-memory database and a mocked bank |

## How to run

### Prerequisites
- .NET 10 SDK
- Docker

### 1. Start the bank simulator
```bash
docker-compose up
```
This starts the Mountebank bank simulator on `http://localhost:8080`.

### 2. Start SQL Server
The API needs a SQL Server instance. If you don't have one, run it in Docker:
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```
Then point the API at it (this overrides the connection string in `appsettings.json`):
```bash
dotnet user-secrets set "ConnectionStrings:PaymentGateway" "Server=localhost,1433;Database=PaymentGateway;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;" --project PaymentGateway.Api
```
The database and tables are created automatically on startup through EF Core migrations.

> The bank URL defaults to `http://host.docker.internal:8080`, which works with Docker Desktop. If it doesn't resolve on your machine, set `Services:BankGateway:BaseUrl` to `http://localhost:8080` in the same way.

### 3. Run the API
```bash
dotnet run --project PaymentGateway.Api --launch-profile http
```
The API listens on `http://localhost:5042`. In Development, Swagger UI is available at `http://localhost:5042/swagger`.

### Running the tests
```bash
dotnet test --solution PaymentGateway.slnx
```
The tests don't need SQL Server or the bank simulator. They use EF Core's in-memory provider and a substituted bank gateway.

## API

### Process a payment: `POST /api/payments`
```json
{
  "cardNumber": "2222405343248877",
  "expiryMonth": 4,
  "expiryYear": 2027,
  "currency": "GBP",
  "amount": 1050,
  "cvv": "123"
}
```
Response `200 OK`:
```json
{
  "id": "0bb07405-6d44-4b50-a14f-7ae0beff13ad",
  "status": "Authorized",
  "cardNumberLastFour": "8877",
  "expiryMonth": 4,
  "expiryYear": 2027,
  "currency": "GBP",
  "amount": 1050
}
```
`status` is one of `Authorized`, `Declined` or `Rejected`. A Rejected payment also returns `200 OK`, with the status in the body. The response doesn't say which field failed validation. If the body isn't valid JSON, or a field has the wrong type, ASP.NET Core returns its standard `400 Bad Request`.

### Retrieve a payment: `GET /api/payments/{id}`
Returns `200 OK` with the same response body as above, or `404 Not Found` if no payment has that ID.

## Validation and assumptions

A request that fails validation is **Rejected** without calling the bank.

| Field | Rule |
|---|---|
| Card number | Required; 14–19 characters; digits only |
| Expiry month | Required; 1–12 |
| Expiry year | Required; the month/year combination must not be in the past |
| Currency | Required; 3 characters; one of `GBP`, `USD` or `EUR` |
| Amount | Required; an integer in the minor currency unit; must be greater than 0 |
| CVV | Required; 3–4 characters; digits only |

Assumptions:
- **A card is valid until the end of its expiry month**, so a card expiring in the current month is accepted.
- **The amount must be greater than 0.** The spec only requires an integer, but a payment of zero or less makes no sense.
- **Currency codes are matched case-insensitively.** They are passed to the bank and stored as supplied.
- **Only the last four digits of the card number are returned or stored.** The full card number and the CVV are sent to the bank and never persisted.

## Storage
Payments are stored in SQL Server. Each record holds the payment ID (a GUID), its status and the full response serialised as JSON.

The spec says a real database isn't needed and a test double would do. I chose SQL Server with EF Core so that payments survive a restart and the data-access layer looks like it would in a real service. The repository sits behind `IPaymentHistoryRepository`, so an in-memory implementation could replace it without changing the payment logic.

The spec doesn't ask for it, but I store **Rejected** payments as well as Authorized and Declined ones. A merchant is just as likely to want to look up a rejected payment as a declined one, so if one is recorded, the other should be too. As a result, `GET /api/payments/{id}` can return `Rejected`, even though the spec's retrieval table only lists `Authorized` and `Declined`.

Storing the response as JSON keeps the schema simple. The trade-off is that the fields inside it can't be queried, and the stored format is tied to the API response model. In production I would give the stored payment its own columns, separate from the API contract.

## Bank gateway
If the bank call fails (a 503, a 400, a timeout or an unreachable bank), the payment is recorded as **Rejected**. I did this to keep the set of outcomes small. The spec defines Rejected as invalid input that never reaches the bank, so this stretches that definition.

In production I would return a `502`/`503` to the merchant so they know they can retry. I would also treat timeouts separately: after a timeout the bank may have authorised the payment, so the true outcome is unknown and needs reconciling rather than being recorded as Rejected.

A retry policy would need to be implemented here that allows for a progressive delay between attempts. I didn't try to implement anything like this, as the architecture needed to achieve it would fundamentally change how the test would be written. The payment processing endpoint would become a "fire and forget" endpoint, only returning a unique payment gateway identifier; validation would still be done here, and a rejected status would still be a response. A new endpoint would allow the merchant to poll the payment gateway to check the status of the payment. This decoupling of request and response would allow us to handle a retry strategy without the merchant having to wait on an open HTTP connection.

## Authentication
I've not attempted to implement any form of authentication on the payment gateway. I judged that doing so would expand the scope of the test significantly. If I were doing this as an actual project, I would have some form of authentication service (e.g. Duende IdentityServer or Keycloak) managing authentication and issuing tokens. The payment gateway would check that the merchant had a valid token both to make payment requests and to retrieve past payments.

## Multi-tenancy
This solution is not multi-tenanted, because the spec didn't require it. As things stand, anyone who knows a payment ID can retrieve that payment. In production, the gateway would identify the merchant making each request (from their authentication token), save that against the payment, and only return payment information to the merchant who made the original payment.

## Known limitations and next steps
- **Idempotency:** there is no idempotency key, so if a merchant retries after a timeout, the shopper could be charged twice. I would accept an `Idempotency-Key` header and return the original result for repeated requests.
- **Authorisation code:** the bank's `authorization_code` isn't stored yet. It should be, for reconciliation. This is something I missed during development, and would add it in as a new column to the payment history table. I wouldn't add the authorization code to the response, unless it came up in a business case.
- **Validation feedback:** Rejected responses don't say which fields were invalid. A `400` with a validation problem-details body would be more useful to merchants.
- **Resilience:** there is no retry or circuit-breaker policy on the bank client. Any retries would need idempotency with the bank first.
- **Operations:** migrations run on startup, which isn't safe with multiple instances. This was added to make it easier for someone to run the code locally and cleaner for the test, as all you need is a database it can run against. My preferred option is to script out the database schema, rather than rely on Entity Framework to create and manage it.

## Use of AI tools
I used GitHub Copilot and Claude Code as assistants during this test. The design, architecture and trade-offs described above are my own. I used the tools to help write unit tests (guided by the conventions in `.github/instructions/unittests.instructions.md` and `Tests/CLAUDE.md`), to review the code, and to tidy up this README. I reviewed every change they made.
