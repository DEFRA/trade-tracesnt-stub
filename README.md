# trade-tracesnt-stub

A stand-in for the EU TRACES NT platform, so Trade Gateway can be tested without reaching the EU
acceptance environment. It has two faces, in two projects, hosted by one service:

| Project | What it is | Paths |
| --- | --- | --- |
| `src/Api.TradeTracesNTStub.Simulator` | A CoreWCF SOAP simulator hosting the five TRACES ports from the real contracts | `/ChedCertificateServiceV2`, `/EuIntraCertificateServiceV1`, `/DocomCertificateRetrievalServiceV1`, `/ReferenceDataServiceV1`, `/CustomsCertexChedServiceV06` |
| `src/Api.TradeTracesNTStub.Mock` | The older WireMock stub returning canned XML, plus a pass-through proxy to EU acceptance | `/mock/**`, `/proxy/**` |
| `src/Api.TradeTracesNTStub` | The host: health endpoint, logging, and the CDP platform boilerplate | `/health` |

The two do not overlap, so both are available at once.

## Simulator

Trade Gateway builds every endpoint as `{TracesNt:BaseUrl}/{ServicePath}`, so pointing it at the
simulator is one setting:

```bash
TRACESNT__BASEURL=http://localhost:8085
```

Contracts come from the `Defra.Trade.Gateway.TracesNT` NuGet package — the same package Trade
Gateway's clients are generated into, which is what stops the two drifting. Nothing is generated in
this repository.

**To pick up a TRACES schema change:** regenerate in `DEFRA/trade-gateway` with
`scripts/update-webservices.sh`, publish a new package version, then bump the `PackageReference` in
`src/Api.TradeTracesNTStub.Simulator` and in `tests/TradeTracesNTStub.IntegrationTests`.

`getChedCertificate` serves CHEDs the control API created. Every other operation still returns a SOAP
fault naming itself as not implemented.

## Control API

The simulator's simple face, on `/control`, is how a test gets data in. It is deliberately not
TRACES-shaped — plain JSON designed for the person writing the test — and it is documented at
`/openapi/v1.json`.

```
POST   /control/cheds            create; returns the stored representation
PUT    /control/cheds/{id}       replace
DELETE /control/cheds/{id}       remove
POST   /control/reset            empty, or ?fixtureSet=<name>
GET    /control/fixture-sets     list the sets and templates available
```

Everything except `type` is optional. Anything left out comes from a baseline template — a real
captured TRACES response — so a test states only what its scenario turns on:

```bash
curl -X POST http://localhost:8085/control/cheds -H 'Content-Type: application/json' -d '{
  "type": "A",
  "status": "VALIDATED",
  "borderControlPost": "GBBEL",
  "commodities": [{ "cnCode": "0101", "originCountry": "AF", "packageType": "BX", "packageCount": 2 }]
}'
```

There is no read-back endpoint on purpose. State is held as the TRACES document, and the SOAP face is
the only way to read it, which stops the two drifting.

**Why the simulator fills in display names.** TRACES enriches on the way out: a document is submitted
with `<StatusCode>1</StatusCode>` and retrieved as `<StatusCode name="To be done (New)">1</StatusCode>`.
Trade Gateway copies that `name` straight into its own model and never derives it, so the simulator
has to supply it. The lookups live in `Control/Lookups/SeedData` and hold only the codes the shipped
fixtures and tests use. An unknown code is rejected with a 400 naming the list and the file to add it
to — a blank name would otherwise surface much later as an unexplained snapshot diff.

State is in memory: a restart is a reset, and `POST /control/reset` is how a test isolates itself.

### Fixture sets

`src/Api.TradeTracesNTStub/fixtures/<name>/*.json` — plain control-model files, so a scenario can be
added and reviewed in a pull request. `reset?fixtureSet=baseline` loads the shipped set, which
includes a CHED that cannot be read (`accessible: false`) for covering the permission-denied path.

### TestKit

`src/Api.TradeTracesNTStub.TestKit` packages builders and a control client for other repositories:

```csharp
var simulator = SimulatorControlClient.At("http://localhost:8085");

var id = await simulator.CreateChed(
    Ched.ChedA()
        .WithStatus("VALIDATED")
        .ArrivingAt("GBBEL")
        .WithCommodity(c => c.CnCode("0101").OriginCountry("AF").Packages(2, "BX"))
        .WithDecision(d => d.Acceptable()));
```

### Authentication

WS-Security is genuinely validated, because a permissive simulator would hide the misconfiguration
most worth catching — a port authenticating as the wrong account. Callers must present a
`wsse:UsernameToken` whose password is `Base64(SHA1(nonce + created + authenticationKey))` inside a
live `wsu:Timestamp`, plus a `WebServiceClientId` header.

The customs port authenticates as a different account from the other four. Credentials are
configured per account, with deliberately fake local defaults in `appsettings.json`:

```
Simulator__Credentials__Default__Username
Simulator__Credentials__Default__AuthenticationKey
Simulator__Credentials__Default__WebServiceClientId
Simulator__Credentials__Customs__...
```

No real TRACES NT credential belongs in this repository. The simulator makes no outbound call to the
EU estate unless a request is deliberately sent to `/proxy/**`.

Anything rejected gets the fault TRACES itself returns — an `env:Client` fault with faultstring
`UnauthenticatedException` — which Trade Gateway surfaces as a 502.

## Running

Build and restore need a GitHub PAT with `read:packages` for the private DEFRA feed:

```bash
export DEFRA_NUGET_PAT=<token>
```

```bash
docker compose up --build -d
```

Or directly:

```bash
dotnet run --project src/Api.TradeTracesNTStub --launch-profile Api.TradeTracesNTStub
```

Either way the service listens on <http://localhost:8085>.

## Testing

```bash
# Unit tests — WS-Security validation and contract serialisation
dotnet test --project tests/TradeTracesNTStub.Test/TradeTracesNTStub.Test.csproj

# Integration tests — require the service to be running
dotnet test --project tests/TradeTracesNTStub.IntegrationTests/TradeTracesNTStub.IntegrationTests.csproj \
  --filter-trait Category=IntegrationTest
```

The integration suite drives the simulator with Trade Gateway's own generated WCF clients, which is
what proves a consumer needs no code change. The WireMock stub is covered by Verify snapshots.

## About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office
(HMSO) to enable information providers in the public sector to license the use and re-use of their
information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few
conditions.
