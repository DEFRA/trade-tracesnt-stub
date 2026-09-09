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

Only the contracts are implemented so far. Every operation returns a SOAP fault naming itself as not
implemented; CHED behaviour arrives in its own stories.

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
