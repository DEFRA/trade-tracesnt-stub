# Traces NT Postman Collection

The files contained within this directory can be imported into Postman.  
They contain environment settings for both a locally running instance of the Traces NT Stub service, as well as an instance running in CDP Dev.  
It also contains a suit of requests for interacting with various Traces NT endpoints.

You will need to provide values for the environment variables:

| Variable                             | Descrption                                                                                                                                                                   | Example                                                                                              |
|--------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| Host                                 | Your locally/CDP running instance                                                                                                                                            | http://localhost:8080<br>https://ephemeral-protected.api.dev.cdp-int.defra.cloud/trade-tracesnt-stub |
| API-KEY                              | Your CDP generated API Key                                                                                                                                                   | See here: https://portal.cdp-int.defra.cloud/documentation/how-to/developer-api-key.md               |
| WEB_SERVICE_CLIENT_ID                | Traces Web Service Client ID assigned to the account you are using to connect to Traces                                                                                      |                                                                                                      |
| USERNAME                             | Your Traces username                                                                                                                                                         |                                                                                                      |
| AUTHENTICATION_KEY                   | The authentication key assigned to your Traces webservice account                                                                                                            |                                                                                                      |
| AUTHORITY_ACTIVITY_ACCESS_IDENTIFIER | The WebService Identifier for Authority Activity. This can be found in the Traces UI by searching for the Operator and viewing the Activity Detail within the Authority Role |                                                                                                      |

## Proxy Requests
The endpoints configured for all the requests are currently set to <code>/mock/</code> by default.  
This will return the mocked responses defined in the Stub service.

The Stub service is able to proxy requests through to the Traces NT Acceptance environment (the default in the Stub service).  
If you wish to proxy the request through to the Traces Acceptance environment, you can update the endpoint path by changing the <code>/mock/</code> part to <code>/proxy/</code>.

For example:
```
# Use the mock response
{{Host}}/mock/tracesnt/ws/EuIntraCertificateServiceV1

# Use the proxy response
{{Host}}/proxy/tracesnt/ws/EuIntraCertificateServiceV1
```

## Request Security Headers

Once you have provided the variable values described above, the Security Headers within the requests will be calculated for you.  
You do not need to do anything further.