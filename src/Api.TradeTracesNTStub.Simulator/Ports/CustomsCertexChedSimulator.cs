using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class CustomsCertexChedSimulator : CustomsCertexChedPort
{
    public Task<ProcessedChedInformationResponse> processedChedRequestAsync(ProcessedChedRequest request) =>
        throw SimulatorFaults.NotImplemented("ProcessedChedRequest");

    public Task<ChedClearanceResponse> chedClearanceRequestAsync(ChedClearanceRequest request) =>
        throw SimulatorFaults.NotImplemented("ChedClearanceRequest");

    public Task<ChedInterventionResponse> chedInterventionRequestAsync(ChedInterventionRequest request) =>
        throw SimulatorFaults.NotImplemented("ChedInterventionRequest");

    public Task<ChedCRFLResponse> chedCRFLRequestAsync(ChedCRFLRequest request) =>
        throw SimulatorFaults.NotImplemented("ChedCRFLRequest");
}
