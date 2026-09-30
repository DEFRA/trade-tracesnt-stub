# Customs captures from TRACES acceptance

Real request and response pairs from the customs CERTEX port, captured on 2026-09-29 for CDMS-1669.
`CustomsCaptureReplayTests` replays the same sequence against the simulator and holds each response
to the one here.

## How they were taken

Trade Gateway sent the requests through its own customs endpoints, with its TRACES base URL pointed at
this stub's `/proxy`. So every request went out exactly as the gateway builds it, and the raw XML could
be read back from the stub's WireMock request log. The sequence is [`capture.http`](capture.http), and
each call was sent once.

To rerun it, use trade-gateway-local-environment with its `.env` set to:

```env
TRACES_NT_BASE_URL=http://trade-tracesnt-stub:8080/proxy/tracesnt/ws
TRACES_NT_CUSTOMS_OFFICE_REFERENCE_NUMBER=<the real office>
TRACES_NT_CREDENTIALS_CUSTOMS_USERNAME=<real customs account>
TRACES_NT_CREDENTIALS_CUSTOMS_AUTHENTICATION_KEY=<real customs account>
TRACES_NT_CREDENTIALS_CUSTOMS_WEB_SERVICE_CLIENT_ID=<real customs account>
```

The gateway is then on `localhost:8080` and the WireMock log on `localhost:3000/mock/__admin/requests`.
The original run used the same route with both services on the host; check one read goes through before
sending anything that changes state. Clear the log afterwards (`DELETE` the same URL), since it holds
the WS-Security header.

Before committing:

- the WS-Security header and `WebServiceClientId` were removed
- each `ChedCertificate` was replaced with a comment, because it is real acceptance certificate data
  and these captures are about the quantity ledger

## The CHEDs

| CHED | State |
| --- | --- |
| `CHEDP.XI.2026.0000875` | Validated. One line: CN `0401` (raw milk), 1000 kg. 600 kg already consumed by MRN `24GBBGBKCDMS640103`. |
| `CHEDP.XI.2026.0000877` | New. One line: CN `0401`, 1100 kg. |

Test MRNs were `26GB1669CAPTUREA01`, `26GB1669CAPTUREB01` and `26GB1669CAPTUREC01`. The run left
0.002 kg consumed on `0000875` against `26GB1669CAPTUREA01`, and nothing reserved.

## What they show

| Step | Request | TRACES answered |
| --- | --- | --- |
| 01 | Read | 400 kg available; the 600 kg consumed allocation is listed on a plain read |
| 02 | Reserve 0.001 kg, MRN A | `ReservationResult 1`; response carries the whole ledger, other MRNs included |
| 03 | Read | 399.999 available, the reservation listed |
| 04 | Reserve MRN A again, 0.002 kg | Replaces the hold, not adds: 399.998 available, one 0.002 reservation |
| 05 | Release MRN A | Outcome `01` |
| 06 | Read | Reservation now consumed; available unchanged |
| 07 | Release MRN A again | Outcome `03` (already consumed) |
| 08 | Reserve 0.001 kg, MRN B | Reserved |
| 09 | Delete MRN B | Outcome `01` |
| 10 | Read | Quantity back; nothing recorded against MRN B |
| 11 | Delete MRN B again | Outcome `02` (no record) |
| 12 | Release MRN B | Outcome `02` |
| 13 | Reserve 99999999 kg | Refused in-band, reason `05`, failed item goods 1 / line 1. **Not a fault.** |
| 14 | Reserve with code `99999999` | Refused, reason `03`, failed item goods 1 / line 1 |
| 15 | Reserve against line 999 | Refused, reason `07`, failed item goods 1 with **no** line number |
| 16 | Read an unknown CHED | Empty response: no certificate, no summary, `OperationCode 0`. Not a fault. |
| 17 | Reserve against the new CHED | Refused, reason `04` (inappropriate status), **no** failed item |
| 18 | Read the new CHED | Served normally: status does not restrict reading |
| 19 | Reserve against the consumed MRN | Refused, reason `06` (write-off exists), no failed item |
| 20 | Delete the consumed MRN | Outcome `03` |
| 21 | Reserve 0.001 kg, MRN C | Reserved |
| 22 | Reserve MRN C again, 99999999 kg | Refused, reason `05`, and MRN C's existing hold is **gone**: the refusal's ledger shows it removed and the quantity available again |
| 23 | Read | Confirms it: nothing reserved against MRN C |

Also true of every response:

- `CertexHeader` echoes the request's `MessageId` and `UniqRequesterPrefix`.
- `OperationCode` is `0`, and `PushActive` is `false` on processed-CHED responses.
- A refused reservation still carries the ledger summary.
- Clearance responses carry no `StatusCode`.
- Commodity codes are in `HarmonizedSystemSubheadingcode`. An available line carries the CHED's CN
  code (`0401`); an allocation carries the code the declaration reserved with (`040100`).

## Not captured

- Release after the CHED's status has changed (outcome `04`): acceptance gave no way to set it up.
  The simulator's behaviour for it follows the CERTEX guidelines.
- Unit mismatch (reason `10`).
- Which statuses besides Validated allow a reservation. Only New was tried, and it is refused.
- A refused replacement for reasons other than quantity (step 22). The simulator drops the old hold
  for every refusal, the status refusal included.
