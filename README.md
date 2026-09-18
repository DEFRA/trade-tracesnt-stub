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

The simulator's simple face, on `/control`, is how a test gets data in. Plain JSON, documented at
`/openapi/v1.json`.

```
POST   /control/cheds            create
PUT    /control/cheds/{id}       replace
PATCH  /control/cheds/{id}       merge a partial update
DELETE /control/cheds/{id}       remove
POST   /control/reset            empty the simulator
```

**The client specifies everything a submitter would. The simulator fills in only what TRACES fills
in.** Nothing is defaulted on your behalf, so a request is the certificate — you can read one
and know what comes back.

That boundary is not guesswork. DG SANTE publish it per element in the CHED mapping workbook
(`TNT-UN-CEFACT-Mappings-CHED-V2.xlsx`, sheet `CHED`, column `Issue`): `M`/`O`/`C` are the
submitter's, `N` is TRACES's. The control model covers the first set; `Control/Mapping` derives the
second.

What that means in practice — you send a code, and the simulator supplies what TRACES would:

| You send | The simulator adds |
| --- | --- |
| `consignorParty.identifier` | the operator's name, role and activity codes |
| `unloadingBaseportLocation` | five more location names, and the whole `IssuerSPSParty` subtree |
| a CN code | its full description hierarchy, up to four levels |
| a clause code | the display text beside it |
| any code | `name=`, `listName=`, `schemeName=` |

Worked examples are in `tickets/create-update-ched-examples/` — `1.submitForDecision.http` creates a
CHED, `4.submitInspectionDecision.http` decides it.

### Creating and deciding

Everything a submitter sends, and nothing else:

```bash
curl -X POST http://localhost:8085/control/cheds -H 'Content-Type: application/json' -d '{
  "exchangedDocument": {
    "includedNote": { "CHED_TYPE": "A" },
    "declaration": { "includedClause": { "PURPOSE": "FREE_CIRCULATION" } }
  },
  "specifiedConsignment": {
    "consignorParty": { "identifier": "770198", "postalAddress": { "countryId": "XI" } },
    "unloadingBaseportLocation": { "identifier": "GBBEL", "countryId": "XI" },
    "includedConsignmentItem": {
      "includedTradeLineItem": [
        { "applicableClassification": { "CN": "0101" }, "originCountry": "AF" }
      ]
    }
  }
}'
```

Deciding it is a `PATCH` carrying only the decision, because notes and clauses merge key by key:

```bash
curl -X PATCH http://localhost:8085/control/cheds/{id} -H 'Content-Type: application/json' -d '{
  "status": "VALIDATED",
  "exchangedDocument": {
    "clearance": { "includedClause": { "DECISION_CONCLUSION": "ACCEPTABLE_FOR_FREE_CIRCULATION" } }
  }
}'
```

Property names follow Trade Gateway's own JSON model, so the vocabulary going in matches what comes
back from `GET /certificates/cheds/{id}`.

There is no read-back endpoint on purpose: the SOAP face is the only way to read a certificate, which
stops the two drifting.

### Lookups, and what happens when one is missing

`Control/Lookups/SeedData` holds the code lists and the operator and authority registries. Only the
codes the tests use are seeded. An unknown code is a `400` naming the list and the file
to add it to — a silent blank would otherwise surface much later as an unexplained snapshot diff.

If a test needs an operator the registry has never heard of, send a `name` and no `identifier`. That
is how TRACES models an operator created on the fly, and it skips the lookup entirely.

State is in memory: a restart is a reset. There are no shared fixture sets to reset to — a test
states the CHEDs it needs, which is the same rule as everywhere else here. Note that `POST
/control/reset` empties the whole simulator, so a test that calls it takes any other test's CHEDs
with it.

### TestKit

`src/Api.TradeTracesNTStub.TestKit` packages builders and a control client for other repositories:

```csharp
var simulator = SimulatorControlClient.At("http://localhost:8085");

var id = await simulator.CreateChed(
    Ched.ChedA()
        .WithDeclaration(d => d.Declaring("FREE_CIRCULATION", "FATTENING"))
        .WithConsignment(c => c
            .ArrivingAt("GBBEL", "XI")
            .WithConsignor(p => p.Operator("770198").InCountry("XI"))
            .WithCommodity(i => i.CnCode("0101").OriginCountry("AF").Packages(2, "BX"))));

await simulator.PatchChed(id, Ched.ChedA().WithStatus("VALIDATED").WithClearance(c => c.Acceptable()));
```

## About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office
(HMSO) to enable information providers in the public sector to license the use and re-use of their
information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few
conditions.
